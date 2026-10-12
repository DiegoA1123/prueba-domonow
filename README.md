# DomoNow — Control de parqueaderos de visitantes

Solución Full Stack para la prueba técnica de OFIMA SAS: registrar el ingreso y la salida de vehículos de visitantes en los parqueaderos de una propiedad horizontal, garantizando que un parqueadero nunca tenga dos asignaciones activas.

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10 · ASP.NET Core (Minimal APIs) · EF Core · PostgreSQL 17 |
| Frontend | Angular 22 (zoneless) · TypeScript 6 strict · Signals · Signal Forms · Angular Material 22 · RxJS |
| Seguridad | JWT Bearer · PasswordHasher (PBKDF2) · user-secrets |
| Pruebas | xUnit · Shouldly · WebApplicationFactory + PostgreSQL real |

> 🚧 En construcción. Estado detallado en [`docs/PROGRESS.md`](docs/PROGRESS.md).

## Requisitos previos

- .NET SDK 10.0.401+
- Node.js 24 LTS (recomendado vía nvm) y Angular CLI 22
- PostgreSQL 17

## Configurar la base de datos

1. Tener PostgreSQL 17 corriendo en `localhost:5432`.
2. Configurar los secretos de desarrollo con `dotnet user-secrets` (no se versionan). La forma más simple es el script, que pide las contraseñas, genera una clave JWT aleatoria y configura también la base de las pruebas:

   ```bash
   powershell -ExecutionPolicy Bypass -File scripts/setup-dev-secrets.ps1
   ```

   O manualmente:

   ```bash
   cd backend
   dotnet user-secrets set "ConnectionStrings:Parking" "Host=localhost;Port=5432;Database=domonow_parking;Username=postgres;Password=<tu-password>" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "Jwt:SigningKey" "<clave aleatoria de 32+ bytes>" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "SeedOperator:Username" "portero" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "SeedOperator:Password" "<password del operador de prueba>" --project src/DomoNow.Parking.Api
   ```

   Una clave aleatoria se puede generar con `openssl rand -base64 48`.
3. Al arrancar en Development la API aplica las migraciones y siembra 30 parqueaderos (`P04` y `P17` fuera de servicio) y el operador de prueba. No hace falta crear la base de datos a mano.

> Los user-secrets solo se cargan con `ASPNETCORE_ENVIRONMENT=Development`. Ejecute con el perfil `http` o `https` (`dotnet run --launch-profile http`, o el selector de perfil de Visual Studio); ejecutar el `.exe` directamente arranca en Production y la API indicará que falta `ConnectionStrings:Parking`.

## Ejecutar el backend

```bash
dotnet run --project backend/src/DomoNow.Parking.Api --launch-profile http
```

- API en `http://localhost:5080`; documento OpenAPI en `http://localhost:5080/openapi/v1.json`.
- Obtener un token: `POST /api/auth/login` con `{ "username": "portero", "password": "<password>" }` y enviarlo como `Authorization: Bearer <token>`.

| Método | Ruta | Autorización | Descripción |
|--------|------|--------------|-------------|
| POST | `/api/auth/login` | Anónimo (máx. 10/min por IP) | Inicia sesión y devuelve un JWT |
| GET | `/api/parking-spots?status=Available` | Autenticado | Parqueaderos con estado, disponibilidad y vehículo actual; filtro opcional por estado |
| GET | `/api/parking-assignments/active` | Autenticado | Vehículos que están dentro de la propiedad |
| POST | `/api/parking-assignments` | Rol `Operator` | Registra un ingreso `{ parkingSpotId, licensePlate, visitorName, destinationUnit }` |
| PUT | `/api/parking-assignments/{id}/checkout` | Rol `Operator` | Registra la salida; cuerpo opcional `{ checkedOutAt }` |

Los errores siguen RFC 9457 (ProblemDetails) con un campo `code` estable (`Parking.SpotOccupied`, `Parking.VehicleAlreadyInside`…) y un mensaje en español: 400 validación · 401 sin sesión · 403 sin rol · 404 no encontrado · 409 conflicto · 422 regla de negocio.

## Ejecutar el frontend

Requiere la API corriendo en `http://localhost:5080` (su CORS ya permite `http://localhost:4200`).

