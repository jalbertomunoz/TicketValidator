# Requisitos de TicketValidator

## 1. Propósito

Este documento define los requisitos funcionales, reglas de negocio, requisitos no funcionales, alcance y criterios de aceptación del MVP de **TicketValidator**.

TicketValidator es un servicio web REST para analizar imágenes de tickets y facturas de gasto.

El servicio recibe:

- Una imagen del documento.
- El tipo de gasto indicado por el usuario.

A partir de esta información:

1. Corrige la orientación y obtiene evidencia textual mediante OCR.
2. Extrae la información estructurada directamente de la imagen mediante IA.
3. Contrasta fecha y total entre la lectura visual y OCR.
4. Analiza indicios visuales de manipulación.
5. Aplica reglas de negocio deterministas.
6. Devuelve una decisión estructurada y explicable.

El principio fundamental del sistema es:

```text
IA visual = fuente estructurada principal

OCR = legibilidad, RawText, fecha, total, contraste y diagnóstico

Código = decisión
```

Esta política se ajustó tras validación experimental del MVP con tickets reales:
GPT-4.1 visual estructuró documentos donde Tesseract perdió información. El
código conserva OCR como evidencia independiente de fecha y total y decide de
forma determinista.

---

# 2. Objetivo

Desarrollar un servicio capaz de analizar tickets y facturas de gasto en formato JPEG o PNG y devolver uno de los siguientes resultados:

```text
APPROVED
REJECTED
REVIEW_REQUIRED
UNREADABLE
PROCESSING_ERROR
```

El resultado debe contener el motivo de la decisión y los principales datos obtenidos del documento.

---

# 3. Objetivos específicos

El proyecto deberá:

- Exponer una API REST mediante ASP.NET Core.
- Documentar la API mediante OpenAPI y Swagger UI.
- Procesar imágenes JPEG y PNG.
- Detectar y corregir la orientación del documento cuando sea posible.
- Extraer texto mediante Tesseract OCR.
- Conservar el texto OCR como evidencia.
- Utilizar GPT-4.1 para interpretación y clasificación semántica.
- Utilizar análisis visual mediante IA para detectar indicios de manipulación.
- Extraer los principales datos de tickets y facturas.
- Extraer las líneas de productos o servicios.
- Detectar bebidas alcohólicas.
- Validar la coherencia con el tipo de gasto declarado.
- Contrastar OCR e IA en los campos críticos.
- Aplicar las reglas mediante código.
- Registrar información técnica en ficheros de log.
- Poder ejecutarse mediante Docker.
- Poder desplegarse en Render.
- Contener pruebas automatizadas con xUnit.

---

# 4. Motivación

El proyecto nace de problemas observados en un sistema anterior basado principalmente en análisis mediante Inteligencia Artificial.

Entre los problemas detectados se encuentran:

- Fechas interpretadas incorrectamente.
- Fechas completadas por la IA cuando algunos dígitos no eran legibles.
- Tickets fotografiados desde demasiada distancia.
- Tickets fotografiados con orientación incorrecta.
- Tickets borrosos o desenfocados.
- Falsos positivos provocados por asociaciones semánticas.

Un ejemplo observado es:

```text
CEREZAS
```

clasificado incorrectamente como una bebida alcohólica, posiblemente por asociación semántica con conceptos como:

```text
LICOR DE CEREZAS
```

El nuevo sistema debe reducir este tipo de errores mediante el uso de OCR como evidencia textual independiente.

---

# 5. Alcance del MVP

El núcleo del proyecto será un **servicio web REST**.

El MVP incluye:

- ASP.NET Core.
- C#.
- Endpoint REST para analizar documentos.
- Entrada mediante `multipart/form-data`.
- Imágenes JPEG.
- Imágenes PNG.
- Tipo de gasto.
- Corrección de orientación/rotación 0/90/180/270 mediante Tesseract OSD, sin corrección fina de inclinación/skew.
- Tesseract OCR.
- GPT-4.1.
- Extracción de datos.
- Clasificación de productos.
- Análisis visual de manipulación.
- Comparación OCR / IA.
- Motor de reglas.
- Estados de aprobación, rechazo, revisión e ilegibilidad.
- Logs en fichero.
- Tests xUnit.
- Swagger UI.
- Docker.
- Despliegue objetivo en Render.

---

# 6. Fuera de alcance

No forman parte del MVP:

- Gestión de usuarios.
- Autenticación.
- Login.
- Gestión completa de gastos.
- Gestión de expedientes.
- Contabilidad.
- Pagos.
- Aplicación móvil.
- Procesamiento masivo.
- Colas de mensajes.
- Microservicios.
- CI/CD complejo.
- Desarrollo de un OCR propio.
- Entrenamiento de modelos de IA propios.
- Base de datos funcional de gastos.
- Persistencia de imágenes.
- Corrección fina de inclinación/skew.
- OpenCV/OpenCvSharp para el preprocesamiento de imagen.
- Corrección avanzada de perspectiva.
- Restauración de imágenes.
- Filtros avanzados de contraste.
- Certificación forense de autenticidad.
- Detección garantizada de fraude.
- Validación C2PA en el MVP.
- Análisis EXIF como prueba de autenticidad.
- Calibración definitiva de umbrales OCR.
- Frontend completo.

