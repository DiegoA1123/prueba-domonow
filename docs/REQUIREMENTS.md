# Requerimientos (resumen del enunciado)

Fuente: `PRUEBA_TECNICA_FULLSTACK.docx` (OFIMA SAS — Ingeniero de IA / Full Stack, DomoNow).
Tiempo máximo: **6 h** (≈3 h backend, ≈3 h frontend + pruebas + revisión).

## Dominio

**Parqueadero**: código (P01…), estado: `Available` | `Occupied` | `OutOfService`.
**Asignación**: parqueadero, placa, nombre visitante, unidad destino, fecha/hora ingreso, fecha/hora salida, estado: `Active` | `Finished` | `Cancelled`.

## Reglas de negocio

1. Un parqueadero no puede tener más de una asignación activa simultáneamente.
2. No se puede asignar un parqueadero Fuera de servicio.
3. Al registrar salida, el parqueadero queda Disponible.
4. Placa obligatoria y normalizada (`abc-123` → `ABC123`).
5. Fecha de salida no puede ser anterior a la de ingreso.
6. Concurrencia: dos usuarios asignan P15 casi a la vez → solo una asignación activa.

### Reglas adicionales definidas por el ingeniero

7. Un vehículo (placa) no puede ingresar dos veces sin haber salido: máximo una asignación activa por placa en toda la propiedad (ADR-0005).

## Backend (.NET, C#, EF Core, SQL Server/PostgreSQL)

- `GET` parqueaderos (código, estado, disponibilidad; filtro opcional por estado).
- `POST /api/parking-assignments` → `{ parkingSpotId, licensePlate, visitorName, destinationUnit }`.
- `PUT /api/parking-assignments/{id}/checkout`.
- `GET` asignaciones activas (vehículos dentro).
- Calidad: SoC, errores, validaciones, DI, async, modelado de dominio, concurrencia, seguridad.

## Pruebas mínimas (xUnit/NUnit/MSTest)

- Asignación correcta de parqueadero disponible.
- Intento de asignar parqueadero ocupado.
- Intento de asignar parqueadero fuera de servicio.
- Finalización de una asignación.
- Validación de fechas.

## Frontend (Angular + TypeScript)

- Pantalla "Parqueaderos de visitantes": ver rápidamente Disponibles / Ocupados (con placa) / Fuera de servicio.
- Desde uno disponible: registrar placa, visitante, unidad → consumir API, refrescar, mostrar errores del backend.
- Desde uno ocupado: "Registrar salida" → finaliza y vuelve a disponible; UI refleja cambio.
- Se evalúa: arquitectura, organización, estado, integración API, tipado, componentización, UX básica, errores.

## Entregables

- Repo Git con `/backend`, `/frontend`, `README.md`, `AI-USAGE.md`.
- README: ejecutar backend, frontend, configurar BD, decisiones de arquitectura, supuestos, mejoras futuras, **"Evolución de la solución"** (5.000 propiedades, cientos de miles de movimientos/día).
- AI-USAGE.md: 3–5 casos (Problema, Herramienta, Instrucción, Resultado, Decisión, Justificación) + **AI Code Review** con 3 hallazgos (Hallazgo, ¿De acuerdo?, Acción, Justificación).
- Opcional: desafío de predicción de demanda (≤1 página). Docker opcional.

## Ponderación

Backend/C# 25% · Arquitectura 20% · Angular/TS 20% · Calidad y pruebas 15% · Uso de IA 15% · Comunicación 5%.
> "Preferimos una solución incompleta pero técnicamente sólida y bien razonada."
