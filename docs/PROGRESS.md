# PROGRESS — Bitácora y estado del proyecto

> **Leer esto primero en cada sesión.** Se actualiza después de cada paso significativo.
> Formato de entrada: fecha · qué · archivos · decisiones · verificación · siguiente paso.

## Estado actual

**Fase**: ✅ **Entrega lista.** Backend, frontend, mejoras de UX, revisión final, predicción de demanda y docker-compose en `main` (PR #1–#10).
**Último paso**: PR #10 (docker-compose) fusionado y ramas fusionadas borradas; en el repositorio solo queda `main`. Verificación final: frontend 69/69, backend 82/82 (en contenedor), lint y build limpios.
**Siguiente paso**: dar acceso al evaluador (repositorio público o invitación como colaborador). Acción del usuario en GitHub; no requiere cambios en el código.

## Tablero

| Fase | Tarea | Estado |
|------|-------|--------|
| 0 | Entorno: .NET 10, Git, nvm + Node 24, Angular CLI 22, PostgreSQL 17 | ✅ |
| 0 | Plugins de Claude Code (superpowers, feature-dev, pr-review-toolkit, commit-commands, frontend-design) | ✅ |
| 0 | Carpeta, docs de trabajo, convenciones, ADR-0001/0002 | ✅ |
| 0 | DDD: ADR-0003, `docs/DOMAIN.md` (lenguaje ubicuo, agregado, invariantes) | ✅ |
| 0 | `git init -b main` | ✅ |
| 0 | Identidad Git (local: Diego Andres) + primer commit `d6d9997` | ✅ |
| 0 | Decisiones de diseño cerradas + ADR-0004 (JWT) + AI-USAGE inicial + README supuestos | ✅ |
| 0 | Revisión del usuario: límite del agregado (ADR-0003) — aprobado | ✅ |
| 1 | Plan de implementación backend (10 tasks) — `docs/plans/2026-10-10-backend-implementation.md` | ✅ ejecutado |
| 1 | R7: placa única activa (ADR-0005) | ✅ diseño |
| 1 | Backend: solución + 4 proyectos + 3 de tests | ✅ |
| 1 | Dominio: ParkingSpot, ParkingAssignment, LicensePlate (TDD R1–R5) | ✅ |
| 1 | Application: casos de uso CheckIn, CheckOut, GetSpots, GetActive | ✅ |
| 1 | Infrastructure: DbContext, configuraciones, índice parcial, xmin, migración, seed | ✅ |
| 1 | Api: endpoints, ProblemDetails, CORS, OpenAPI | ✅ |
| 1 | Autenticación: login JWT, operadores (hash), roles, user-secrets (ADR-0004) | ✅ |
| 1 | Test de integración de concurrencia (R6) | ✅ |
| 1 | Revisión final de la rama + merge a main (PR #1, PR #2) | ✅ |
| 2 | Frontend: proyecto Angular + estructura hexagonal | ✅ |
| 2 | Login + interceptor JWT + guard | ✅ |
| 2 | Pantalla "Parqueaderos de visitantes" + check-in + checkout + errores | ✅ |
| 2 | Tests frontend (store, mappers, componentes) — 69 | ✅ |
| 2 | Mejoras de UX detectadas por el ingeniero (PR #5) | ✅ |
| 2 | Revisión final + PR #3 (merge del usuario) | ✅ |
| 3 | AI Code Review + 3 hallazgos en AI-USAGE.md | ✅ |
| 3 | README completo + "Evolución de la solución" | ✅ |
| 3 | (Opcional) Desafío predicción de demanda | ✅ |
| 3 | (Opcional) docker-compose | ✅ |
| 3 | Entrega: repositorio con solo `main` (PR #1–#10 fusionados) | ✅ |
| 3 | Acceso del evaluador (público o colaborador) | ⏳ acción del usuario |

Leyenda: ✅ hecho · 🔄 en curso · ⏳ pendiente inmediato · ⬜ por hacer · ❌ descartado

## Decisiones tomadas

| Fecha | Decisión | Referencia |
|-------|----------|-----------|
| 2026-10-10 | PostgreSQL 17 nativo (Docker requiere WSL, no instalado) | — |
| 2026-10-10 | Clean + Hexagonal pragmático, 4 proyectos, sin MediatR/AutoMapper | ADR-0001 |
| 2026-10-10 | Concurrencia: índice único parcial + xmin → 409 | ADR-0002 |
| 2026-10-10 | Código/commits en inglés, docs en español | CLAUDE.md |
| 2026-10-10 | DDD: `ParkingSpot` Aggregate Root, `ParkingAssignment` entidad interna (solo activa cargada); repo por agregado; lecturas por proyección; sin domain events por ahora | ADR-0003 |

| 2026-10-10 | Auth: JWT + operadores con hash + rol `Operator` (usuario rechazó dejarla fuera de alcance) | ADR-0004 · AI-USAGE #3 |
| 2026-10-10 | Placa estricta Colombia: carro `AAA999`, moto `AAA99A` (usuario modificó propuesta laxa) | DOMAIN §4 · AI-USAGE #4 |
| 2026-10-10 | Hora de salida: servidor por defecto + `checkedOutAt` opcional validado (no anterior al ingreso ni futura) | DOMAIN §3 · AI-USAGE #5 |
| 2026-10-10 | Tests de integración contra PostgreSQL local, BD `domonow_parking_tests` | ARCHITECTURE §2.5 · AI-USAGE #6 |
| 2026-10-10 | R7 (regla del usuario): placa única activa; cruza agregados → handler + índice único parcial por placa → 409 | ADR-0005 · AI-USAGE #8 |
| 2026-10-10 | Consultas sin handler: los endpoints usan el puerto `IParkingReadModel` directamente; `IUnitOfWork.CommitAsync` devuelve `Result` | ARCHITECTURE §2.2 |
| 2026-10-10 | Mensajes de error en español, códigos en inglés; `ReturnToService` eliminado (YAGNI) | CONVENTIONS · DOMAIN |

| 2026-10-11 | Frontend: Angular Material; refresco tras cada acción + polling cada 30 s (pausado con la pestaña oculta); filtro por estado, lista de vehículos dentro y detalle en tarjeta ocupada; salida con hora del servidor (sin UI de hora tardía) | Usuario |
| 2026-10-11 | Regla del usuario: **reactividad en todo el frontend** (signals/computed, input/output, zoneless + OnPush, RxJS → signals, formularios reactivos) | CONVENTIONS §3 |

## Decisiones pendientes

Ninguna. (Límite del agregado aprobado por el usuario el 2026-10-10 → AI-USAGE #7.)

## Ideas para README § "Evolución de la solución" (se redactan al final)

- **Concurrencia**: mantener optimista (`xmin`) + índices parciales; la contención por parqueadero no crece con el número de propiedades, solo el volumen. `SELECT … FOR UPDATE` evaluado y descartado para asignación manual; usar `FOR UPDATE SKIP LOCKED` si se agrega "asignar automáticamente el primer libre" (muchas solicitudes compiten por las mismas filas). (AI-USAGE #10)
- **Rate limiting detrás de proxy**: `ForwardedHeaders` para particionar por IP real; a escala, rate limiting en el API gateway.
- **Multi-tenancy**: particionar por propiedad → la contención queda local a cada propiedad.
- **Mensajería**: domain events + outbox (ver ADR-0003, no implementados hoy).
- **Seguridad**: OIDC externo / refresh tokens (ver ADR-0004).

## Supuestos

Movidos y mantenidos en `README.md` § Supuestos (fuente única).

## Bitácora

### 2026-10-10 — Fase 0: preparación
- **Qué**: Instalación de entorno (nvm-windows 1.2.2, Node 24.21.0, Angular CLI 22.2.2, PostgreSQL 17.11). Git y .NET 10.0.401 ya existían.
- **Qué**: Creada carpeta del repo con `backend/`, `frontend/`, `docs/`; manual `CLAUDE.md`, `docs/REQUIREMENTS.md`, `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, ADR-0001, ADR-0002, `AI-USAGE.md` y `README.md` base; `.editorconfig`, `.gitignore`, `.gitattributes`, `backend/global.json`, `backend/Directory.Build.props`, `backend/Directory.Packages.props`, `frontend/.prettierrc`.
- **Decisiones**: ver tabla.
- **Verificación**: N/A (sin código aún).
- **Siguiente**: resolver decisiones pendientes → Fase 1.

### 2026-10-10 — Fase 0: incorporación de DDD
- **Qué**: A pedido del usuario, DDD para todo el proyecto. Nuevo `docs/adr/0003-domain-driven-design.md` y `docs/DOMAIN.md` (lenguaje ubicuo ES↔EN, bounded context `Parking`, mapa de contextos, agregado, invariantes R1–R5 mapeadas a métodos y errores, VOs, transiciones de estado).
- **Archivos actualizados**: `CLAUDE.md` (reglas DDD + tabla de reglas apuntando al agregado), `docs/ARCHITECTURE.md` (tabla DDD/Clean/Hexagonal, árbol Domain por agregado, repo por agregado + read model, flujo de CheckIn), `docs/CONVENTIONS.md` (sección Dominio DDD + DoD), ADR-0002 (referencia al agregado), `README.md`.
- **Decisión clave**: límite del agregado = `ParkingSpot` porque R1 y R3 son invariantes transaccionales entre parqueadero y asignación. Con eso, la versión del agregado (`xmin`) resuelve R6 y el índice parcial queda como red de seguridad.
- **Verificación**: N/A (documentación). Revisado que no queden referencias al diseño anterior (`Occupy`, `IParkingAssignmentRepository`).
- **Siguiente**: identidad Git + primer commit.

### 2026-10-10 — Fase 0: decisiones de diseño cerradas
- **Qué**: Git configurado localmente (Diego Andres) y primer commit `d6d9997`. Usuario respondió las 4 decisiones pendientes.
- **Archivos**: nuevo `docs/adr/0004-authentication-jwt.md`; actualizados `docs/DOMAIN.md` (placa estricta, R5 con opcional, término Operador, subdominio identidad), `docs/ARCHITECTURE.md` (Authentication/Identity en capas, core/auth en frontend, 401/403, BDs dev/test), `docs/CONVENTIONS.md` (seguridad), `CLAUDE.md`, `README.md` (stack de seguridad + supuestos), `AI-USAGE.md` (registro continuo #1–#7).
- **Decisiones**: ver tabla. Dos propuestas de la IA cambiaron por criterio del usuario (auth rechazada, placa modificada).
- **Verificación**: N/A (documentación).
- **Siguiente**: revisión del agregado por el usuario → plan de implementación backend.
- **Actualización**: el usuario aprobó el límite del agregado (`ParkingSpot` raíz, asignación interna) tras comparar con la alternativa de dos agregados. AI-USAGE #7 → Aceptada.

### 2026-10-10 — Fase 1: regla R7 y plan del backend
- **Qué**: Commit `b7ebcb2` de las decisiones. Usuario agregó R7 (una placa no ingresa dos veces sin salir). Nuevo ADR-0005; actualizados DOMAIN, CLAUDE, REQUIREMENTS, ARCHITECTURE (consultas sin handler, flujo CheckIn con R7, índice por placa), CONVENTIONS (mensajes en español).
- **Plan**: `docs/plans/2026-10-10-backend-implementation.md` — 10 tasks TDD con código completo: scaffold → kernel+VOs → agregado → casos de uso → login → persistencia → identidad/seed → API → tests de integración (incluye concurrencia R6/R7) → docs+merge.
- **Autorrevisión del plan**: corregidos datos de test compartidos (AAA444 provocaría 409 en vez de 422), test dependiente del orden, `using` faltante del rate limiter, captura de excepciones del UnitOfWork (filtros `when` por restricción), `!` → `ShouldNotBeNull()`. Registrado en AI-USAGE #9.
- **Verificación**: N/A (documentación).
- **Siguiente**: revisión del plan por el usuario + modo de ejecución.

### 2026-10-10 — Fase 1: backend implementado (rama `feat/backend`)
- **Qué**: Ejecutado el plan completo (10 tasks) con TDD: scaffold → kernel + VOs → agregado → casos de uso → login → persistencia → identidad/seed → API → tests de integración → docs.
- **Commits**: `100eaed` scaffold · `b661e5c` dominio VOs · `4cfd17e` agregado · `5ff9db2` casos de uso · `22f0cb0` login · `2adcd42` persistencia · `7a03245` identidad/seed · `43a04f0` API · `7ab4f55` fix EF · `a2a5a61` fix 400 · `c5822cc` tests de integración.
- **Bugs encontrados al verificar** (no los detectaron los tests unitarios):
  1. EF hacía `UPDATE` en lugar de `INSERT` para asignaciones nuevas (claves Guid asumidas como generadas por BD) → falso 409. Fix: `ValueGeneratedNever()`; migración inicial regenerada. AI-USAGE #11.
  2. JSON inválido → 500 en Development. Fix: `BadHttpRequestException` → 400. AI-USAGE #12.
  3. Tests de integración disparaban el rate limit del login (defecto del test, no del producto) → token cacheado por corrida. AI-USAGE #13.
- **Desviaciones del plan** (registradas en el ledger): manifiesto de herramientas en `backend/dotnet-tools.json` (.NET 10); `<param>` extra en `CheckOutCommand`; `ReadAsync<T> where T : class`; `ApiCollection`→`ApiTestGroup` (CA1711); tests extra: filtro por estado y JSON inválido.
- **Secretos**: cadena de conexión, clave JWT y operador de prueba en `dotnet user-secrets` (Api y tests); contraseña del operador de prueba en `postgres-dev-credentials.txt` (ignorado por Git).
- **Verificación**: `dotnet build` → 0 advertencias, 0 errores. `dotnet test` → 42/42 + 14/14 + 18/18 = **74/74**. Tests de concurrencia 5/5 corridas estables. Prueba de humo manual: 401 sin token, login, 30 parqueaderos con P04/P17 fuera de servicio.
- **Siguiente**: revisión final independiente → merge con aprobación del usuario → frontend.

### 2026-10-10 — Fase 1: revisión final independiente del backend
- **Qué**: Agente revisor con contexto nuevo (Opus) revisó los 12 commits contra plan, spec y Review Focus. Resultado: 0 críticos, 4 importantes, 4 menores. Arquitectura y concurrencia (R1/R6/R7) validadas sin cambios.
- **Corregidos con TDD** (commits `6a33088`, `d33773d`, `9dfccdf`):
  1. `checkedOutAt` con offset (-05:00) → 500 (Npgsql exige UTC). Fix: normalizar a UTC en el dominio.
  2. Rate limit de login global → un cliente bloqueaba a todos. Fix: partición por IP (test verificado en rojo contra el código anterior).
  3. Faltaban tests 401/403 en escritura → 4 tests nuevos (token ausente, otra firma, vencido, sin rol).
  4. Salida concurrente devolvía el mensaje de check-in → nuevo error neutro `Parking.ConcurrentModification`.
- **Diferidos (menores)**: lectura en 2 consultas, validación de `?status=` y `parkingSpotId`, HTTPS/HSTS, test de doble salida vía API → README § Mejoras.
- **Verificación**: `dotnet build` 0 advertencias; `dotnet test` **82/82** (43 + 14 + 25).
- **Siguiente**: merge a `main` con aprobación del usuario.

### 2026-10-11 — Repositorio remoto y Pull Request del backend
- **Qué**: GitHub CLI 2.102.0 instalado; sesión del usuario (`DiegoA1123`) iniciada por él con `gh auth login --web`. Repositorio **privado** creado: https://github.com/DiegoA1123/prueba-domonow. Subidas `main` y `feat/backend`.
- **PR**: https://github.com/DiegoA1123/prueba-domonow/pull/1 (`feat/backend` → `main`), estado MERGEABLE. Sin CI configurado (no hay checks).
- **Verificación**: escaneo previo al push — ninguna contraseña local presente en archivos versionados ni en el historial.
- **Siguiente**: el usuario revisa y aprueba el merge del PR → Fase 2 (frontend).

### 2026-10-11 — Corrección: user-secrets invisibles para Visual Studio (rama `fix/api-dev-user-secrets`)
- **Síntoma**: con F5 (perfil `http`) la API lanzaba "Falta la cadena de conexión 'ConnectionStrings:Parking'"; desde la terminal de Claude funcionaba.
- **Causa raíz**: los secretos se habían configurado desde el sandbox de Claude, que virtualiza `AppData`; el `secrets.json` no existía en el disco real. Diagnóstico temporal en `Program.cs` → `SecretsPath=…secrets.json Exists=False` bajo Visual Studio.
- **Primera hipótesis descartada**: registrar los secretos con `AddUserSecrets<Program>()` (no resolvió; revertido).
- **Corrección**: `scripts/setup-dev-secrets.ps1` (pide o lee contraseñas del archivo local ignorado, genera clave JWT, configura API y tests) ejecutado en la terminal real del usuario; mensaje de error de `AddInfrastructure` más explicativo; README § Configurar la base de datos y CLAUDE.md § Entorno actualizados.
- **Verificación**: el usuario confirmó que la API arranca en Visual Studio. `dotnet build` 0 advertencias; `dotnet test` 82/82.
- **Nota de entorno**: ExecutionPolicy de PowerShell del usuario en Restricted → usar `npm.cmd`/`ng.cmd` o que el usuario configure `RemoteSigned` para su cuenta.
- **Siguiente**: merge del PR de esta corrección → retomar `feat/frontend` (rebase sobre `main`) y ejecutar el plan del frontend.

### 2026-10-11 — Fase 2: frontend implementado (rama `feat/frontend`)
- **Qué**: Tasks 1–9 del plan del frontend ejecutadas con TDD: scaffold Angular 22 + Material + ESLint, modelo de errores, sesión/interceptores/guards, login con Signal Forms, dominio + adaptador HTTP, `ParkingStore` reactivo (polling 30 s pausado con pestaña oculta), componentes, diálogo de ingreso y tablero.
- **Verificación real en el navegador**: 7 de 8 flujos OK (login, filtro, ingreso con vista previa, placa repetida, salida, sesión vencida). Flujo 8 (apagar la API) no se ejecutó porque la API en 5080 era un proceso ajeno; cubierto por test del store. Encontrado y corregido 1 bug de UX (error del diálogo fuera de la vista). Capturas en `docs/screenshots/`.
- **Verificación**: `npm test` → **52/52**, lint limpio, build OK. Backend sin cambios (82/82).
- **Docs**: README (ejecutar frontend, tests, capturas), ARCHITECTURE §3, CLAUDE.md §5, AI-USAGE #16–#17.
- **Pendiente** (Task 10): revisión final independiente de la rama, push de `feat/frontend` y PR; luego README § "Evolución de la solución", "Qué mejoraría" y sección AI Code Review / casos seleccionados de AI-USAGE.

### 2026-10-11 — Fase 2: revisión final independiente del frontend
- **Qué**: Agente revisor (Opus, contexto nuevo) revisó `feat/frontend` contra plan, spec y Review Focus: 0 críticos, 3 importantes, 7 menores. Arquitectura, reactividad (CONVENTIONS §3) y manejo de errores validados.
- **Corregidos con TDD** (`8a629d7`, `3ea2970`): diálogos que quedaban encima del login tras un 401 (`closeAll` al destruir la página); timeout de 10 s si la API se cuelga; una sola acción por parqueadero aunque el botón no se haya deshabilitado; sin "No hay vehículos dentro" cuando la primera carga falla; tests de refresco tras error y de `checkOut`.
- **Diferidos (menores)**: README § Mejoras (frontend). AI-USAGE #18.
- **Verificación**: `npm test` **58/58**, lint limpio, build OK.
- **Siguiente**: paso 2 — push de `feat/frontend` y PR #3.

### 2026-10-11 — Fase 2: Pull Request del frontend
- **Qué**: `feat/frontend` subida a GitHub; PR https://github.com/DiegoA1123/prueba-domonow/pull/3 (`feat/frontend` → `main`). Tablero de estado puesto al día (fases 1 y 2).
- **Verificación**: `npm test` 58/58 sobre el árbol subido. Sin CI configurado.
- **Siguiente**: el usuario revisa y hace merge → README § "Evolución de la solución" → AI-USAGE final.

### 2026-10-11 — Fase 3: README § "Evolución de la solución" (rama `docs/final-deliverables`)
- **Qué**: respuesta a la pregunta 15 del enunciado, cubriendo los 10 temas pedidos (escalabilidad, concurrencia, caché, BD, microservicios, observabilidad, mensajería, seguridad, multi-tenancy, cloud) en una tabla qué/por qué, más el orden de implementación.
- **Razonamiento central**: 500.000 movimientos/día ≈ 6 escrituras/s (≈60/s en pico); lo que crece son las lecturas por polling (~330 req/s con 10.000 pantallas) → push en vez de polling; la contención sigue siendo por parqueadero → se mantiene ADR-0002; monolito modular, no microservicios.
- **Verificado contra el código** antes de afirmarlo: `code` de parqueadero y placa activa son únicos globalmente (índices actuales) y la asignación no guarda el operador → multi-tenancy y auditoría como primeros cambios.
- **Siguiente**: AI-USAGE final.
- **Revisión del usuario** (opciones propuestas por la IA, elegidas por él): push con **SignalR** (grupo por propiedad, Azure SignalR Service como backplane); multi-tenancy **híbrido** (base compartida + RLS, base propia para propiedades grandes o por contrato); microservicios **a futuro** con orden y criterio de separación (*strangler*); **analítica fuera de la base principal** vía CDC; **portería sin conexión** (PWA + cola local); **sin caché distribuida del tablero** (Redis eliminado). Descartadas: SSE, reintentos idempotentes como tema propio, tablero precalculado, cuotas por tenant, resiliencia, pruebas de carga, versionado, Habeas Data, LPR, pre-registro, alertas.

### 2026-10-11 — Fase 3: AI-USAGE final (rama `docs/final-deliverables`)
- **Qué**: §1 Casos seleccionados con 5 casos del registro (#3 rechazada, #10 cuestionada y aceptada, #11 y #15 errores de la IA corregidos, #19 modificada por el ingeniero). §2 AI Code Review con 3 hallazgos de las revisiones independientes (#14 y #18): rate limit global (Sí), diálogos sobre el login tras 401 (Sí), doble acción por parqueadero (Parcialmente: severidad subida). Registro #19 añadido. Se corrigió la nota de §2, que nombraba herramientas que no se usaron.
- **Verificación**: commits, nombres de tests y cifras citados comprobados contra `git log` y el código.
- **Siguiente**: revisión del usuario → PR.

### 2026-10-11 — Fase 2: mejoras de UX detectadas por el ingeniero (rama `feat/frontend-ux`)
- **Origen**: el ingeniero usó la app y encontró problemas que ni la IA ni las dos revisiones independientes habían señalado: había que recorrer las tarjetas para encontrar un vehículo, el botón "Actualizar" sobraba con el refresco automático y "Unidad" confunde en Colombia, donde se entiende como el conjunto residencial.
- **Qué**: búsqueda por código, placa (sin importar guiones o espacios), visitante (sin mayúsculas ni tildes) o apartamento, combinada con el filtro por estado (`domain/spot-search.ts` + `computed` del store + `spot-search-field`); se quita "Actualizar" y se mantiene "Reintentar" en error; etiquetas "Apto/casa" y "Apartamento o casa que visita" con ejemplo.
- **Decisión**: el ingeniero propuso que la "unidad" llegara implícita según el portero. Aclarado el lenguaje: el enunciado llama **propiedad** al conjunto y **unidad** al apartamento ("Unidad o apartamento que visita"). La propiedad ya es implícita (una sola; con multi-tenancy saldría del token, README § Evolución). Se cambian solo los textos de pantalla y se conserva `DestinationUnit` en dominio y API.
- **Verificación**: TDD (11 tests nuevos; los de UI vistos en rojo), `npm test` 69/69, lint limpio, build OK. Probado en el navegador por el ingeniero (la IA no pudo iniciar sesión: la lectura de la contraseña de prueba quedó bloqueada por permisos).
- **Siguiente**: merge del PR #5.

### 2026-10-11 — Fase 3: caso de usabilidad en AI-USAGE y `angular.json` (rama `docs/ai-usage-operator-case`)
- **Qué**: el registro #20 (mejoras de UX detectadas por el ingeniero) reemplaza al caso 5 de §1 Casos seleccionados, decisión del ingeniero. El enunciado pide 3–5 casos y el ingeniero eligió no pasarse; la sección de evolución sigue en el registro #19. `frontend/angular.json`: `"analytics": false` (cambio hecho por el ingeniero en otra sesión, ajustado al formato del archivo), para que el evaluador no reciba la pregunta de analytics de Angular CLI.
- **Verificación**: `npm test` 69/69, `npm run build` OK.
- **Siguiente**: merge del PR (usuario) → revisión final de la entrega desde cero.

### 2026-10-11 — Fase 3: revisión final de la entrega (rama `docs/final-review`)
- **Verificación sobre `main`**: frontend `npm test` 69/69, lint, `format:check` y build limpios; backend unitarias 43 + 14 OK. **Clon limpio desde GitHub**: backend compila con 0 advertencias; frontend `npm ci` + 69/69 + build OK (no depende de archivos locales). Sin secretos versionados (`postgres-dev-credentials.txt` ignorado; `appsettings*.json` sin contraseñas ni claves).
- **Integración (25) no ejecutada**: Smart App Control de Windows (estado 1, activado) bloqueó `DomoNow.Parking.Api.dll` al cargarla `testhost.exe` (error 0x800711C7, registrado en el log CodeIntegrity). Es configuración de seguridad del sistema; no se tocó. El backend no cambió desde la última corrida 82/82 (salvo una sangría idéntica a la original).
- **README**: quitada la mención al botón "Actualizar" en Mejoras; Angular CLI global no es requisito; `DestinationUnit` = apartamento o casa. **Capturas** retomadas (01–04) con la búsqueda y "Apto/casa", y una nueva `06-busqueda-sin-tildes.jpg`; 05 sigue vigente. Los datos de prueba usados se retiraron (0 ocupados al terminar).
- **Observaciones visuales sin corregir**: la etiqueta flotante del primer campo del diálogo de ingreso queda recortada arriba cuando no hay mensaje de error; en la vista "Todos" las tarjetas disponibles se estiran a la altura de una ocupada de la misma fila.
- **Siguiente**: merge del PR (usuario) → dar acceso al evaluador.

### 2026-10-11 — Fase 3: detalles visuales de la revisión final (rama `fix/board-visual-polish`)
- **Etiqueta recortada en el diálogo de ingreso**: causa confirmada en el navegador: la regla de Material `.mat-mdc-dialog-title + .mat-mdc-dialog-content` anulaba el `padding-top` del componente (0 px); la etiqueta flotante quedaba 7 px por encima del borde del contenido con scroll. Con error del backend no pasaba porque el mensaje separa título y contenido. Corrección: selector `.check-in.mat-mdc-dialog-content` (más específico). Verificado: padding 8 px y etiqueta dentro del contenido.
- **Tarjetas estiradas**: `align-items: start` en la grilla del tablero; una tarjeta disponible ya no toma la altura de una ocupada de la misma fila. Verificado en el navegador con un ingreso de prueba (retirado después).
- **Sin test automatizado**: son defectos de layout que jsdom no calcula; la verificación fue visual y por estilos computados en el navegador.
- **Verificación**: `npm test` 69/69, lint limpio, build OK.

### 2026-10-12 — Fase 3: desafío adicional de predicción de demanda (rama `docs/demand-forecast`)
- **Qué**: `docs/PREDICCION-DEMANDA.md` (~1 página, unas 650 palabras) con los 8 puntos del §16: datos, variables, algoritmo, entrenamiento, medición, integración .NET/Angular, producción y degradación. Enlazado desde el README (sección nueva antes de "Uso de IA").
- **Decisiones del ingeniero**: documento aparte; entrenamiento en Python + exportación ONNX (predicción por lotes, la API solo lee `demand_forecasts`); incluir el evento "sin cupo" (la ocupación topada oculta la demanda real) y las reservas de zonas comunes como señal. AI-USAGE #21.
- **Aportes del ingeniero tras revisar**: vehículos que pernoctan o llevan días dentro (reducen la capacidad del día siguiente), detalle de los eventos en salones sociales (apartamento, horario, invitados) y promedio de eventos por semana. AI-USAGE #22.
- **Verificación**: documentación únicamente; enlaces del README revisados.
- **Siguiente**: merge del PR → acceso del evaluador.

### 2026-10-12 — Fase 3: docker-compose (rama `feat/docker-compose`)
- **Hallazgo previo**: Docker Desktop 4.94 **ya estaba instalado** (por usuario, en `%LOCALAPPDATA%\Programs\DockerDesktop`, motor sobre WSL2). Las comprobaciones anteriores corrían en el sandbox de Claude, que no lo veía; `winget` en la terminal del usuario lo encontró. Solo había que iniciarlo. AI-USAGE #23.
- **Qué**: `backend/Dockerfile` (etapas build → tests → runtime; contexto la raíz por el `.editorconfig`), `frontend/Dockerfile` (build de Angular + nginx con fallback de SPA), `docker-compose.yml` (PostgreSQL 17 sin puerto publicado, API en 5080 con `Production` y variables de entorno, web en 4200, servicio `tests` en el perfil `tests`), `.env.example` con valores locales de ejemplo (`.env` ignorado), `.dockerignore`. README § "Opción rápida: todo con Docker". Sin cambios de código.
- **Verificación** (terminal del usuario): `docker compose up --build` → base sana, migración aplicada, API en Production; login + `GET /api/parking-spots` (30 parqueaderos); web 200 en `/` y `/login`; preflight CORS 204. `docker compose run --rm tests` → **82/82** (primera corrida de integración desde el bloqueo de Smart App Control). Login y tablero en el navegador contra el stack de Docker.

### 2026-10-12 — Entrega lista
- **Estado**: PR #1–#10 fusionados por el usuario; ramas fusionadas borradas en local y en GitHub (solo queda `main`).
- **Entregables del enunciado**: `/backend`, `/frontend`, `README.md` (ejecución con y sin Docker, base de datos, arquitectura, supuestos, mejoras, evolución), `AI-USAGE.md` (5 casos + AI Code Review con 3 hallazgos + registro de 23 entradas). Opcionales: `docs/PREDICCION-DEMANDA.md` y `docker-compose.yml`.
- **Última verificación**: frontend 69/69, lint, formato y build; backend 82/82 con `docker compose run --rm tests`; clon limpio desde GitHub compila y pasa las pruebas del frontend.
- **Pendiente (usuario)**: dar acceso al evaluador.