```bash
cd frontend
npm install
npm start
```

Abrir `http://localhost:4200` e ingresar con el operador de prueba configurado en `SeedOperator` (usuario `portero`). En PowerShell con la política de ejecución por defecto, usar `npm.cmd` en lugar de `npm`.

| Comando | Qué hace |
|---------|----------|
| `npm start` | Servidor de desarrollo en `http://localhost:4200` |
| `npm test` | Pruebas unitarias y de componentes (Vitest + jsdom) |
| `npm run lint` | angular-eslint |
| `npm run build` | Build de producción en `frontend/dist/` |

**Qué hace la pantalla:** tarjetas por parqueadero con color por estado (verde disponible, rojo ocupado con placa, visitante, unidad y tiempo transcurrido, gris fuera de servicio), filtro por estado con contadores, registro de ingreso con vista previa de la placa normalizada, salida con confirmación y la tabla de vehículos dentro. Se actualiza sola después de cada acción y cada 30 s (se pausa si la pestaña no está visible). Capturas reales en [`docs/screenshots/`](docs/screenshots/).

| Tablero (filtro "Ocupados") | Placa ya dentro (error del backend) |
|---|---|
| ![Tablero](docs/screenshots/01-tablero-filtro-ocupados.jpg) | ![Error placa](docs/screenshots/03-ingreso-placa-ya-dentro.jpg) |

## Ejecutar las pruebas

```bash
dotnet user-secrets set "ConnectionStrings:ParkingTests" "Host=localhost;Port=5432;Database=domonow_parking_tests;Username=postgres;Password=<tu-password>" --project backend/tests/DomoNow.Parking.Api.IntegrationTests
dotnet test backend/DomoNow.Parking.slnx
```

| Proyecto | Qué prueba | Tests |
|----------|-----------|-------|
| `Domain.UnitTests` | Value objects y agregado `ParkingSpot`: reglas R1–R5 sin base de datos | 43 |
| `Application.UnitTests` | Casos de uso con puertos falsos: R7, errores de validación, login | 14 |
| `Api.IntegrationTests` | API completa contra PostgreSQL real: autenticación (401/403, token vencido o con otra firma, rate limit por IP), flujos, errores y **concurrencia** (20 ingresos simultáneos al mismo parqueadero → exactamente 1 éxito; misma placa en 2 parqueaderos a la vez → 1 éxito; 10 salidas simultáneas → 1 éxito) | 25 |

Las pruebas de integración recrean la base `domonow_parking_tests` en cada corrida. No se usan SQLite ni InMemory porque no reproducen `xmin`, los índices parciales ni la concurrencia real.

Frontend (`cd frontend && npm test`): **58 pruebas** (Vitest).

| Área | Qué prueba |
|------|-----------|
| `core/http` | Traducción de ProblemDetails a mensajes en español (sin conexión, 401, 429, genérico) |
| `core/auth` | Sesión en signals + `sessionStorage`, interceptor JWT solo hacia la API, 401 → login con aviso, página de login (Signal Forms) |
| `features/parking` | Reglas de placa, mapeo DTO → dominio, adaptador HTTP, **store reactivo** (contadores, filtro, polling cada 30 s pausado con la pestaña oculta, timeout de 10 s si la API se cuelga, último tablero conservado si la API falla, una sola acción por parqueadero), tarjeta, diálogo de ingreso, página del tablero (cierra diálogos al salir, sin estados vacíos engañosos) |

## Decisiones de arquitectura

Resumen en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md); decisiones formales en [`docs/adr/`](docs/adr/).

- **Domain-Driven Design**: `ParkingSpot` como Aggregate Root, lenguaje ubicuo en [`docs/DOMAIN.md`](docs/DOMAIN.md) (ADR-0003).
- **Clean Architecture + Hexagonal**, versión pragmática (ADR-0001).
- **Concurrencia** garantizada en la base de datos con índice único parcial + concurrencia optimista (ADR-0002).
- **Autenticación JWT** con operadores y roles; la identidad es un subdominio genérico fuera del dominio `Parking` (ADR-0004).
- **Regla adicional R7**: una placa no puede ingresar dos veces sin haber salido; como cruza agregados, se protege en el caso de uso y con un índice único parcial por placa (ADR-0005).