Estas funcionalidades podrán considerarse como mejoras futuras.

---

# 7. Tecnologías

Las tecnologías principales están cerradas:

```text
Lenguaje:
C#

Framework:
.NET / ASP.NET Core

OCR:
Tesseract

Inteligencia Artificial:
OpenAI GPT-4.1

Testing:
xUnit

API:
REST

Documentación API:
OpenAPI / Swagger UI

Contenedores:
Docker

Hosting:
Render

Logging:
Fichero

Formatos:
JPEG
PNG
```

---

# 8. Entrada del servicio

El servicio recibirá dos parámetros principales.

## 8.1 Archivo

Campo:

```text
file
```

Obligatorio.

Formatos admitidos:

```text
Extensiones: .jpg, .jpeg, .png
Firma binaria: fuente principal para comprobar que .jpg/.jpeg son JPEG y .png es PNG
MIME habitual: image/jpeg, image/png
MIME genérico admitido con extensión y firma coherentes: application/octet-stream, image/jpg
```

El tamaño máximo de carga se configura mediante `Uploads:MaxFileSizeBytes` y su valor inicial es 10 MB.

---

## 8.2 Tipo de gasto

Campo:

```text
expenseType
```

Obligatorio.

Tipos iniciales:

```text
COMIDA
DIETA
ALMUERZO
CENA
DESAYUNO
COMBUSTIBLE
HOTEL
TAXI
PARKING
MATERIAL
AUTOPISTA
OTROS
```

---

# 9. Flujo general

El flujo conceptual será:

```text
Imagen
  ↓
Validación de entrada
  ↓
Tesseract OSD y OCR inicial
  ↓
Fallback 0°/90°/180°/270° si falta texto, palabras, fecha o total
  ↓
Selección conjunta de imagen y OcrResult
  ↓
Lectura visual estructurada de la misma imagen
  ↓
Clasificación de productos y análisis de coherencia
  ↓
Verificación OCR/visual de fecha y total
  ↓
Motor de reglas, auditoría y respuesta REST
```

---

# 10. Política de evidencia

La IA visual es la fuente principal de lectura directamente de la imagen para
emisor, CIF, número de factura, hora, dirección, IVA, productos, fecha y total.
OCR conserva evidencia textual independiente exclusivamente para contrastar
fecha y total, determinar legibilidad y facilitar diagnóstico.

Para fecha y total:

```text
Visual + OCR coinciden
→ dato corroborado

Visual existe + OCR no existe
→ se conserva el valor visual; Match = null; REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

Visual existe + OCR nulo
→ se conserva el valor visual; REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

Visual y OCR difieren
→ REVIEW_REQUIRED y código de discrepancia

Solo OCR existe
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

Ambas fuentes no obtienen el dato
→ ERR_SIN_FECHA o ERR_SIN_TOTAL
```

`OcrReadable` solo indica que existe evidencia textual OCR. Se distingue OCR
parcial, cuando existe texto aunque falten fecha o total, de OCR nulo, sin texto
ni palabras. En ambos casos, un campo crítico sin corroboración conserva el valor
visual pero requiere `REVIEW_REQUIRED / OCR_LOW_CONFIDENCE`. OCR nulo con
ticket/factura y evidencia visual suficiente evita `ERR_NO_LEGIBLE`; sin
evidencia visual suficiente devuelve `UNREADABLE / ERR_NO_LEGIBLE`.

---

# 11. Campos críticos

Inicialmente se consideran campos críticos:

```text
Fecha
Total
Tipo de documento
Conceptos que puedan provocar un rechazo
```

---

# 12. Política de discrepancias

## 12.1 IA visual y OCR coinciden

```text
IA visual
+
OCR coincide

→ campo verificado
```

---

## 12.2 IA visual y OCR discrepan

Cuando OCR e IA visual proporcionen valores diferentes para un campo crítico:

```text
→ REVIEW_REQUIRED
```

Ejemplo:

```text
OCR:
14/08/2026

IA visual:
17/08/2026

Resultado:
REVIEW_REQUIRED
DATE_MISMATCH
```

---

## 12.3 IA visual obtiene el dato y OCR parcial no

Ejemplo:

```text
IA visual:
14/08/2026

OCR:
texto sin fecha
```

El sistema utilizará:

```text
14/08/2026
```

como lectura principal sin marcarla como corroborada por OCR.

El resultado es `REVIEW_REQUIRED / OCR_LOW_CONFIDENCE`: para aprobar, fecha y
total deben estar corroborados por ambas fuentes.

---

## 12.4 OCR nulo con lectura visual suficiente

Si OCR no obtiene texto ni palabras, pero la IA visual identifica ticket o
factura y lee fecha o total:

```text
→ conservar los valores visuales
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

## 12.5 Solo OCR obtiene el dato

Si OCR encuentra un dato crítico y la IA visual no:

```text
→ conservar la evidencia OCR
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

# 13. Confianza OCR

Tesseract podrá proporcionar información de confianza.

Cuando sea posible se conservarán:

- Confianza global.
- Confianza por palabra.
- Texto reconocido.
- Posiciones o bounding boxes.

No se fija inicialmente un umbral como:

```text
80 %
90 %
95 %
```

