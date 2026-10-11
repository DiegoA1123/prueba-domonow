# DomoNow — Control de parqueaderos de visitantes

Solución Full Stack para la prueba técnica de OFIMA SAS: registrar el ingreso y la salida de vehículos de visitantes en los parqueaderos de una propiedad horizontal, garantizando que un parqueadero nunca tenga dos asignaciones activas.

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10 · ASP.NET Core (Minimal APIs) · EF Core · PostgreSQL 17 |
| Frontend | Angular 22 · TypeScript · Signals |
| Seguridad | JWT Bearer · PasswordHasher (PBKDF2) · user-secrets |
| Pruebas | xUnit · Shouldly · WebApplicationFactory + PostgreSQL real |

> 🚧 En construcción. Estado detallado en [`docs/PROGRESS.md`](docs/PROGRESS.md).

## Requisitos previos

- .NET SDK 10.0.401+
- Node.js 24 LTS (recomendado vía nvm) y Angular CLI 22
- PostgreSQL 17

## Configurar la base de datos

_Pendiente._

## Ejecutar el backend

_Pendiente._

## Ejecutar el frontend

_Pendiente._

## Ejecutar las pruebas

_Pendiente._

## Decisiones de arquitectura

Resumen en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md); decisiones formales en [`docs/adr/`](docs/adr/).

- **Domain-Driven Design**: `ParkingSpot` como Aggregate Root, lenguaje ubicuo en [`docs/DOMAIN.md`](docs/DOMAIN.md) (ADR-0003).
- **Clean Architecture + Hexagonal**, versión pragmática (ADR-0001).
- **Concurrencia** garantizada en la base de datos con índice único parcial + concurrencia optimista (ADR-0002).
- **Autenticación JWT** con operadores y roles; la identidad es un subdominio genérico fuera del dominio `Parking` (ADR-0004).

## Supuestos

- Una sola propiedad con 30 parqueaderos de visitantes (`P01`–`P30`), sembrados al crear la base de datos.
- Placas colombianas: carro `ABC123`, moto `ABC12D`. Se normalizan quitando espacios, guiones y puntos y pasando a mayúsculas. Placas extranjeras o diplomáticas no se aceptan en esta versión.
- La hora de salida la pone el servidor; opcionalmente el cliente puede indicar una hora pasada (registro tardío), que nunca puede ser anterior al ingreso ni futura.
- Las fechas se almacenan en UTC; el frontend las muestra en la zona horaria del navegador.
- Solo los operadores autenticados con rol `Operator` registran ingresos y salidas. Un operador de prueba se crea en Development con credenciales tomadas de `dotnet user-secrets`.
- El estado "Cancelada" existe en el modelo, pero no se expone endpoint de cancelación.
- `DestinationUnit` es texto libre (no se valida contra un maestro de unidades, que pertenece a otro contexto de DomoNow).

## Qué mejoraría con más tiempo

_Pendiente._

## Evolución de la solución

> ¿Qué cambiaría si DomoNow manejara 5.000 propiedades y cientos de miles de movimientos diarios?

_Pendiente._

## Uso de IA

Ver [`AI-USAGE.md`](AI-USAGE.md).
