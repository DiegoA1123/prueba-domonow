# ADR-0005: Un vehículo no puede ingresar dos veces sin haber salido (R7)

- **Estado**: Aceptado
- **Fecha**: 2026-10-10
- **Relacionado**: ADR-0002 (concurrencia), ADR-0003 (límite del agregado)

## Contexto

Regla adicional definida por el ingeniero (no está en el enunciado): **una misma placa no puede tener más de una asignación activa**, en ningún parqueadero. Si `ABC123` está en P02, no puede registrarse en P07 hasta que salga.

Con el límite de agregado de ADR-0003 (cada `ParkingSpot` solo conoce *su* asignación activa), esta regla **cruza agregados**: ningún `ParkingSpot` individual puede verificarla. Es una validación de conjunto (*set-based*), el caso clásico en DDD donde el agregado no es la herramienta adecuada.

## Decisión

Doble barrera, igual que R1/R6:

1. **Aplicación** (mensaje claro en el caso normal): el `CheckInHandler` consulta el puerto `IParkingSpotRepository.IsVehicleInsideAsync(plate)` antes de invocar al agregado → error `Parking.VehicleAlreadyInside` (**409**).
2. **Base de datos** (garantía bajo concurrencia): índice único parcial
   ```sql
   CREATE UNIQUE INDEX ux_parking_assignments_active_plate
       ON parking_assignments (license_plate)
       WHERE status = 'Active';
   ```
   Si dos operadores registran la misma placa en parqueaderos distintos al mismo tiempo, la segunda inserción falla → se traduce a `Parking.VehicleAlreadyInside` (**409**).

La traducción del error de BD se hace por **nombre de restricción** en Infrastructure (`ux_parking_assignments_active_plate` → `VehicleAlreadyInside`, `ux_parking_assignments_active_spot` → `SpotAlreadyTaken`).

## Alternativas consideradas

| Opción | Por qué no |
|--------|-----------|
| Agregado `Vehicle` con su asignación activa | Introduce un agregado artificial y obliga a modificar dos agregados en una transacción |
| Servicio de dominio con la consulta | Igual necesita el índice para la concurrencia; la consulta en el handler es suficiente y más simple |
| Solo la consulta previa | *check-then-act*: falla bajo concurrencia |
| Solo el índice | Correcto, pero el caso normal devolvería un error genérico de BD en lugar de un mensaje de negocio |

## Consecuencias

- (+) R7 se garantiza aun con concurrencia y con varias instancias de la API.
- (+) Se documenta conscientemente una regla que **no** vive en el agregado y por qué.
- (−) La regla está repartida entre handler e índice; se cubre con un test unitario del handler y un test de integración concurrente.