Los umbrales se determinarán mediante experimentación con tickets reales.

La configuración de confianza deberá permanecer separada de las reglas de negocio.

---

# 14. Requisitos funcionales

## RF-001 — Recepción de documento

El sistema deberá aceptar una imagen JPEG o PNG mediante una petición REST.

---

## RF-002 — Recepción del tipo de gasto

El sistema deberá recibir el tipo de gasto declarado por el usuario.

---

## RF-003 — Validación de entrada

El sistema deberá rechazar solicitudes:

- Sin archivo.
- Sin tipo de gasto.
- Con formato no permitido.
- Con parámetros obligatorios ausentes.

---

## RF-004 — Orientación

El sistema deberá intentar detectar la orientación gruesa 0/90/180/270 mediante
Tesseract OSD antes de realizar OCR. Si OSD no alcanza confianza técnica
suficiente (15 como criterio técnico inicial del wrapper), se conservará la
imagen original. Si OSD no puede detectar orientación por falta de evidencia,
también se conservará la imagen original y continuará OCR normal. Este umbral
no es confianza OCR de negocio.

Si ese primer OCR no contiene texto útil, reconoce menos de tres palabras, no
obtiene fecha o no obtiene total, el sistema probará 0/90/180/270, reutilizando
el resultado inicial para 0°, y seleccionará de forma determinista la mayor
evidencia OCR. La imagen elegida y su resultado OCR se usarán en el resto del
pipeline. Este fallback no se ejecuta cuando el OCR inicial aporta texto, fecha y
total suficientes y no incluye inclinación fina, perspectiva ni OpenCV.

---

## RF-005 — Rotación

El sistema deberá poder corregir documentos orientados a:

```text
0°
90°
180°
270°
```

La corrección no incluye inclinaciones arbitrarias ni skew fino. El fixture de
ticket inclinado aproximadamente 10 grados permanece como limitación conocida.

---

## RF-006 — OCR

El sistema deberá utilizar Tesseract para extraer el texto visible del documento.

---

## RF-007 — Evidencia OCR

El texto obtenido mediante OCR deberá conservarse durante el análisis.

---

## RF-008 — Confianza OCR

Cuando Tesseract proporcione información de confianza, el sistema deberá poder conservarla y utilizarla durante la verificación.

---

## RF-009 — Lectura visual estructurada

El sistema deberá utilizar GPT-4.1 visual sobre la imagen seleccionada para
extraer tipo de documento, proveedor, CIF/NIF, dirección, número, fecha, hora,
total, IVA, productos e indicios de manipulación. El texto OCR no se utilizará
para completar estos campos.

---

## RF-010 — Datos desconocidos

La IA deberá devolver `null` cuando no pueda determinar un campo con suficiente seguridad.

No deberá inventar información ausente.

---

## RF-011 — Tipo de documento

El análisis visual deberá determinar explícitamente:

```text
TICKET
FACTURA
NO_DOCUMENTO
UNKNOWN
```

`NO_DOCUMENTO` requiere evidencia positiva de que la imagen no es un ticket ni factura. `UNKNOWN` representa evidencia insuficiente y no podrá convertirse automáticamente en `NO_DOCUMENTO`.

---

## RF-012 — Empresa

El sistema deberá intentar extraer el nombre del establecimiento o empresa.

---

## RF-013 — CIF

El sistema deberá intentar extraer el identificador fiscal cuando exista.

---

## RF-013a — CIF obligatorio condicionado

Para `Meals`, `Diet`, `Breakfast`, `Lunch`, `Dinner` y `Material`, un `TaxId`
vacío, nulo o compuesto solo por espacios producirá:

```text
REVIEW_REQUIRED
ERR_SIN_CIF
```

La ausencia de CIF/NIF no bloquea por sí sola `Parking`, `Highway`, `Taxi`,
`Fuel`, `Accommodation`, `Other` ni `Unknown`.

---

## RF-014 — Número de documento

El sistema deberá intentar extraer:

- Número de factura.
- Número de ticket.
- Identificador equivalente.

---

## RF-015 — Fecha

El sistema deberá intentar extraer la fecha del documento.

---

## RF-016 — Hora

El sistema deberá intentar extraer la hora cuando exista.

---

## RF-017 — Total

El sistema deberá intentar determinar el importe total finalmente pagado.

---

## RF-018 — Dirección

Cuando exista información suficiente, el sistema deberá intentar separar:

```text
street
city
postalCode
country
```

---

## RF-019 — IVA

El sistema deberá intentar extraer todos los desgloses de IVA existentes.

Por cada elemento:

```text
rate
taxableAmount
amount
```

---

## RF-020 — Productos

La IA visual deberá intentar extraer directamente de la imagen las líneas
correspondientes a:

- Productos.
- Artículos.
- Consumiciones.
- Servicios facturados.

Estas líneas se enviarán posteriormente al clasificador de productos y al
analizador de coherencia. OCR no es fuente de productos.

---

## RF-021 — Concepto de producto

Cada producto deberá conservar el concepto visible de la línea facturada cuando
sea posible, sin reinterpretarlo semánticamente.

---

## RF-022 — Texto normalizado

Cada producto podrá disponer de una versión normalizada para facilitar su interpretación.

La normalización no podrá alterar el significado esencial del producto.

