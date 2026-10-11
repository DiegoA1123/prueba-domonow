# ADR-0001: Clean Architecture + Hexagonal, versión pragmática

- **Estado**: Aceptado
- **Fecha**: 2026-10-10

## Contexto

DomoNow crecerá a cientos de funcionalidades (enunciado §5), pero la prueba evalúa explícitamente la **capacidad de evitar sobreingeniería**. El usuario definió Clean Architecture + Hexagonal como estilo para todo el sistema.

## Decisión

- 4 proyectos backend: `Domain`, `Application`, `Infrastructure`, `Api`. La regla de dependencia se hace cumplir con las referencias entre proyectos (el compilador impide que `Domain` use EF Core).
- Puertos de salida como interfaces en `Application`; adaptadores en `Infrastructure`.
- Puertos de entrada = handlers concretos por caso de uso, organizados por *slice* (`ParkingAssignments/CheckIn/…`). **Sin MediatR**.
- Errores de negocio con `Result<T>`, no excepciones.
- Frontend: misma idea por feature (`domain` / `application` / `infrastructure` / `ui`), puerto como `abstract class` usada como token de DI.

## Alternativas consideradas

| Opción | Por qué no |
|--------|-----------|
| Un solo proyecto con carpetas | Más simple, pero la regla de dependencia no la garantiza el compilador |
| Vertical Slice puro | Bueno para crecer, pero mezcla persistencia y negocio en cada slice; se toma solo la organización por slice dentro de `Application` |
| MediatR + pipeline behaviors | Indirección extra para 4 casos de uso; licencia comercial desde 2025 |
| Repositorio genérico `IRepository<T>` | Expone CRUD que el dominio no necesita; los puertos se diseñan por caso de uso |

## Consecuencias

- (+) Dominio 100% testeable sin BD; la infraestructura se puede cambiar (SQL Server ↔ PostgreSQL) sin tocar reglas.
- (+) Estructura que escala por features.
- (−) Más proyectos y archivos que una API mínima; se acepta porque el costo es bajo y es el criterio evaluado.
