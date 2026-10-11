# Convenciones de código y buenas prácticas

Principios guía: **Clean Code, SOLID, KISS, YAGNI**. Ante la duda: lo más simple que cumpla la regla de negocio y sea fácil de probar.
Las reglas automatizables están en `.editorconfig`, `backend/Directory.Build.props` y `frontend/.prettierrc`; el build falla ante warnings.

## 1. Generales

- Nombres que revelan intención; sin abreviaturas (`assignment`, no `asg`). Sin números mágicos ni strings mágicos.
- Funciones cortas, un solo nivel de abstracción, preferir *early return* a `if` anidados.
- Comentarios solo para el **porqué**, nunca para el qué. Código muerto o comentado → se borra.
- Sin duplicación de reglas de negocio: una regla vive en un único lugar (el dominio).
- Inglés en código y commits; español en documentación.

## 2. C# / .NET

### Nombres
| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Tipos, métodos, propiedades, constantes | PascalCase | `ParkingSpot`, `CheckOut()`, `MaxPlateLength` |
| Interfaces | `I` + PascalCase | `IParkingSpotRepository` |
| Parámetros y locales | camelCase | `licensePlate` |
| Campos privados | `_camelCase` | `_repository` |
| Métodos asíncronos | sufijo `Async` | `GetByIdAsync` |
| Tests | `Method_Scenario_ExpectedResult` | `CheckIn_WhenSpotIsOutOfService_ReturnsBusinessRuleError` |

### Estilo
- `namespace` con ámbito de archivo; un tipo público por archivo; nombre de archivo = nombre del tipo.
- `sealed` por defecto en clases que no se diseñan para herencia.
- Nullable reference types habilitado; nada de `!` (null-forgiving) sin comentario que lo justifique.
- `var` solo cuando el tipo es evidente por el lado derecho.
- Llaves siempre, incluso en `if` de una línea.
- `record` para DTOs/commands; `readonly record struct` o clases selladas para value objects.

### Dominio (DDD)
- **Lenguaje ubicuo**: los nombres de tipos, métodos, endpoints, columnas y modelos TS salen del glosario de `docs/DOMAIN.md`. Un término nuevo se agrega primero al glosario.
- **Agregados**: solo la raíz (`ParkingSpot`) es accesible desde fuera; las entidades internas (`ParkingAssignment`) se modifican únicamente a través de la raíz. Referencias a otros agregados/contextos solo por Id o por valor.
- **Un agregado por transacción**; un repositorio por agregado; el repositorio devuelve agregados completos y consistentes.
- **Nada de modelo anémico**: si una regla habla de estados o invariantes, es un método del agregado, no un `if` en el handler.
- Los handlers (application services) **orquestan**: obtienen el agregado, invocan un método de dominio, persisten. Si un handler tiene lógica condicional de negocio, está en la capa equivocada.
- Errores de dominio en un catálogo (`ParkingErrors`) con códigos estables `Parking.<Motivo>`.
- Entidades con **setters privados**; el estado cambia solo mediante métodos con nombre de negocio (`CheckIn`, `CheckOut`, `MarkOutOfService`).
- Constructores privados + *factory methods* (`ParkingAssignment.Create(...)`) que devuelven `Result<T>` cuando la creación puede fallar.
- Value objects validan en su creación: un `LicensePlate` existente siempre es válido.
- Prohibido `DateTime.Now`/`UtcNow` en dominio/aplicación: se recibe la hora (`TimeProvider`).

### Asincronía
- `async` de punta a punta; `CancellationToken` en todo método público asíncrono y propagado hasta EF Core.
- Nunca `.Result`, `.Wait()` ni `async void`.

### Errores
- Errores esperados → `Result<T>`. Excepciones solo para lo excepcional.
- Nunca `catch (Exception)` silencioso. Solo se capturan excepciones que se saben manejar (p. ej. `DbUpdateException` por violación de unicidad → `Conflict`).

### EF Core
- Configuración con `IEntityTypeConfiguration<T>` (nada de data annotations en dominio).
- Lecturas con `AsNoTracking()` y proyección a DTO. Sin *lazy loading*.
- Migraciones versionadas en el repo; nunca `EnsureCreated` en producción.

### Seguridad
- Validar toda entrada en el borde (longitudes máximas, formato de placa).
- Sin secretos en el repo: `dotnet user-secrets` / variables de entorno (clave JWT, credenciales del operador de prueba, cadena de conexión).
- Contraseñas siempre hasheadas (`PasswordHasher<T>`); jamás se registran en logs ni se devuelven en respuestas.
- Endpoints con `RequireAuthorization()` por defecto; los anónimos (`/api/auth/login`, health) se marcan explícitamente con `AllowAnonymous()`.
- CORS restringido al origen del frontend. ProblemDetails sin stack traces fuera de Development.
- Solo consultas parametrizadas (EF Core); prohibido SQL concatenado.

## 3. TypeScript / Angular

- `strict: true` + `strictTemplates`. Prohibido `any` (usar `unknown` y estrechar).
- Componentes **standalone**, `ChangeDetectionStrategy.OnPush`, `input()` / `output()` / `signal()` / `computed()`.
- Control flow nativo (`@if`, `@for` con `track`), nada de `*ngIf`/`*ngFor`.
- `inject()` en lugar de inyección por constructor.
- Archivos en kebab-case con sufijo de rol: `spot-card.component.ts`, `parking.store.ts`, `parking-http.repository.ts`.
- Componentes presentacionales sin servicios de datos; solo la página (contenedor) usa el store.
- Las respuestas HTTP se tipan con DTOs y se **mapean** a modelos de dominio en la capa `infrastructure`.
- Observables: sufijo `$`; desuscripción con `takeUntilDestroyed()` o `toSignal()`.
- Formateo: Prettier (`frontend/.prettierrc`). Lint: angular-eslint (se configura al crear el proyecto).

## 4. Pruebas

- Patrón **Arrange / Act / Assert**, una razón de fallo por test.
- Pirámide: muchos unitarios de dominio → pocos de aplicación → pocos de integración (concurrencia real contra PostgreSQL).
- Se prueba **comportamiento**, no implementación. Nada de mocks del dominio.
- Herramientas backend: xUnit + Shouldly + `FakeTimeProvider` (FluentAssertions v8 pasó a licencia comercial, por eso no se usa).
- Frontend: el runner por defecto de Angular CLI; tests del store con un `ParkingRepository` falso.

## 5. Git

- Rama principal `main`; trabajo en ramas `feat/…`, `fix/…`, `docs/…`, `test/…`, `chore/…`, integradas con merge `--no-ff`.
- **Conventional Commits** en inglés, modo imperativo, ≤ 72 caracteres en el asunto:
  - `feat(backend): add check-in use case with plate normalization`
  - `test(domain): cover out-of-service assignment rule`
  - `docs: record concurrency decision in ADR-0002`
- Commits pequeños y atómicos; cada commit compila y pasa los tests.
- Nunca commitear: `bin/`, `obj/`, `node_modules/`, secretos, credenciales locales.

## 6. Definition of Done (por tarea)

- [ ] Compila sin warnings (`dotnet build` / `ng build`).
- [ ] Tests nuevos/afectados en verde y ejecutados (evidencia citada).
- [ ] Respeta la regla de dependencia y estas convenciones.
- [ ] Usa el lenguaje ubicuo de `docs/DOMAIN.md`; las reglas de negocio están en el agregado, no en handlers/endpoints/componentes.
- [ ] `docs/PROGRESS.md` actualizado; ADR si hubo decisión; `AI-USAGE.md` si hubo decisión sobre una propuesta de IA.