---

## RF-023 — Clasificación de productos

El sistema deberá clasificar productos cuando sea necesario aplicar una regla de negocio.

---

## RF-024 — Bebidas alcohólicas

El sistema deberá detectar bebidas alcohólicas presentes como productos realmente facturados.

---

## RF-025 — Contexto del alcohol

La detección de alcohol deberá realizarse únicamente sobre líneas de compra.

---

## RF-026 — Exclusiones de alcohol

No deberán utilizarse como evidencia de alcohol textos pertenecientes a:

- Nombre del establecimiento.
- Razón social.
- Dirección.
- Empleado.
- Cajero.
- Camarero.
- Cliente.
- Mesa.
- Eslogan.
- Información administrativa.
- Información fiscal.

---

## RF-027 — Validación del tipo de gasto

El sistema deberá comprobar la coherencia entre los productos comprados y el tipo de gasto indicado.

La IA interpretará la coherencia semántica del conjunto de productos. El motor de reglas decidirá `ERR_TIPO_GASTO_INCOHERENTE` únicamente cuando la señal indique que la mayoría de la compra es claramente incoherente.

---

## RF-028 — Contexto del establecimiento

El sistema podrá clasificar el contexto del establecimiento como:

```text
RESTAURACION
SUPERMERCADO
ESTACION_SERVICIO
HOTEL
TRANSPORTE
PARKING
COMERCIO
OTRO
```

---

## RF-029 — Gastos de comida

Los gastos:

```text
COMIDA
DIETA
ALMUERZO
CENA
DESAYUNO
```

deberán corresponder principalmente a alimentos o consumiciones aptos para consumo inmediato.

---

## RF-030 — Restauración

En restaurantes, bares, cafeterías y establecimientos equivalentes se considerarán válidos los productos propios de restauración salvo que otra regla indique lo contrario.

---

## RF-031 — Supermercados

En supermercados se deberá diferenciar entre:

- Comida preparada.
- Productos para consumo inmediato.
- Productos que requieren preparación o cocinado.
- Productos no alimentarios.

---

## RF-032 — Producto completo

La clasificación de un producto deberá analizar su denominación completa.

No podrá rechazarse por una única palabra aislada.

---

## RF-033 — Producto ambiguo

Un producto desconocido o ambiguo no deberá provocar por sí solo el rechazo.

---

## RF-034 — Mayoría de la compra

La coherencia del gasto deberá determinarse considerando el conjunto de la compra.

Cuando existan importes individuales se utilizarán preferentemente los importes.

Cuando no existan se podrá utilizar el número de líneas.

---

## RF-035 — Fecha obligatoria

Si ninguna fuente puede determinar la fecha:

```text
ERR_SIN_FECHA
```

---

## RF-036 — Total obligatorio

Si ninguna fuente puede determinar el total:

```text
ERR_SIN_TOTAL
```

---

## RF-037 — Normalización de fecha

Cuando la fecha pueda determinarse, el contrato JSON la devolverá en formato ISO:

```text
yyyy-MM-dd
```

---

## RF-038 — Prohibición de reconstrucción de fecha

El sistema no deberá completar dígitos de una fecha que no puedan leerse con suficiente seguridad.

---

## RF-038a — Fecha temporalmente sospechosa

Solo cuando `DateMatch = true`, una fecha posterior a la fecha UTC actual deberá
producir `REVIEW_REQUIRED / ERR_FECHA_FUTURA`. Una fecha cuyo año sea anterior al
año UTC actual deberá producir `REVIEW_REQUIRED / ERR_FECHA_ANTIGUA`. Una fecha
del mismo año no se considera antigua por esta regla.

---

## RF-039 — Análisis visual

El sistema deberá utilizar análisis visual para clasificar explícitamente ticket,
factura, no documento o desconocido, detectar indicios visibles de manipulación y
realizar la lectura principal de los datos semánticos directamente desde la imagen.

---

## RF-040 — Tipos de manipulación

El análisis visual deberá considerar:

- Tachaduras.
- Sobrescrituras.
- Correcciones manuales.
- Corrector.
- Modificaciones visibles sobre datos impresos.

---

## RF-041 — Exclusiones de manipulación

No se considerarán automáticamente manipulación:

- Firmas.
- Sellos.
- Subrayados.
- Círculos.
- Notas en márgenes.
- Marcas que no modifiquen información impresa.

---

## RF-042 — Comparación OCR / IA visual

El sistema deberá comparar fecha y total de OCR con la lectura independiente de la IA visual.

---

## RF-043 — Discrepancia de fecha

Una discrepancia verificable de fecha deberá producir:

```text
REVIEW_REQUIRED
DATE_MISMATCH
```

salvo que exista una regla de mayor prioridad.

---

## RF-044 — Discrepancia de total

Una discrepancia verificable de total deberá producir:

```text
REVIEW_REQUIRED
TOTAL_MISMATCH
```

salvo que exista una regla de mayor prioridad.

---

## RF-045 — Discrepancia en producto crítico

Si OCR e IA discrepan sobre un concepto que podría provocar rechazo:

```text
REVIEW_REQUIRED
```

El documento no se rechazará basándose únicamente en la interpretación de IA.

---

## RF-046 — Motor de reglas

Las reglas deberán aplicarse mediante código.

