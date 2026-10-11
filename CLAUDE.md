# CLAUDE.md — Manual de trabajo del proyecto

Prueba técnica OFIMA / DomoNow: **Control de parqueaderos de visitantes**.
Stack: .NET 10 + ASP.NET Core + EF Core + PostgreSQL 17 · Angular 22 + TypeScript.
Enunciado completo resumido en `docs/REQUIREMENTS.md`.

## 1. Protocolo obligatorio en cada sesión / tarea

1. **Antes de hacer cualquier cosa**: leer `docs/PROGRESS.md` (estado actual, último paso, siguiente paso).
2. Si la tarea toca arquitectura o código, releer la sección relevante de `docs/ARCHITECTURE.md` y `docs/CONVENTIONS.md`.
3. **Al terminar cada paso significativo** (no al final del día):
   - Añadir una entrada en la bitácora de `docs/PROGRESS.md` (qué se hizo, archivos, decisiones, verificación, siguiente paso).
   - Actualizar el tablero de estado de `docs/PROGRESS.md`.
   - Si se tomó una decisión de arquitectura no trivial → nuevo ADR en `docs/adr/`.
   - Si la IA propuso algo que se aceptó, modificó o **rechazó** con criterio → registrarlo en la sección "Registro continuo" de `AI-USAGE.md`. Esto es el 15% de la nota; no se reconstruye al final, se captura en el momento.
4. Nunca afirmar que algo funciona sin evidencia: ejecutar `dotnet build` / `dotnet test` / `ng test` / `ng build` y citar el resultado.

## 2. Reglas de arquitectura (no negociables)

- **DDD + Clean Architecture + Hexagonal** (Ports & Adapters). Detalle en `docs/ARCHITECTURE.md`; modelo de dominio y lenguaje ubicuo en `docs/DOMAIN.md`.
- **DDD**: `ParkingSpot` es el Aggregate Root; `ParkingAssignment` es entidad interna (solo se modifica vía la raíz). Un repositorio por agregado, un agregado por transacción, nada de modelo anémico. Todo nombre sale del glosario de `docs/DOMAIN.md` (ADR-0003).
- **Regla de dependencia**: las dependencias apuntan hacia el dominio. `Domain` no referencia nada. `Application` solo referencia `Domain`. Los adaptadores (`Infrastructure`, `Api`) dependen de `Application`.
- La lógica de negocio vive en el **dominio** (entidades / value objects), no en controladores, repositorios ni componentes Angular.
- **Pragmatismo**: la prueba penaliza la sobreingeniería. Prohibido sin justificación escrita en un ADR: MediatR, AutoMapper, repositorio genérico, microservicios, CQRS con bases separadas, event sourcing.

## 3. Reglas de negocio que el código DEBE garantizar

| # | Regla | Dónde se garantiza |
|---|-------|-------------------|
| R1 | Máx. 1 asignación activa por parqueadero | Agregado `ParkingSpot.CheckIn` + índice único parcial en BD |
| R2 | No asignar parqueadero Fuera de servicio | Agregado `ParkingSpot.CheckIn` |
| R3 | Al dar salida el parqueadero vuelve a Disponible | Agregado `ParkingSpot.CheckOut` (misma transacción) |
| R4 | Placa obligatoria y normalizada (`abc-123` → `ABC123`) | Value object `LicensePlate` |
| R5 | Salida no anterior al ingreso | Entidad `ParkingAssignment.Finish` (invocada por la raíz) |
| R6 | Concurrencia: solo una solicitud gana | Versión del agregado (`xmin`) + índice único parcial → HTTP 409 |
| R7 | Una placa no ingresa dos veces sin haber salido (regla del cliente) | `CheckInHandler` + índice único parcial por placa → HTTP 409 (cruza agregados, ADR-0005) |

Ver `docs/adr/0002-concurrency-strategy.md`.

Decisiones cerradas con el usuario (2026-10-10): **JWT + operador de prueba** (ADR-0004) · placa estricta `AAA999`/`AAA99A` · hora de salida del servidor + `checkedOutAt` opcional · tests de integración contra PostgreSQL local (`domonow_parking_tests`).

## 4. Flujo de trabajo

- Planear antes de codificar (skill `superpowers:writing-plans`), TDD para reglas de dominio (`superpowers:test-driven-development`), verificar antes de cerrar (`superpowers:verification-before-completion`).
- Commits pequeños con Conventional Commits (ver `docs/CONVENTIONS.md` §5). Solo commitear cuando el usuario lo pida.
- Idioma: **código, identificadores y commits en inglés**; **documentación en español**.

## 5. Comandos

> Se completan a medida que se crean los proyectos.

```bash
# Backend
dotnet build backend/DomoNow.Parking.slnx
dotnet test backend/DomoNow.Parking.slnx

# Frontend
cd frontend && npm start
cd frontend && npm test
```

## 6. Entorno local

- Node vía **nvm-windows** (24.21.0). PostgreSQL 17 nativo como servicio `postgresql-x64-17`, puerto 5432.
- Las credenciales locales NUNCA se commitean: usar `dotnet user-secrets` o variables de entorno.
- Docker no está instalado (requiere WSL); el `docker-compose` es opcional para la entrega.
