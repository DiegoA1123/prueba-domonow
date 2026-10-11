# DomoNow — Control de parqueaderos de visitantes

Solución Full Stack para la prueba técnica de OFIMA SAS: registrar el ingreso y la salida de vehículos de visitantes en los parqueaderos de una propiedad horizontal, garantizando que un parqueadero nunca tenga dos asignaciones activas.

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10 · ASP.NET Core (Minimal APIs) · EF Core · PostgreSQL 17 |
| Frontend | Angular 22 · TypeScript · Signals |
| Pruebas | xUnit · Shouldly · WebApplicationFactory |

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

## Supuestos

_Pendiente._

## Qué mejoraría con más tiempo

_Pendiente._

## Evolución de la solución

> ¿Qué cambiaría si DomoNow manejara 5.000 propiedades y cientos de miles de movimientos diarios?

_Pendiente._

## Uso de IA

Ver [`AI-USAGE.md`](AI-USAGE.md).
