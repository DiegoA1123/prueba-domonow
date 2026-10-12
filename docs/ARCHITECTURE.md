# Arquitectura

Estilo: **Domain-Driven Design + Clean Architecture + Hexagonal (Ports & Adapters)**, aplicado con pragmatismo.

| Enfoque | Qué decide | Referencia |
|---------|-----------|-----------|
| DDD | Qué hay en el dominio y cómo se modela (lenguaje ubicuo, agregados, VOs, invariantes) | `adr/0003-domain-driven-design.md`, `DOMAIN.md` |
| Clean Architecture | Capas y regla de dependencia hacia el dominio | `adr/0001-clean-hexagonal-architecture.md` |
| Hexagonal | Cómo entra y sale el mundo exterior: puertos (interfaces) y adaptadores | `adr/0001-clean-hexagonal-architecture.md` |

## 1. Visión general

```
        Adaptadores de entrada (driving)            Adaptadores de salida (driven)
   ┌───────────────────────────┐                ┌──────────────────────────────┐
   │ Api (ASP.NET Core)        │                │ Infrastructure (EF Core +    │
   │ Angular (vía HTTP)        │                │ PostgreSQL)                  │
   └────────────┬──────────────┘                └──────────────▲───────────────┘
                │ llama casos de uso                           │ implementa puertos
        ┌───────▼──────────────────────────────────────────────┴───────┐
        │ Application: casos de uso + PUERTOS (interfaces)             │
        │   ┌────────────────────────────────────────────────────┐     │
        │   │ Domain: entidades, value objects, reglas, errores  │     │
        │   └────────────────────────────────────────────────────┘     │
        └──────────────────────────────────────────────────────────────┘
```

Regla de dependencia: **todo apunta hacia adentro**. El dominio no conoce EF Core, HTTP ni Angular.

## 2. Backend (`/backend`)

```
backend/
├── DomoNow.Parking.slnx
├── global.json · Directory.Build.props · Directory.Packages.props
├── src/
│   ├── DomoNow.Parking.Domain/            # Núcleo DDD. Sin dependencias.
│   │   ├── ParkingSpots/                  # Agregado: ParkingSpot (root) + ParkingAssignment (entity),
│   │   │                                  #   VOs (SpotCode, LicensePlate, VisitorName, DestinationUnit),
│   │   │                                  #   enums de estado, ParkingErrors
│   │   └── Common/                        # Entity/AggregateRoot base, Error, Result
│   ├── DomoNow.Parking.Application/       # Application services (casos de uso) + puertos. Depende de Domain.
│   │   ├── Abstractions/                  # Puertos de salida: IParkingSpotRepository (1 por agregado), IUnitOfWork
│   │   ├── ParkingAssignments/            # Un folder por caso de uso de escritura (slice):
│   │   │   ├── CheckIn/                   #   CheckInCommand, CheckInResponse, CheckInHandler
│   │   │   └── CheckOut/                  #   CheckOutCommand, CheckOutResponse, CheckOutHandler
│   │   ├── Queries/                       # Puerto de lectura IParkingReadModel + DTOs (sin handler:
│   │   │                                  #   no hay lógica que orquestar; el endpoint usa el puerto)
│   │   └── Authentication/                # Subdominio genérico (ADR-0004): LoginHandler, Roles,
│   │                                      #   puertos IOperatorCredentialStore, IAccessTokenIssuer
│   ├── DomoNow.Parking.Infrastructure/    # Adaptador driven. Depende de Application.
│   │   ├── Persistence/                   # ParkingDbContext, Configurations/, Migrations/, Seed
│   │   ├── Repositories/                  # Implementaciones de los puertos
│   │   └── Identity/                      # Operator (tabla), PasswordHasher, JwtAccessTokenIssuer
│   └── DomoNow.Parking.Api/               # Adaptador driving + composition root.
│       ├── Endpoints/                     # Minimal APIs agrupadas por recurso
│       ├── ErrorHandling/                 # Result → ProblemDetails, IExceptionHandler global
│       └── Program.cs
└── tests/
    ├── DomoNow.Parking.Domain.UnitTests/       # Reglas R1–R5 (rápidas, sin BD); R7 en Application.UnitTests
    ├── DomoNow.Parking.Application.UnitTests/  # Handlers con puertos falsos
    └── DomoNow.Parking.Api.IntegrationTests/   # WebApplicationFactory + PostgreSQL local real (R6, auth)
                                                #   BD dedicada `domonow_parking_tests`, recreada por migraciones
```