---

## RF-047 — Decisión final

La IA no deberá decidir directamente el estado final.

---

## RF-048 — Motivo

Toda decisión distinta de `APPROVED` deberá incluir un motivo comprensible.

---

## RF-049 — Trazabilidad

Cuando sea posible, el motivo deberá identificar el concepto o dato que ha provocado la decisión.

---

## RF-050 — Swagger

La API deberá proporcionar documentación interactiva mediante Swagger UI.

---

## RF-051 — Logging

Cada análisis deberá generar un registro técnico.

---

# 15. Reglas de negocio

## RN-001 — Decisión determinista

La decisión final se realizará mediante código.

La IA no seleccionará directamente el estado final.

---

## RN-002 — IA visual como lectura principal

La IA visual proporciona los datos semánticos y productos del ticket cuando los
lee directamente de la imagen. Fecha y total son los únicos campos contrastados
con OCR para la decisión automática.

---

## RN-003 — OCR como contraste independiente

OCR conserva evidencia textual para contrastar fecha y total, determinar
legibilidad y facilitar diagnóstico; no completa datos semánticos ausentes en la
lectura visual.

---

## RN-004 — No reconstrucción

Ninguna fuente podrá reconstruir dígitos o datos que no haya leído. Un dato
visual sin OCR se utiliza como lectura principal, pero no se marca como
corroborado.

---

## RN-005 — Discrepancias críticas

Las discrepancias en campos críticos deberán provocar:

```text
REVIEW_REQUIRED
```

cuando no exista una regla de mayor prioridad.

---

## RN-005a — Corroboración para aprobación

`APPROVED / OK` requiere `DateMatch = true` y `TotalMatch = true`. La IA visual
sigue siendo la lectura principal de ambos valores, pero OCR debe corroborarlos
para aprobar automáticamente. Además, el documento no podrá estar clasificado
visualmente como `NO_DOCUMENTO` ni incumplir otra regla de mayor prioridad.

---

## RN-006 — Fecha

La fecha no podrá reconstruirse mediante inferencias.

---

## RN-006a — Revisión temporal preventiva

La fecha corroborada se compara por día, usando `TimeProvider` y fecha UTC. La
regla de futuro se evalúa antes que la de año anterior y ambas devuelven revisión
manual, nunca rechazo.

---

## RN-007 — Total

El total deberá corresponder al importe identificado realmente como total o importe pagado.

---

## RN-008 — Alcohol

Una bebida alcohólica deberá provocar:

```text
REJECTED
ERR_BEBIDA_ALCOHOLICA
```

cuando exista evidencia suficiente de que corresponde a un producto comprado.

---

## RN-009 — Asociación semántica

Una asociación semántica aislada no podrá provocar rechazo.

Ejemplo:

```text
CEREZAS
```

no puede convertirse en:

```text
LICOR DE CEREZAS
```

---

## RN-010 — Evidencia para alcohol

La clasificación como alcohol deberá estar respaldada por una línea real de producto.

---

## RN-011 — Nombre del establecimiento

Ejemplo:

```text
Bar La Cerveza
```

no deberá generar rechazo por alcohol únicamente por su nombre.

---

## RN-012 — Empleados

Ejemplo:

```text
Empleado: Vino
```

no deberá generar rechazo por alcohol.

---

## RN-013 — Producto alcohólico real

Ejemplo:

```text
1 CERVEZA MAHOU 3,50 €
```

podrá provocar:

```text
ERR_BEBIDA_ALCOHOLICA
```

---

## RN-014 — Producto ambiguo

Un producto que no pueda clasificarse con suficiente seguridad se considerará neutro.

---

## RN-015 — Texto original

La descripción normalizada de un producto no podrá modificar su significado esencial.

---

## RN-016 — Restauración

Un concepto propio de restauración deberá considerarse válido salvo evidencia clara en contra.

---

## RN-017 — Supermercado

En supermercados deberá analizarse si el producto está destinado principalmente al consumo inmediato.

---

## RN-018 — Contexto

No deberán aplicarse reglas de productos crudos de supermercado a platos servidos en restaurantes.

Ejemplo:

```text
CHULETÓN
```

en un restaurante se considera un plato válido.

---

## RN-019 — Nombre completo

Los productos deberán clasificarse utilizando el nombre completo.

Ejemplo:

```text
HAMBURGUESA PLÁSTICA
```

no deberá interpretarse como un objeto de plástico cuando el contexto indique que corresponde a una hamburguesa.

---

## RN-020 — Mayoría

`ERR_TIPO_GASTO_INCOHERENTE` deberá utilizarse cuando la mayoría de la compra sea claramente incompatible con el tipo de gasto.

Cuando existan importes individuales, se valorará preferentemente el peso económico; en caso contrario, el número de líneas.

---

## RN-021 — Artículo secundario

Un único artículo secundario incompatible no deberá rechazar automáticamente el gasto cuando el conjunto sea coherente.

---

## RN-022 — Excepción de alcohol

Una bebida alcohólica confirmada podrá provocar rechazo aunque el resto de la compra sea válida.

---

## RN-023 — Manipulación

Los indicios visuales claros de modificación de información impresa podrán provocar:

```text
ERR_DOCUMENTO_MANIPULADO
```

