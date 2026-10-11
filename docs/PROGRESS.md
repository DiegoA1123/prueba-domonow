# PROGRESS — Bitácora y estado del proyecto

> **Leer esto primero en cada sesión.** Se actualiza después de cada paso significativo.
> Formato de entrada: fecha · qué · archivos · decisiones · verificación · siguiente paso.

## Estado actual

**Fase**: 0 — Definición de forma de trabajo (sin proyectos creados)
**Último paso**: DDD incorporado (ADR-0003 + `docs/DOMAIN.md`) y documentos alineados.
**Siguiente paso**: Configurar identidad Git + primer commit → resolver decisiones pendientes → Fase 1 (backend).

## Tablero

| Fase | Tarea | Estado |
|------|-------|--------|
| 0 | Entorno: .NET 10, Git, nvm + Node 24, Angular CLI 22, PostgreSQL 17 | ✅ |
| 0 | Plugins de Claude Code (superpowers, feature-dev, pr-review-toolkit, commit-commands, frontend-design) | ✅ |
| 0 | Carpeta, docs de trabajo, convenciones, ADR-0001/0002 | ✅ |
| 0 | DDD: ADR-0003, `docs/DOMAIN.md` (lenguaje ubicuo, agregado, invariantes) | ✅ |
| 0 | `git init -b main` | ✅ |
| 0 | Identidad Git (`user.name`/`user.email`) + primer commit | ⏳ |
| 1 | Backend: solución + 4 proyectos + 3 de tests | ⬜ |
| 1 | Dominio: ParkingSpot, ParkingAssignment, LicensePlate (TDD R1–R5) | ⬜ |
| 1 | Application: casos de uso CheckIn, CheckOut, GetSpots, GetActive | ⬜ |
| 1 | Infrastructure: DbContext, configuraciones, índice parcial, xmin, migración, seed | ⬜ |
| 1 | Api: endpoints, ProblemDetails, CORS, OpenAPI | ⬜ |
| 1 | Test de integración de concurrencia (R6) | ⬜ |
| 2 | Frontend: proyecto Angular + estructura hexagonal | ⬜ |
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

## Decisiones pendientes (confirmar con el usuario)

1. **Autenticación**: el enunciado dice "usuario autorizado". Propuesta: fuera de alcance, documentado como supuesto (o API key simple si sobra tiempo).
2. **Formato de placa**: propuesta `^[A-Z0-9]{5,7}$` tras normalizar (carros `ABC123`, motos `ABC12D`).
3. **Hora de salida**: propuesta — la pone el servidor (`TimeProvider`); el endpoint acepta `checkOutAt` opcional solo para validar R5.
4. **Tests de integración**: contra PostgreSQL local con BD de test dedicada (Testcontainers requiere Docker).

## Supuestos (irán al README)

- Una sola propiedad, 30 parqueaderos sembrados.
- "Cancelada" existe como estado pero no se expone endpoint de cancelación salvo que sobre tiempo.

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