## Supuestos

- Una sola propiedad con 30 parqueaderos de visitantes (`P01`–`P30`), sembrados al crear la base de datos.
- Placas colombianas: carro `ABC123`, moto `ABC12D`. Se normalizan quitando espacios, guiones y puntos y pasando a mayúsculas. Placas extranjeras o diplomáticas no se aceptan en esta versión.
- La hora de salida la pone el servidor; opcionalmente el cliente puede indicar una hora pasada (registro tardío), que nunca puede ser anterior al ingreso ni futura.
- Las fechas se almacenan en UTC; el frontend las muestra en la zona horaria del navegador.
- Solo los operadores autenticados con rol `Operator` registran ingresos y salidas. Un operador de prueba se crea en Development con credenciales tomadas de `dotnet user-secrets`.
- El estado "Cancelada" existe en el modelo, pero no se expone endpoint de cancelación.
- `DestinationUnit` es texto libre (no se valida contra un maestro de unidades, que pertenece a otro contexto de DomoNow).

## Qué mejoraría con más tiempo

Backend (hallazgos menores de la revisión con IA, conscientemente diferidos):

- Leer parqueaderos y asignaciones activas en **una sola consulta** (hoy son dos; un ingreso entre ambas puede mostrar por un instante un parqueadero ocupado sin placa).
- Validar explícitamente el filtro `?status=` (hoy `?status=7` devuelve lista vacía) y exigir `parkingSpotId` en el cuerpo (hoy su ausencia da 404 en lugar de 400).
- HTTPS/HSTS fuera de Development, o documentar la terminación TLS en el proxy; detrás de un proxy, `ForwardedHeaders` para que el rate limit por IP vea la IP real.
- Refresh tokens y revocación; endpoint de cancelación de asignaciones; volver a poner en servicio un parqueadero.

Frontend (hallazgos menores de la revisión con IA, conscientemente diferidos):

- `disableClose` en los diálogos para que un doble clic en la tarjeta no cierre el diálogo recién abierto.
- Aplicar localmente el resultado de un ingreso/salida en vez de esperar la recarga (hoy la tarjeta muestra el estado anterior durante una petición).
- Indicador visual en "Actualizar"/"Reintentar" mientras se recarga.
- Validar nombre y unidad sobre el texto recortado (hoy "   " pasa en el cliente y el backend responde 400, que sí se muestra).
- Comparar el origen completo de la URL en el interceptor JWT en lugar de `startsWith`.

## Evolución de la solución

> ¿Qué cambiaría si DomoNow manejara 5.000 propiedades y cientos de miles de movimientos diarios?

**Primero, los números.** 500.000 movimientos al día son ~6 escrituras por segundo de promedio y, suponiendo picos de 10× en horas de visita, ~60/s. Un PostgreSQL bien indexado lo resuelve sin esfuerzo. La carga que de verdad crece son las **lecturas**: si cada propiedad tiene 2 porterías con el tablero abierto, 10.000 pantallas consultando cada 30 s son ~330 peticiones/s de tablero, con dos consultas cada una. Por eso la evolución empieza por aislar propiedades y cambiar cómo se refresca la pantalla, no por partir el sistema.

**Lo que no cambiaría:** el modelo de dominio y la estrategia de concurrencia. La contención es por parqueadero (dos porteros sobre el mismo `P07`), y eso no crece con el número de propiedades; solo crece el volumen total.

