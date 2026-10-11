# AI-USAGE — Uso de Inteligencia Artificial

Herramienta principal: **Claude Code** (modelo Claude Opus 5.5) con plugins `superpowers`, `feature-dev`, `pr-review-toolkit`, `commit-commands`, `frontend-design`.

> Este documento tiene dos partes:
> 1. **Casos seleccionados** (3–5): la versión final que pide la prueba.
> 2. **Registro continuo**: se llena *durante* el desarrollo cada vez que se acepta, modifica o rechaza una propuesta de la IA. De aquí salen los casos finales.

---

## 1. Casos seleccionados

> Se completan al final a partir del registro continuo.

### Caso 1 — _título_
- **Problema**:
- **Herramienta**:
- **Instrucción**:
- **Resultado (propuesta de la IA)**:
- **Decisión del ingeniero**: Aceptada / Modificada / Rechazada
- **Justificación**:

---

## 2. AI Code Review

> Se ejecuta al terminar la solución (agentes `pr-review-toolkit` + `/code-review` + `/security-review`).
> Instrucción: buscar problemas de arquitectura, bugs, concurrencia, vulnerabilidades, rendimiento, complejidad innecesaria y pruebas ausentes.

### Hallazgo 1
- **Hallazgo**:
- **¿De acuerdo?**: Sí / No / Parcialmente
- **Acción tomada**:
- **Justificación técnica**:

### Hallazgo 2
- **Hallazgo**:
- **¿De acuerdo?**:
- **Acción tomada**:
- **Justificación técnica**:

### Hallazgo 3
- **Hallazgo**:
- **¿De acuerdo?**:
- **Acción tomada**:
- **Justificación técnica**:

---

## 3. Registro continuo

> Regla: solo hechos reales. Cada fila debe corresponder a una propuesta concreta de la IA en la sesión y a lo que el ingeniero hizo con ella.

| # | Fecha | Problema | Instrucción (resumen) | Propuesta de la IA | Decisión | Justificación |
|---|-------|----------|-----------------------|--------------------|----------|---------------|
| 1 | 2026-10-10 | Elegir herramientas de IA para la prueba | "Lee la prueba y dime qué skills/plugins instalar para hacerlo de la mejor manera" | Set de plugins mapeado a la rúbrica (superpowers, feature-dev, pr-review-toolkit, commit-commands, context7, frontend-design). Recomendó **no** instalar un plugin "clean-architecture" de la comunidad (hook en cada edición, empuja a sobreingeniería) ni variantes duplicadas de superpowers | **Aceptada** | Las herramientas se eligieron por criterio evaluado (arquitectura, review, tests, Git), no por cantidad |
| 2 | 2026-10-10 | Plantilla de AI-USAGE.md | "Define cómo vamos a trabajar y la documentación base" | La IA redactó dos casos de ejemplo **como si hubieran ocurrido** (rechazo de MediatR; validación *check-then-act*) sin que existieran en la sesión | **Rechazada** | Error de la IA (contenido fabricado). Detectado en autorrevisión antes de entregar; se eliminaron y se fijó la regla "solo hechos reales" en este documento |
| 3 | 2026-10-10 | Autenticación ("usuario autorizado", §2 del enunciado) | Decisiones de diseño previas al código | Dejar autenticación **fuera de alcance** y documentarla como supuesto, para invertir el tiempo en dominio y concurrencia | **Rechazada** | El enunciado lo pide explícitamente y la rúbrica evalúa seguridad. Se implementará JWT + operador de prueba con contraseña hasheada y roles (ADR-0004) |
| 4 | 2026-10-10 | Formato válido de placa (R4) | Ídem | Validación laxa `^[A-Z0-9]{5,7}$` para no rechazar placas especiales | **Modificada** | El enunciado da `ABC123` como forma normalizada; se adoptó formato colombiano estricto: carro `AAA999`, moto `AAA99A`. Placas extranjeras quedan como supuesto documentado |
| 5 | 2026-10-10 | Origen de la hora de salida (R5) | Ídem | Hora del servidor por defecto (`TimeProvider`) + `checkedOutAt` opcional validado por el dominio | **Aceptada** | El servidor es la fuente confiable de tiempo; el opcional permite registro tardío y hace que R5 sea verificable por API, no solo en tests unitarios |
| 6 | 2026-10-10 | BD para tests de integración (R6) | Ídem | PostgreSQL local con BD dedicada; desaconsejó SQLite/InMemory | **Aceptada** | InMemory/SQLite no soportan `xmin` ni reproducen índices parciales y concurrencia real; la prueba de R6 no sería válida |
| 7 | 2026-10-10 | Límite del agregado al incorporar DDD | "Agrega DDD para todo el proyecto" | `ParkingSpot` como Aggregate Root con `ParkingAssignment` interna (solo la activa cargada); sin domain events ni domain services por ahora | **Aceptada** | Se evaluó la alternativa de dos agregados + servicio de dominio y se descartó: R1 y R3 son invariantes transaccionales entre parqueadero y asignación, así que pertenecen a un mismo agregado; la versión de la raíz cubre R6 (ADR-0003) |
| 8 | 2026-10-10 | Nueva regla del ingeniero (R7): una placa no puede ingresar dos veces sin haber salido | "Agrega: el que ya ingresó no puede volver a ingresar sin haber salido" | Señaló que R7 **cruza agregados** (cada `ParkingSpot` solo conoce su asignación), así que el agregado no puede protegerla; propuso consulta previa en el handler + índice único parcial por placa, traducido a 409 por nombre de restricción | **Aceptada** | El ingeniero aprobó el diseño tras revisarlo: la consulta previa da un mensaje claro y el índice por placa es la garantía real bajo concurrencia (ADR-0005) |
| 9 | 2026-10-10 | Autorrevisión del plan de backend | Skill `superpowers:writing-plans` (paso de self-review) | La IA encontró errores en su propio plan: datos de test compartidos que harían fallar la prueba de "fuera de servicio" con 409 en vez de 422, un test duplicado dependiente del orden, un `using` faltante y uso de `!` contrario a las convenciones | **Corregida antes de ejecutar** | Los tests de integración deben ser independientes del orden; se asignó a cada test su propio parqueadero y placa |
| 10 | 2026-10-10 | Estrategia de concurrencia (R6): ¿bloqueo pesimista? | "Para la concurrencia, ¿no tenemos algo como el lock for update?" | Comparó `SELECT … FOR UPDATE` (pesimista) con `xmin` (optimista): ambos garantizan R6, pero el pesimista exige transacción explícita en el caso de uso, retiene bloqueos y no resuelve R7. Recomendó mantener el optimista + índices; propuso `FOR UPDATE SKIP LOCKED` solo para una futura asignación automática del primer parqueadero libre | **Aceptada** | El ingeniero cuestionó la estrategia antes de implementarla; se mantuvo la optimista por baja contención por fila, y el pesimista quedó documentado como evolución para escenarios de alta contención (README § Evolución) |
