# PROGRESS — Bitácora y estado del proyecto

> **Leer esto primero en cada sesión.** Se actualiza después de cada paso significativo.
> Formato de entrada: fecha · qué · archivos · decisiones · verificación · siguiente paso.

## Estado actual

**Fase**: 1 — Backend implementado (rama `feat/backend`), pendiente revisión final y merge
**Último paso**: Plan de backend ejecutado (10 tasks, TDD). Revisión final independiente hecha: 4 hallazgos importantes corregidos con TDD. **82/82 tests** (43 dominio, 14 aplicación, 25 integración contra PostgreSQL).
**Siguiente paso**: Revisión y merge del PR #1 (https://github.com/DiegoA1123/prueba-domonow/pull/1) → Fase 2 (frontend).

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
| 1 | Revisión final de la rama + merge a main | ⏳ |
| 2 | Frontend: proyecto Angular + estructura hexagonal | ⬜ |
| 2 | Login + interceptor JWT + guard | ⬜ |
| 2 | Pantalla "Parqueaderos de visitantes" + check-in + checkout + errores | ⬜ |
| 2 | Tests frontend (store, mappers) | ⬜ |
| 3 | AI Code Review + 3 hallazgos en AI-USAGE.md | ⬜ |
| 3 | README completo + "Evolución de la solución" | ⬜ |
| 3 | (Opcional) Desafío predicción de demanda · docker-compose | ⬜ |

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