| Tema | Qué haría | Por qué |
|------|-----------|---------|
| **Multi-tenancy** | Agregar `property_id` a parqueaderos y asignaciones. Los índices únicos pasan a `(property_id, code)` y `(property_id, license_plate) WHERE status = 'Active'`. La propiedad sale del token, nunca de la URL. Filtro global de EF Core por tenant y **Row-Level Security** en PostgreSQL como segunda barrera. | Hoy `P01` y la placa activa son únicos en toda la base. Con 5.000 propiedades, una placa puede estar en dos conjuntos distintos el mismo día, y un error de filtro no puede mostrar los vehículos de otra propiedad. |
| **Bases de datos** | Un PostgreSQL gestionado con alta disponibilidad y réplica de lectura. `parking_assignments` particionada por mes (el histórico crece ~15 M filas/mes; las activas son pocas y están cubiertas por el índice parcial). Archivo del histórico frío. Pool de conexiones (PgBouncer). Sharding por propiedad (p. ej. Citus) solo si un único primario se queda corto, y la clave ya estaría lista: `property_id`. | El volumen cabe en un nodo. Partir por tiempo mantiene rápidas las consultas y el mantenimiento; partir por propiedad es caro y hoy no hace falta. |
| **Concurrencia** | Se mantiene: versión del agregado (`xmin`) + índices únicos parciales → 409 (ADR-0002). Si se agrega "asignar el primer parqueadero libre", ahí sí `SELECT … FOR UPDATE SKIP LOCKED`, porque muchas solicitudes competirían por las mismas filas. | La base es el árbitro, así que funciona igual con 1 o con 50 instancias de la API. |
| **Escalabilidad** | API sin estado, escalado horizontal. En el frontend, reemplazar el polling por **push** (SignalR o Server-Sent Events, con backplane en Redis): cada ingreso o salida notifica solo a las pantallas de esa propiedad. Mientras exista polling, `ETag` → `304 Not Modified`. | Elimina la mayor parte de las ~330 peticiones/s de lectura, y el portero ve el cambio al instante en vez de hasta 30 s después. |
| **Caché** | Tablero por propiedad en Redis (clave `board:{propertyId}`), invalidado por el evento de ingreso o salida, no por tiempo. Las escrituras **nunca** consultan la caché: la regla "un vehículo por parqueadero" la decide la base. | Una caché con TTL podría mostrar libre un parqueadero ocupado. Usarla solo para lectura, con invalidación por evento, evita ese error. |
| **Mensajería** | Domain events (`VehicleCheckedIn`, `VehicleCheckedOut`) guardados con **outbox** en la misma transacción y publicados a un broker (Azure Service Bus o RabbitMQ). Consumidores idempotentes. | Notificar al residente, alimentar reportes y predicción de demanda o cobrar por tiempo sin hacer más lenta la transacción del portero ni perder eventos si el broker falla. Hoy no se implementan porque nadie los consume (ADR-0003). |
| **Microservicios** | **No** al inicio: monolito modular, con `Parking` como módulo. Separaría solo lo que ya es otro contexto: identidad (proveedor OIDC) y analítica/reportes, alimentada por eventos. | Partir el registro de ingresos en servicios no lo hace más rápido y sí agrega red, consistencia eventual y despliegues coordinados. Se separa cuando un equipo o una carga lo exija. |
| **Seguridad** | OIDC centralizado (identidad de DomoNow) con refresh tokens y revocación; claim de propiedad y roles en el token. Rate limiting en el API Gateway con la IP real (hoy es por IP en la API). Secretos en Key Vault. **Auditoría**: guardar qué operador registró cada ingreso y salida (hoy no se guarda). | A esta escala, el riesgo principal es que un tenant vea datos de otro, y la auditoría es la evidencia ante reclamos de residentes. |
| **Observabilidad** | OpenTelemetry (trazas, métricas, logs estructurados) con `property_id` y correlation id en cada señal. Métricas de negocio: tasa de 409 (contención), latencia de ingreso, ocupación por propiedad, profundidad del outbox. Alertas por SLO, no por CPU. | Con 5.000 propiedades, "algo falla" no basta: hay que saber cuál, desde cuándo y si es un error o simple contención. |
| **Cloud** | Contenedores de la API en Azure Container Apps o AKS con autoescalado; PostgreSQL, Redis y broker gestionados; el frontend Angular como estático detrás de CDN; backups con PITR e infraestructura como código. Una región cercana (Colombia) con réplica para recuperación, sin multi-región activo al inicio. | Servicios gestionados para lo que no es el negocio, y costo proporcional a la carga real. |

**Orden en que lo haría:** 1) multi-tenancy y auditoría (cambian el esquema, mejor antes que después); 2) observabilidad; 3) push en lugar de polling; 4) outbox y eventos cuando aparezca el primer consumidor; 5) particionado y réplica cuando las métricas lo pidan.

## Uso de IA

Ver [`AI-USAGE.md`](AI-USAGE.md).