### 2.1 Responsabilidades por capa

| Capa | Contiene | No contiene |
|------|----------|-------------|
| Domain | Invariantes, transiciones de estado, value objects, errores de dominio | EF Core, DTOs, HTTP, `DateTime.Now` |
| Application | Orquestación del caso de uso, validación de entrada, puertos, DTOs de respuesta | SQL, lógica de negocio duplicada |
| Infrastructure | EF Core, mapeos, migraciones, traducción de errores de BD (unique violation → conflicto) | Reglas de negocio |
| Api | Endpoints, binding, ProblemDetails, CORS, DI, OpenAPI | Lógica de negocio, acceso directo a DbContext |

### 2.2 Puertos

- **Entrada (driving)**: cada caso de uso es un handler concreto (`CheckInHandler.HandleAsync(cmd, ct)`). Sin MediatR: inyección directa, explícita y trazable (ver ADR-0001).
- **Salida (driven)**:
  - Escritura: `IParkingSpotRepository` — un repositorio **por agregado**, carga la raíz con solo su asignación activa. No existe repositorio de `ParkingAssignment` (es entidad interna).
  - Lectura: `IParkingReadModel` — proyecciones directas a DTO para listados (sin pasar por el agregado).
  - `IUnitOfWork.CommitAsync()` confirma la transacción y devuelve `Result` (traduce conflictos de BD a errores de negocio). El reloj es `TimeProvider` (BCL) → tests deterministas.

### 2.2.1 Flujo de un caso de uso (CheckIn)

```
POST /api/parking-assignments
  → Api: valida forma del request, construye CheckInCommand
  → CheckInHandler (application service):
       1. Crea VOs (LicensePlate.Create…) → si falla, Validation error
       2. repo.GetByIdAsync(spotId)        → si no existe, NotFound
       3. repo.IsVehicleInsideAsync(plate) → si ya está dentro, Conflict (R7, cruza agregados)
       4. spot.CheckIn(plate, visitor, unit, clock.GetUtcNow())   ← R1/R2 viven aquí (dominio)
       5. unitOfWork.CommitAsync()         → conflicto de versión/índice → Conflict (R6/R7)
  → Api: Result → 201 Created | ProblemDetails
```

### 2.3 Manejo de errores

- Errores **esperados** de negocio → `Result<T>` con `Error(code, message, type)`; nunca excepciones para flujo de control.
- Errores **inesperados** → excepción → `IExceptionHandler` global → `500` ProblemDetails sin filtrar detalles internos.
- Mapeo HTTP (RFC 9457 ProblemDetails):

| Tipo de error | HTTP | Ejemplo |
|---------------|------|---------|
| Validation | 400 | Placa vacía, formato inválido |
| Unauthorized | 401 | Sin token, token expirado, credenciales inválidas en login |
| Forbidden | 403 | Usuario autenticado sin rol `Operator` intentando escribir |
| NotFound | 404 | Parqueadero o asignación inexistente |
| Conflict | 409 | Parqueadero ocupado / carrera concurrente perdida / asignación ya finalizada |
| BusinessRule | 422 | Parqueadero fuera de servicio, salida anterior a ingreso |

### 2.4 Concurrencia (R1 + R6)

Doble barrera (ADR-0002 + ADR-0003):
1. **Agregado**: `ParkingSpot.CheckIn(...)` rechaza si no está `Available`. La raíz tiene versión (`xmin` de PostgreSQL como token de concurrencia optimista): dos transacciones que modifican el mismo parqueadero → la segunda falla.
2. **Base de datos** (red de seguridad): índice único parcial
   `UNIQUE (parking_spot_id) WHERE status = 'Active'`.
   Cualquiera de los dos fallos se traduce a `409 Conflict`.
3. **R7 (placa)**: índice único parcial `UNIQUE (license_plate) WHERE status = 'Active'` + consulta previa en el handler (ADR-0005). Infrastructure traduce cada violación por **nombre de restricción** al error de negocio correspondiente.

La validación previa sola **no** basta (check-then-act race).

### 2.4.1 Seguridad (ADR-0004)

- JWT Bearer emitido por `POST /api/auth/login`; expira en 60 min; rol `Operator` requerido para escribir.
- Contraseñas con `PasswordHasher<T>` (PBKDF2 + salt). Clave de firma y usuario de prueba en `dotnet user-secrets`.
- CORS solo para el origen del frontend; validación de entrada en el borde; ProblemDetails sin detalles internos.

