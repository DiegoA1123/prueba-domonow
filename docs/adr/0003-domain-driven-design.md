# ADR-0003: Domain-Driven Design (estratégico y táctico)

- **Estado**: Aceptado
- **Fecha**: 2026-10-10
- **Relacionado**: ADR-0001 (Clean + Hexagonal), ADR-0002 (concurrencia)

## Contexto

El usuario definió DDD para todo el proyecto, junto con Clean Architecture y Hexagonal. Las tres se complementan: **DDD decide qué va dentro del dominio y cómo se modela**; Clean/Hexagonal deciden **cómo se protege** ese dominio del resto. La pregunta de diseño más importante es el límite del agregado, porque determina cómo se garantizan R1, R3 y R6.

## Decisión

### Estratégico
- **Bounded Context**: `Parking` (Parqueaderos de visitantes), dentro de la plataforma DomoNow. Glosario y mapa de contextos en `docs/DOMAIN.md`.
- Otros contextos de DomoNow (Unidades, Residentes, Visitantes, Administración) quedan **fuera**. Nos relacionamos con ellos solo por valor: `DestinationUnit` es un value object (texto), no una referencia a otra entidad.

### Táctico — límite del agregado

**`ParkingSpot` es el Aggregate Root y `ParkingAssignment` es una entidad interna del agregado.**

Motivo: las reglas R1 ("máx. una asignación activa") y R3 ("al dar salida el parqueadero queda disponible") son **invariantes que involucran al parqueadero y a su asignación a la vez**. En DDD, una invariante que debe cumplirse de forma transaccional define el límite del agregado. Si fueran agregados separados, R1 y R3 necesitarían coordinación entre agregados (servicio de dominio + dos escrituras).

Consecuencias del diseño:
- Toda modificación entra por la raíz: `spot.CheckIn(...)`, `spot.CheckOut(assignmentId, at)`, `spot.MarkOutOfService()`.
- El agregado carga **solo la asignación activa** (las finalizadas son historial y no participan en invariantes). Así el agregado no crece sin límite.
- **Un agregado por transacción** → la concurrencia optimista sobre la raíz (`xmin` de `parking_spots`) es la versión del agregado y resuelve R6 de forma natural. El índice único parcial queda como red de seguridad en BD (ADR-0002).
- **Lado de lectura**: listados (parqueaderos, vehículos dentro) no pasan por el agregado; se proyectan directo a DTO con `AsNoTracking`. Es la separación lectura/escritura mínima, en la misma base de datos (no es CQRS con bases separadas).

### Bloques tácticos que SÍ usamos
| Bloque | Uso |
|--------|-----|
| Aggregate Root | `ParkingSpot` |
| Entity | `ParkingAssignment` (identidad propia, vive dentro del agregado) |
| Value Objects | `SpotCode`, `LicensePlate`, `VisitorName`, `DestinationUnit` |
| Domain Errors | Catálogo `ParkingErrors` con códigos estables (`Parking.SpotOccupied`…) |
| Repository (puerto) | Uno por agregado: `IParkingSpotRepository`. No hay repositorio de `ParkingAssignment` |
| Application Services | Handlers de casos de uso (orquestan, no deciden reglas) |

### Bloques que NO usamos (por ahora)
| Bloque | Por qué no |
|--------|-----------|
| Domain Events + dispatcher | Nadie los consumiría hoy → código muerto. Se documentan en "Evolución de la solución" (outbox + mensajería a escala) |
| Domain Services | Ninguna regla cruza agregados con este diseño de límites |
| Specifications | Las consultas son triviales |
| Factories separadas | Bastan los *factory methods* estáticos en las entidades |

## Alternativas consideradas

| Opción | Por qué no |
|--------|-----------|
| `ParkingAssignment` como agregado propio | R1/R3 cruzarían dos agregados en una transacción; requeriría servicio de dominio y la garantía quedaría solo en la BD |
| `ParkingSpot` cargando todo el historial | Agregado ilimitado, lento y propenso a conflictos de concurrencia innecesarios |
| Modelo anémico (entidades con setters + lógica en servicios) | Contrario a DDD; las reglas quedarían dispersas y no protegidas |

## Consecuencias

- (+) Las 5 reglas de negocio de dominio (R1–R5) se prueban con tests unitarios puros sobre el agregado.
- (+) R6 queda cubierta por la versión del agregado + índice parcial.
- (−) El endpoint `PUT /parking-assignments/{id}/checkout` requiere ubicar el agregado por id de asignación: `IParkingSpotRepository.GetByActiveAssignmentIdAsync(id)`. Aceptable.