---

## RN-024 — Autenticidad

La ausencia de indicios de manipulación no significa que el documento haya sido certificado como auténtico.

---

## RN-025 — Prioridad de errores

El motor de reglas utilizará inicialmente la siguiente prioridad:

```text
1. DOCUMENT_TYPE_MISMATCH, ante una clasificación visual `NO_DOCUMENTO` y un `TicketData` marcado como ticket o factura
2. ERR_NO_DOCUMENTO, ante una clasificación visual explícita `NO_DOCUMENTO`
3. ERR_NO_LEGIBLE, solo sin OCR y sin lectura visual suficiente
4. ERR_DOCUMENTO_MANIPULADO
5. ERR_BEBIDA_ALCOHOLICA
6. ERR_TIPO_GASTO_INCOHERENTE
7. DATE_MISMATCH
8. TOTAL_MISMATCH
9. ERR_SIN_TOTAL, solo sin total visual ni OCR
10. ERR_SIN_FECHA, solo sin fecha visual ni OCR
11. OCR_LOW_CONFIDENCE, con un campo crítico exclusivo de OCR u OCR nulo con evidencia visual suficiente
12. ERR_FECHA_FUTURA, solo con DateMatch = true
13. ERR_FECHA_ANTIGUA, solo con DateMatch = true y año anterior
14. ERR_SIN_CIF, para Meals/Diet/Breakfast/Lunch/Dinner/Material sin TaxId
15. OK
```

---

# 16. Estados generales

## APPROVED

El documento cumple las reglas y existe evidencia suficiente.

---

## REJECTED

Se ha comprobado una regla que obliga a rechazar el documento.

---

## REVIEW_REQUIRED

Existe evidencia, pero una discrepancia o incertidumbre impide tomar una decisión automática fiable.

---

## UNREADABLE

El documento no proporciona evidencia suficiente para ser leído o validado.

---

## PROCESSING_ERROR

Ha ocurrido un error técnico durante el procesamiento.

---

# 17. Códigos de resultado

```text
OK

ERR_NO_DOCUMENTO
ERR_NO_LEGIBLE
ERR_DOCUMENTO_MANIPULADO
ERR_BEBIDA_ALCOHOLICA
ERR_TIPO_GASTO_INCOHERENTE
ERR_SIN_TOTAL
ERR_SIN_FECHA

DOCUMENT_TYPE_MISMATCH
DATE_MISMATCH
TOTAL_MISMATCH
OCR_LOW_CONFIDENCE
ERR_SIN_CIF
ERR_FECHA_ANTIGUA
ERR_FECHA_FUTURA
```

---

# 18. Modelo interno

El sistema utilizará un modelo interno sencillo para desacoplar Tesseract, OpenAI, las reglas y la API.

Objetos previstos:

```text
TicketData
ProductData
AddressData
VatData
VerificationData
AnalysisDecision
```

---

## 18.1 TicketData

Campos iniciales:

```text
documentType
establishmentName
establishmentType
taxId
invoiceNumber
date
time
total
address
vatDetails
products
```

---

## 18.2 ProductData

Campos:

```text
concept
normalizedText
amount
category
isAlcohol
```

---

## 18.3 VerificationData

Campos iniciales:

```text
ocrReadable
visualDocumentType

dateMatch
ocrDate
visualDate

totalMatch
ocrTotal
visualTotal

manipulationDetected
```

---

# 19. Contrato REST preliminar

Endpoint:

```http
POST /api/v1/tickets/analyze
```

Content-Type:

```text
multipart/form-data
```

Campos:

```text
file
expenseType
```

---

## 19.1 Ejemplo de respuesta aprobada

```json
{
  "analysisId": "b6436d26-2368-4cbc-80d9-c1e8cf494909",
  "status": "APPROVED",
  "reasonCode": "OK",
  "message": null,
  "ticket": {
    "documentType": "TICKET",
    "establishmentName": "Restaurante Ejemplo",
    "taxId": "B12345678",
    "invoiceNumber": null,
    "date": "2026-08-14",
    "time": "14:30",
    "total": 18.50,
    "products": []
  },
  "verification": {
    "ocrReadable": true,
    "visualDocumentType": "TICKET",
    "dateMatch": true,
    "totalMatch": true,
    "manipulationDetected": false
  }
}
```

---

## 19.2 Ejemplo de revisión por fecha

```json
{
  "status": "REVIEW_REQUIRED",
  "reasonCode": "DATE_MISMATCH",
  "message": "Existe una discrepancia entre OCR e IA en la fecha.",
  "ticket": {
    "documentType": "TICKET",
    "date": "2026-08-17",
    "total": 18.50,
    "products": []
  },
  "verification": {
    "ocrReadable": true,
    "dateMatch": false,
    "ocrDate": "2026-08-14",
    "visualDate": "2026-08-17",
    "totalMatch": true,
    "manipulationDetected": false
  }
}
```

---

# 20. Requisitos no funcionales

## RNF-001 — Lenguaje y framework

La solución se desarrollará en C# sobre .NET / ASP.NET Core.

---

## RNF-002 — Arquitectura

La solución se dividirá en:

```text
Domain
Application
Infrastructure
Api
UnitTests
IntegrationTests
```

---

## RNF-003 — SOLID

