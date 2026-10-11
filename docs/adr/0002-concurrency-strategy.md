# ADR-0002: Estrategia de concurrencia para asignaciones

- **Estado**: Propuesto (se valida con test de integración al implementar)
- **Fecha**: 2026-10-10

## Contexto

Regla 6: si dos usuarios asignan el Parqueadero 15 casi a la vez, solo una solicitud debe crear una asignación activa. Una validación "¿existe asignación activa?" antes de insertar es un *check-then-act*: ambas solicitudes pueden leer "libre" antes de que cualquiera escriba.

## Decisión

Doble barrera, donde **la base de datos es la garantía final**:

1. **Dominio**: el agregado `ParkingSpot.CheckIn()` rechaza si el estado no es `Available` (cubre el caso normal y da mensajes claros). Ver ADR-0003 para el límite del agregado.
2. **Índice único parcial** en PostgreSQL:
   ```sql
   CREATE UNIQUE INDEX ux_parking_assignments_active_spot
       ON parking_assignments (parking_spot_id)
       WHERE status = 'Active';
   ```
   Imposible tener dos activas para el mismo parqueadero, sin importar cuántas instancias de la API existan.
3. **Concurrencia optimista** sobre `parking_spots` usando la columna de sistema `xmin` como token: si dos transacciones cambian el estado del mismo parqueadero, la segunda recibe `DbUpdateConcurrencyException`.

Ambas excepciones (`23505 unique_violation` y `DbUpdateConcurrencyException`) se traducen en Infrastructure a un error de tipo `Conflict` → **HTTP 409**.

## Alternativas consideradas

| Opción | Por qué no |
|--------|-----------|
| Solo validación previa | No garantiza nada bajo concurrencia |
| `lock` / `SemaphoreSlim` en memoria | No funciona con varias instancias de la API |
| `SELECT … FOR UPDATE` (bloqueo pesimista) | Válido, pero acopla el caso de uso a SQL crudo y reduce throughput; el índice parcial es declarativo y más simple |
| Aislamiento `Serializable` | Correcto pero costoso y obliga a reintentos generalizados |

## Consecuencias

- (+) Correcto aun con escalado horizontal.
- (+) El cliente recibe un 409 entendible ("El parqueadero ya fue asignado").
- (−) Depende de una característica de PostgreSQL (índices parciales); SQL Server también los soporta (*filtered index*), así que el riesgo es bajo.
- Verificación: test de integración que lanza N solicitudes simultáneas contra el mismo parqueadero y comprueba exactamente 1 éxito y N−1 respuestas 409.
