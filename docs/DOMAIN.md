# Modelo de dominio (DDD)

Decisión y justificación: `adr/0003-domain-driven-design.md`.

## 1. Lenguaje ubicuo

El mismo término se usa en conversación, documentación, código backend, código frontend, API y base de datos. Si un término cambia, cambia en todos lados.

| Español (negocio) | Inglés (código) | Definición |
|-------------------|-----------------|-----------|
| Parqueadero de visitantes | `ParkingSpot` | Espacio físico identificado por un código, destinado a vehículos de visitantes |
| Código de parqueadero | `SpotCode` | Identificador legible del parqueadero (`P01`…`P30`) |
| Estado del parqueadero | `ParkingSpotStatus` | `Available` (Disponible), `Occupied` (Ocupado), `OutOfService` (Fuera de servicio) |
| Asignación | `ParkingAssignment` | Uso de un parqueadero por un vehículo visitante, desde el ingreso hasta la salida |
| Estado de la asignación | `AssignmentStatus` | `Active` (Activa), `Finished` (Finalizada), `Cancelled` (Cancelada) |
| Registrar ingreso | `CheckIn` | Crear una asignación activa en un parqueadero disponible |
| Registrar salida | `CheckOut` | Finalizar la asignación activa y liberar el parqueadero |
| Placa | `LicensePlate` | Identificador del vehículo, siempre normalizado (mayúsculas, sin separadores) |
| Visitante | `VisitorName` | Nombre de la persona que ingresa el vehículo |
| Unidad destino | `DestinationUnit` | Apartamento/unidad que se visita (`302`, `Torre 2 - 501`) |
| Fecha/hora de ingreso | `CheckedInAt` | Momento del ingreso (UTC) |
| Fecha/hora de salida | `CheckedOutAt` | Momento de la salida (UTC), nunca anterior al ingreso |
| Vehículos dentro | `ActiveAssignments` | Asignaciones en estado `Active` |
| Operador | `Operator` | Usuario autorizado (portería/administración) que registra ingresos y salidas. Pertenece al subdominio genérico de identidad, no al contexto `Parking` (ADR-0004) |

Términos a **evitar** por ambiguos: "reserva" (no hay reservas futuras), "slot", "celda", "ticket".

## 2. Bounded Context y mapa de contextos

```
┌──────────────────────── DomoNow ────────────────────────┐
│                                                         │
│  ┌──────────────────┐        ┌───────────────────────┐  │
│  │  Parking (este)  │ ─ ─ ─▶ │ Units / Residents     │  │
│  │  Parqueaderos de │ valor  │ (fuera de alcance)    │  │
│  │  visitantes      │        └───────────────────────┘  │
│  └──────────────────┘        ┌───────────────────────┐  │
│                              │ Visitors, Common      │  │
│                              │ areas, Admin…         │  │
│                              └───────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

- `Parking` no conoce entidades de otros contextos. `DestinationUnit` se guarda como valor.
- **Identidad (subdominio genérico)**: operadores, contraseñas y tokens JWT. Vive fuera del dominio `Parking`, en `Application/Authentication` + `Infrastructure/Identity` (ADR-0004).
- A futuro: integración por eventos (p. ej. `VehicleCheckedIn` → notificación al residente). Ver README "Evolución de la solución".

## 3. Agregado `ParkingSpot`

```
ParkingSpot  «Aggregate Root»
├── Id : Guid
├── Code : SpotCode                  «VO»
├── Status : ParkingSpotStatus
├── ActiveAssignment : ParkingAssignment?   «Entity» (0..1)
│     ├── Id : Guid
│     ├── LicensePlate : LicensePlate «VO»
│     ├── VisitorName : VisitorName   «VO»
│     ├── DestinationUnit : DestinationUnit «VO»
│     ├── CheckedInAt : DateTimeOffset
│     ├── CheckedOutAt : DateTimeOffset?
│     └── Status : AssignmentStatus
│
├── CheckIn(plate, visitor, unit, now)          → Result<ParkingAssignment>
├── CheckOut(assignmentId, checkedOutAt, now)   → Result<ParkingAssignment>
└── MarkOutOfService()                          → Result   (usado por el seed)
```

### Invariantes del agregado

| Regla | Invariante | Método que la protege | Error |
|-------|-----------|-----------------------|-------|
| R1 | `Status == Occupied` ⇔ existe exactamente una `ActiveAssignment` | `CheckIn`, `CheckOut` | `Parking.SpotOccupied` (409) |
| R2 | No hay `CheckIn` si `Status == OutOfService` | `CheckIn` | `Parking.SpotOutOfService` (422) |
| R3 | `CheckOut` deja `Status = Available` y la asignación `Finished` en la misma operación | `CheckOut` | — |
| R4 | `LicensePlate` siempre normalizada y válida | `LicensePlate.Create` | `Parking.InvalidLicensePlate` (400) |
| R5 | `CheckedInAt <= CheckedOutAt <= ahora`. Por defecto la hora la pone el servidor; el cliente puede enviar `checkedOutAt` opcional (registro tardío) y se valida igual | `ParkingAssignment.Finish` | `Parking.CheckOutBeforeCheckIn` / `Parking.CheckOutInFuture` (422) |
| R7 | Una placa no puede tener dos asignaciones activas (no entra dos veces sin salir). **Cruza agregados**: no la protege `ParkingSpot`, sino el `CheckInHandler` + índice único parcial (ADR-0005) | `CheckInHandler` + `ux_parking_assignments_active_plate` | `Parking.VehicleAlreadyInside` (409) |
| — | No se puede marcar fuera de servicio un parqueadero ocupado | `MarkOutOfService` | `Parking.SpotOccupied` (409) |
| — | No se puede dar salida a una asignación no activa | `CheckOut` | `Parking.AssignmentNotActive` (409) |

### Transiciones de estado

```
ParkingSpot:      Available ──CheckIn──▶ Occupied ──CheckOut──▶ Available
                  Available ──MarkOutOfService──▶ OutOfService
                  (volver a servicio: fuera de alcance; no hay requisito ni endpoint)

ParkingAssignment: (create) ─▶ Active ──Finish──▶ Finished
                                      └─Cancel──▶ Cancelled   (sin endpoint por ahora)
```

## 4. Value Objects

| VO | Regla de creación |
|----|-------------------|
| `LicensePlate` | Obligatoria; elimina espacios, guiones y puntos; mayúsculas. Formato colombiano estricto, según el ejemplo del enunciado: **carro** `^[A-Z]{3}[0-9]{3}$` (`ABC123`) o **moto** `^[A-Z]{3}[0-9]{2}[A-Z]$` (`ABC12D`). Placas extranjeras/diplomáticas quedan fuera (supuesto documentado) |
| `SpotCode` | Obligatorio; mayúsculas; formato `P` + 2 dígitos |
| `VisitorName` | Obligatorio; recortado; 2–100 caracteres |
| `DestinationUnit` | Obligatorio; recortado; 1–20 caracteres |

Los VOs son inmutables, se comparan por valor y **no pueden existir en estado inválido**.