La implementación deberá respetar los principios SOLID.

---

## RNF-004 — Dependencias

Las capas superiores deberán depender de abstracciones cuando interactúen con servicios externos.

---

## RNF-005 — OCR desacoplado

Tesseract deberá implementarse detrás de una interfaz.

---

## RNF-006 — IA desacoplada

OpenAI deberá implementarse detrás de interfaces.

---

## RNF-007 — Testabilidad

Las reglas deberán poder probarse sin ejecutar Tesseract ni realizar llamadas reales a OpenAI.

---

## RNF-008 — Testing

Se utilizará xUnit.

---

## RNF-009 — Swagger

La API deberá estar documentada mediante OpenAPI y Swagger UI.

---

## RNF-010 — Docker

La API deberá poder ejecutarse mediante Docker.

---

## RNF-011 — Render

Render es el destino final de despliegue público mediante Docker y la rama
`main`.

---

## RNF-012 — Secretos

Las claves de OpenAI no deberán almacenarse en:

- Código.
- Git.
- README.
- Tests.
- Logs.

Se utilizará `OpenAI__ApiKey` en Render. La aplicación admite también:

```text
OPENAI_API_KEY
```

como variable de entorno.

---

## RNF-013 — Logs

Los logs deberán incluir como mínimo:

```text
Timestamp
AnalysisId
ExpenseType
Status
ReasonCode
DurationMs
Error
```

---

## RNF-014 — Imágenes

Las imágenes procesadas no se almacenarán como requisito del MVP.

---

## RNF-015 — Rendimiento

El tiempo de análisis deberá ser razonable para una operación interactiva sobre un ticket individual.

No se establece inicialmente un SLA concreto.

---

## RNF-016 — Mantenibilidad

OCR, IA, reglas, análisis visual y API deberán permanecer suficientemente desacoplados.

---

# 21. Casos problemáticos prioritarios

## CP-01 — Ticket nítido

Entrada:

Ticket correctamente orientado y legible.

Resultado esperado:

```text
OCR correcto
IA coincide
Reglas aplicadas normalmente
```

---

## CP-02 — Ticket girado 90 grados

Resultado esperado:

```text
Detectar orientación
Rotar
Ejecutar OCR
```

---

## CP-03 — Foto lejana

Resultado esperado:

No inventar datos cuando la resolución no sea suficiente.

---

## CP-04 — Ticket borroso

Si OCR está vacío y la IA visual tampoco obtiene tipo de documento, fecha, total
ni evidencia visual suficiente:

```text
UNREADABLE
o
REVIEW_REQUIRED
```

según la evidencia disponible.

---

## CP-05 — Discrepancia de fecha

```text
OCR:
14/08/2026

IA:
17/08/2026
```

Resultado:

```text
REVIEW_REQUIRED
DATE_MISMATCH
```

---

## CP-06 — Discrepancia de total

Resultado:

```text
REVIEW_REQUIRED
TOTAL_MISMATCH
```

---

## CP-07 — Cerezas

Producto:

```text
CEREZAS
```

Resultado:

```text
No alcohol
No rechazo
```

---

## CP-08 — Licor de cerezas

Producto:

```text
LICOR DE CEREZAS
```

Resultado esperado:

```text
Alcohol
```

si existe evidencia suficiente.

---

## CP-09 — Nombre del establecimiento

```text
Bar La Cerveza
```

Resultado:

No rechazar por alcohol únicamente por el nombre del establecimiento.

---

## CP-10 — Nombre de empleado

```text
Empleado: Vino
```

Resultado:

No rechazar por alcohol.

---

## CP-11 — Cerveza facturada

```text
1 CERVEZA MAHOU 3,50 €
```

Resultado:

```text
REJECTED
ERR_BEBIDA_ALCOHOLICA
```

si existe evidencia suficiente.

---

## CP-12 — Chuletón en restaurante

Resultado:

Producto válido para un gasto de comida.

---

## CP-13 — Carne cruda en supermercado

Resultado:

Producto incompatible con consumo inmediato.

---

## CP-14 — Hamburguesa plástica

```text
HAMBURGUESA PLÁSTICA
```

en contexto de restauración.

Resultado:

No interpretar como artículo de plástico.

---

## CP-15 — Producto desconocido

Resultado:

Producto neutro.

No provocar rechazo automáticamente.

---

## CP-16 — Tachadura visible

Resultado:

Analizar como indicio de manipulación.

---

## CP-17 — Firma

Una firma que no modifica información impresa:

```text
No manipulación
```

---

## CP-18 — Ticket inclinado

El fixture sintético con una inclinación aproximada de 10 grados se conserva
como caso observado de una limitación conocida del OCR. En las pruebas actuales
Tesseract no obtiene evidencia textual de ese fixture.

El MVP no aplicará corrección fina de inclinación/skew ni incorporará
OpenCV/OpenCvSharp para resolver este caso. No se debe completar la evidencia
ausente mediante IA.

---

# 22. Logging

El MVP utilizará logs en fichero.

Se registrará:

```text
Timestamp
AnalysisId
ExpenseType
Status
ReasonCode
DurationMs
Error
```

No se registrarán:

```text
OPENAI_API_KEY
Credenciales
Secretos
Imagen del ticket
```

---