### 2.5 Persistencia

- PostgreSQL 17 + Npgsql EF Core provider. Nombres `snake_case`.
- Fechas en `DateTimeOffset` UTC (`timestamptz`).
- Enums guardados como texto (legibles, robustos a reordenamientos).
- Seed: 30 parqueaderos `P01`…`P30` (algunos `OutOfService`) + operador de prueba (solo Development, credenciales desde user-secrets).
- BDs: `domonow_parking` (desarrollo) y `domonow_parking_tests` (integración). Sin SQLite/InMemory: no soportan `xmin` ni reproducen la concurrencia real.
- Consultas de lectura con `AsNoTracking()` y proyección directa a DTO.

## 3. Frontend (`/frontend`)

Mismo principio hexagonal, adaptado a Angular 22: standalone components, **zoneless**, signals, control flow nativo y **Signal Forms**. Reactividad en todo (CONVENTIONS §3).

```
frontend/src/app/
├── app.ts · app.routes.ts · app.config.ts   # shell (toolbar), rutas con guards, providers (zoneless, HTTP, es-CO)
├── core/                        # Transversal, se carga una vez
│   ├── auth/                    # session.store (signals + sessionStorage vía effect), auth-api,
│   │                            #   auth.interceptor (JWT solo hacia la API), auth.guard, login-page (ADR-0004)
│   ├── config/api-base-url.ts   # API base URL (environment)
│   ├── http/                    # api-error (ProblemDetails → ApiError en español) + api-error.interceptor (401 → login)
│   └── notifications/notifier.ts# Avisos (MatSnackBar)
├── features/
│   └── parking/
│       ├── domain/              # parking.models.ts (MISMO lenguaje ubicuo), license-plate.ts (espejo de UX del VO),
│       │                        #   parking.repository.ts = PUERTO (abstract class)
│       ├── application/         # parking.store.ts: estado en signals, computed (contadores, filtro),
│       │                        #   polling RxJS visible-aware → signals, checkIn/checkOut
│       ├── infrastructure/      # ADAPTADOR: parking-http.repository.ts + parking-api.dto.ts + parking.mapper.ts
│       └── ui/
│           ├── pages/parking-board-page.*       # contenedor: provee ParkingStore, abre diálogos
│           ├── dialogs/check-in-dialog.*        # Signal Forms + vista previa reactiva de la placa
│           └── components/ spot-card · status-filter · active-vehicles-table   # presentacionales
└── shared/                      # format-elapsed ("hace 2 h 5 min"), confirm-dialog
```

- **Puerto ↔ adaptador** se enlazan en los `providers` de la ruta del tablero: `{ provide: ParkingRepository, useClass: ParkingHttpRepository }` → en tests se sustituye por un fake.
- **Estado**: `ParkingStore` se provee a nivel de página (vive y hace polling solo mientras la página existe). Polling = `fromEvent(visibilitychange) → switchMap(timer(0, 30 s))` + refrescos manuales, todo con `switchMap` (una respuesta vieja nunca pisa una nueva) y `takeUntilDestroyed`.
- **Errores**: el interceptor convierte la respuesta en `ApiError`; un 401 cierra la sesión y lleva a `/login?reason=expired`. Si una recarga falla, el store conserva el último tablero y expone `loadError` (banner + "Reintentar").
- **Concurrencia en la UI**: `busySpotIds` deshabilita la acción de ese parqueadero mientras hay una petición en vuelo; tras éxito o error (p. ej. 409) se refresca el tablero.
- **Smart vs dumb**: solo la página y el diálogo usan el store; tarjeta, filtro y tabla son presentacionales (`input()`/`output()`/`model()`, OnPush).

## 4. Lo que deliberadamente NO hacemos (y por qué)

| Descartado | Motivo |
|------------|--------|
| MediatR | Indirección sin beneficio en 4 casos de uso; además cambió a licencia comercial |
| AutoMapper | Mapeos triviales; manuales son explícitos y verificados por el compilador |
| Repositorio genérico | Oculta EF Core sin aportar; en DDD hay un repositorio por agregado |
| Domain events + dispatcher | Sin consumidores hoy; se proponen para la evolución a escala (outbox) |
| Reglas de negocio en el frontend | El frontend solo refleja estados; la autoridad es el agregado del backend |
| NgRx | Un store con signals es suficiente para una pantalla |
| Microservicios | Se discuten solo en "Evolución de la solución" del README |
