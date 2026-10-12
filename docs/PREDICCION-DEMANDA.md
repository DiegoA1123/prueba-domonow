# Predicción de demanda de parqueaderos de visitantes

> Desafío adicional (§16 del enunciado): diseño, sin implementación. Objetivo: predecir qué días y franjas horarias tendrán mayor demanda en cada propiedad.

**Datos.** Cada ingreso y salida de `parking_assignments` (propiedad, parqueadero, `checked_in_at`, `checked_out_at`), leídos desde la base analítica alimentada por CDC (README § Evolución), nunca desde la base que atiende al portero. De ahí sale el objetivo: **ocupación por propiedad y franja de una hora**. Ojo: la ocupación está topada por la capacidad; cuando el parqueadero se llena, el visitante 31 no deja rastro y la demanda real se subestima justo en los picos. Por eso se agrega un evento nuevo, **"sin cupo"** (el portero lo marca o el sistema lo registra al intentar un ingreso sin parqueaderos libres), que mide la demanda que no se atendió.

**Variables.**
- Calendario: día de la semana, hora, mes, **festivos y puentes de Colombia** (Ley Emiliani: muchos festivos pasan a lunes), **quincenas** (días 15 y 30), diciembre y Semana Santa.
- Historial: ocupación de la misma franja 1 y 4 semanas antes; tendencia de la propiedad.
- Propiedad: número de unidades y de parqueaderos de visitantes.
- **Reservas de zonas comunes** de DomoNow: un salón social reservado el sábado a las 7 p. m. anuncia visitantes. Es una señal que solo DomoNow tiene.
- Opcional: pronóstico de lluvia.

**Algoritmo.** Primero una **línea base**: promedio de la misma franja en las últimas 4 semanas. Luego un modelo **gradient boosting (LightGBM)** global, entrenado con todas las propiedades a la vez para que las nuevas aprovechen lo aprendido de las demás. Predice ocupación esperada y un percentil 90 (regresión por cuantiles). Se elige porque los datos son tabulares, el modelo es explicable (importancia de variables) y no requiere GPU. El modelo solo se usa si le gana a la línea base.

**Entrenamiento.** En Python (pandas + LightGBM), con validación **por tiempo, nunca al azar**: *backtesting* con origen móvil (entrenar hasta la semana N, validar la N+1, avanzar). Reentrenamiento semanal en un job programado. El modelo se exporta a **ONNX**.

**Medición.**
- Error: **WMAPE** y MAE de la ocupación por franja, siempre comparados con la línea base.
- Lo que importa al negocio: **acierto en los picos**. De las 10 franjas que el modelo marca como más demandadas por propiedad y semana, cuántas lo fueron de verdad.
- Calibración: el p90 debe cubrir la ocupación real cerca del 90 % de las veces.

**Integración con .NET y Angular.** Predicción **por lotes**: cada noche el job calcula los próximos 7 días y los guarda en `demand_forecasts` (propiedad, franja, ocupación esperada, p90, versión del modelo). La API .NET solo lee esa tabla (`GET /api/demand-forecast?propertyId=…`), sin ejecutar el modelo ni sumar latencia al portero. Si se necesitara inferencia en línea, .NET carga el ONNX con ONNX Runtime sin depender de Python. Angular muestra un **mapa de calor días × horas** para el administrador, con los picos resaltados.

**Producción.** Pipeline programado: extraer → construir variables → entrenar → validar contra la línea base y contra el modelo vigente → registrar la versión → predecir. Un modelo nuevo se publica solo si mejora; cada predicción guarda su versión, y volver a la anterior es cambiar un puntero.

**Degradación.**
- Cada día se compara lo predicho con lo que ocurrió: WMAPE móvil por propiedad, con alerta si sube de un umbral o si el modelo empieza a perder contra la línea base.
- Deriva de datos: alerta si cambia la distribución de las variables de entrada (propiedades nuevas muy distintas, un festivo nuevo, un cambio de reglas de visitantes).
- Cualquier alerta dispara un reentrenamiento fuera de calendario; si el modelo nuevo no mejora, se vuelve a la línea base, que siempre está disponible.