# 23. Persistencia

El proyecto no requiere una base de datos funcional.

La persistencia se limita a información técnica y de auditoría.

No se almacenarán las imágenes como parte del MVP.

---

# 24. Swagger

Swagger UI será suficiente como interfaz técnica del MVP.

Debe permitir:

1. Seleccionar un archivo.
2. Indicar el tipo de gasto.
3. Ejecutar el análisis.
4. Consultar la respuesta JSON.

La aplicación incluye una web estática auxiliar en `/`, disponible en español,
que consume el mismo endpoint REST y facilita la demostración.

---

# 25. Docker

La API deberá empaquetarse en Docker.

El contenedor deberá incluir:

- Runtime de .NET.
- Aplicación TicketValidator.
- Tesseract.
- Datos del idioma español necesarios para Tesseract.

Los secretos se proporcionarán mediante variables de entorno.

---

# 26. Render

El despliegue final utiliza un Render Web Service conectado a la rama `main`.

Flujo desplegado:

```text
GitHub
   ↓
Render
   ↓
Docker build
   ↓
TicketValidator.Api
```

La clave se configura mediante `OpenAI__ApiKey` y no se incorpora al código ni
a la imagen Docker.

- Web pública: https://ticketvalidator-juo1.onrender.com
- Swagger público: https://ticketvalidator-juo1.onrender.com/swagger

---

# 27. Criterios de aceptación

El MVP se considerará completado cuando:

- La solución compile correctamente.
- La API REST pueda ejecutarse.
- El endpoint acepte JPEG y PNG.
- El endpoint reciba `expenseType`.
- Swagger permita probar el endpoint.
- Se ejecute la corrección de orientación antes del OCR.
- Tesseract extraiga texto.
- El texto OCR se conserve como evidencia.
- GPT-4.1 visual pueda extraer los datos estructurados de la imagen seleccionada.
- Exista análisis visual de indicios de manipulación y productos.
- Fecha y total puedan compararse entre OCR e IA.
- Las discrepancias produzcan `REVIEW_REQUIRED`.
- La lectura visual de fecha y total se conserve aunque OCR no obtenga esos datos.
- Las reglas se ejecuten mediante código.
- El caso `CEREZAS` no sea rechazado como alcohol.
- Una cerveza real pueda provocar rechazo.
- Existan pruebas xUnit representativas.
- La suite automatizada finalice con sus 233 tests correctos.
- Los tests de reglas no necesiten OpenAI real.
- Existan logs técnicos.
- No se almacenen imágenes como requisito del MVP.
- La API pueda ejecutarse mediante Docker.
- El proyecto esté desplegado públicamente en Render mediante Docker.
- El código esté disponible en GitHub.
- El repositorio incluya README y documentación técnica.

---

# 28. Decisiones cerradas

```text
Core:
Servicio web REST

Lenguaje:
C#

Framework:
ASP.NET Core

OCR:
Tesseract

IA:
OpenAI GPT-4.1

Testing:
xUnit

Entrada:
JPEG / PNG

Preprocesamiento:
Orientación / rotación, sin corrección fina de inclinación/skew ni
OpenCV/OpenCvSharp

Documentación API:
OpenAPI / Swagger UI

Contenedor:
Docker

Hosting:
Render

Persistencia:
Logs y auditoría técnica

Logs:
Fichero

Manipulación:
Análisis visual

IA visual:
Fuente estructurada principal del documento y de sus productos

OCR:
Legibilidad, RawText, fecha, total, contraste y diagnóstico

Decisión:
Motor de reglas en código

Discrepancias:
REVIEW_REQUIRED

Web demo:
Incluida en español y desplegada en la ruta raíz
```

---

# 29. Decisiones pendientes de experimentación

## Confianza OCR

Los umbrales de confianza deberán probarse utilizando tickets reales.

No se establecerá un porcentaje arbitrario durante el diseño.

Las pruebas deberán incluir:

```text
Ticket nítido
Ticket girado
Ticket lejano
Ticket borroso
Fecha difícil de leer
Total difícil de leer
```

---

# 30. Mejoras futuras

Una vez completado el MVP se podrán estudiar:

- Corrección fina de inclinación/skew.
- Corrección de perspectiva.
- Recorte automático.
- Mejora de contraste.
- Eliminación de ruido.
- Restauración de imagen.
- Calibración avanzada de Tesseract.
- Diccionarios adicionales.
- Reglas más completas por tipo de gasto.
- Aplicación móvil para captura guiada.
- Detección de bordes del ticket.
- Optimización del consumo y coste de IA.
- C2PA.
- EXIF.
- Persistencia de auditoría.
- Base de datos.
- Autenticación.
- Web de demostración más completa.
- Métricas.
- Observabilidad.
- Pruebas de rendimiento.

Estas mejoras no son necesarias para completar el MVP.

---

# 31. Principio final

Ante cualquier duda durante la implementación deberá mantenerse el siguiente criterio:

```text
IA visual = fuente estructurada principal

OCR = legibilidad, RawText, fecha, total, contraste y diagnóstico

Código = decisión
```

El objetivo del proyecto no es conseguir que la IA proporcione siempre una respuesta.

El objetivo es conseguir que el sistema **no tome una decisión no respaldada por evidencia suficiente**.
