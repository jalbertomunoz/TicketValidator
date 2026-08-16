# Requisitos de TicketValidator

## 1. Propósito

Este documento define los requisitos funcionales, reglas de negocio, requisitos no funcionales, alcance y criterios de aceptación del MVP de **TicketValidator**.

TicketValidator es un servicio web REST para analizar imágenes de tickets y facturas de gasto.

El servicio recibe:

- Una imagen del documento.
- El tipo de gasto indicado por el usuario.

A partir de esta información:

1. Obtiene evidencia textual mediante OCR.
2. Interpreta la información mediante Inteligencia Artificial.
3. Contrasta los campos críticos.
4. Analiza indicios visuales de manipulación.
5. Aplica reglas de negocio deterministas.
6. Devuelve una decisión estructurada y explicable.

El principio fundamental del sistema es:

```text
OCR = evidencia

IA = interpretación

Código = decisión
```

La Inteligencia Artificial no debe ser la única fuente de verdad para los campos críticos del documento.

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
- Corrección de orientación/rotación, sin corrección fina de inclinación/skew.
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
image/jpeg
image/png
```

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
Orientación / rotación
  ↓
Tesseract OCR
  ↓
Conservación de evidencia OCR
  ↓
┌────────────────────────────┐
│                            │
▼                            ▼
Extracción IA        Análisis visual IA
│                            │
└─────────────┬──────────────┘
              ↓
      Verificación OCR / IA
              ↓
       Motor de reglas
              ↓
          Decisión
              ↓
          Auditoría
              ↓
       Respuesta REST
```

---

# 10. Política de evidencia

El OCR será la fuente principal de evidencia textual.

La IA se utilizará para:

- Interpretar.
- Normalizar.
- Estructurar.
- Clasificar.
- Comprender contexto.

La IA de extracción basada en texto OCR estructura esa evidencia, pero no es una segunda fuente independiente para fecha ni total. La IA visual lee esos campos directamente desde la imagen y se contrasta con OCR.

La IA no podrá sustituir automáticamente información ausente en OCR cuando dicha información sea necesaria para aprobar o rechazar un documento.

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

## 12.1 OCR e IA coinciden

```text
OCR claro
+
IA coincide

→ campo verificado
```

---

## 12.2 OCR e IA discrepan

Cuando OCR e IA proporcionen valores diferentes para un campo crítico:

```text
→ REVIEW_REQUIRED
```

Ejemplo:

```text
OCR:
14/08/2026

IA:
17/08/2026

Resultado:
REVIEW_REQUIRED
DATE_MISMATCH
```

---

## 12.3 OCR insuficiente e IA propone un dato

Ejemplo:

```text
OCR:
1?/08/2026

IA:
14/08/2026
```

El sistema no considerará automáticamente:

```text
14/08/2026
```

como información verificada.

---

## 12.4 OCR no detecta el dato

Si OCR no encuentra evidencia de un campo y la IA proporciona un valor:

```text
→ el valor de IA no constituye evidencia suficiente por sí solo
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

El sistema deberá intentar detectar la orientación del documento antes de realizar OCR.

---

## RF-005 — Rotación

El sistema deberá poder corregir documentos orientados a:

```text
0°
90°
180°
270°
```

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

## RF-009 — Extracción IA

El sistema deberá utilizar GPT-4.1 para transformar el texto OCR en una representación estructurada.

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
Si la visión indica `NO_DOCUMENTO` pero el tipo estructurado desde OCR es ticket o factura, el resultado será revisión humana mediante `DocumentTypeMismatch`, no rechazo automático.

---

## RF-012 — Empresa

El sistema deberá intentar extraer el nombre del establecimiento o empresa.

---

## RF-013 — CIF

El sistema deberá intentar extraer el identificador fiscal cuando exista.

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
codigoPostal
localidad
provincia
restoDireccion
```

---

## RF-019 — IVA

El sistema deberá intentar extraer todos los desgloses de IVA existentes.

Por cada elemento:

```text
baseImponible
tipo
cuota
```

---

## RF-020 — Productos

El sistema deberá intentar extraer las líneas correspondientes a:

- Productos.
- Artículos.
- Consumiciones.
- Servicios facturados.

---

## RF-021 — Texto OCR de producto

Cada producto deberá conservar el texto OCR original cuando sea posible.

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

Si el documento es válido pero no puede determinarse la fecha:

```text
ERR_SIN_FECHA
```

---

## RF-036 — Total obligatorio

Si el documento es válido pero no puede determinarse el total:

```text
ERR_SIN_TOTAL
```

---

## RF-037 — Normalización de fecha

Cuando la fecha pueda verificarse correctamente deberá devolverse en formato:

```text
dd/MM/yyyy
```

---

## RF-038 — Prohibición de reconstrucción de fecha

El sistema no deberá completar dígitos de una fecha que no puedan leerse con suficiente seguridad.

---

## RF-039 — Análisis visual

El sistema deberá utilizar análisis visual para clasificar explícitamente ticket, factura, no documento o desconocido, detectar indicios visibles de manipulación y realizar una lectura independiente de fecha y total directamente desde la imagen.

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

## RN-002 — OCR como evidencia

El OCR prevalece como fuente de evidencia textual.

---

## RN-003 — IA como interpretación

La IA podrá interpretar información existente, pero no sustituir evidencia ausente.

---

## RN-004 — Información inventada

Un dato proporcionado exclusivamente por IA no deberá considerarse automáticamente verificado.

---

## RN-005 — Discrepancias críticas

Las discrepancias en campos críticos deberán provocar:

```text
REVIEW_REQUIRED
```

cuando no exista una regla de mayor prioridad.

---

## RN-006 — Fecha

La fecha no podrá reconstruirse mediante inferencias.

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
1. ERR_NO_DOCUMENTO, solo ante una clasificación visual explícita `NO_DOCUMENTO` sin evidencia OCR contradictoria de ticket o factura
2. ERR_NO_LEGIBLE
3. ERR_DOCUMENTO_MANIPULADO
4. ERR_BEBIDA_ALCOHOLICA
5. ERR_TIPO_GASTO_INCOHERENTE
6. ERR_SIN_TOTAL
7. ERR_SIN_FECHA
8. DATE_MISMATCH
9. TOTAL_MISMATCH
10. OK
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

DATE_MISMATCH
TOTAL_MISMATCH
OCR_LOW_CONFIDENCE
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
companyName
companyTaxId
invoiceNumber
date
time
total
address
vat
products
```

---

## 18.2 ProductData

Campos:

```text
ocrText
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
  "status": "APPROVED",
  "reasonCode": "OK",
  "message": null,
  "ticket": {
    "documentType": "TICKET",
    "companyName": "Restaurante Ejemplo",
    "companyTaxId": "B12345678",
    "invoiceNumber": null,
    "date": "14/08/2026",
    "time": "14:30",
    "total": 18.50,
    "products": []
  },
  "verification": {
    "ocrReadable": true,
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
    "date": null,
    "total": 18.50,
    "products": []
  },
  "verification": {
    "ocrReadable": true,
    "dateMatch": false,
    "ocrDate": "14/08/2026",
    "aiDate": "17/08/2026",
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

Render será el destino inicial de despliegue.

---

## RNF-012 — Secretos

Las claves de OpenAI no deberán almacenarse en:

- Código.
- Git.
- README.
- Tests.
- Logs.

Se utilizará:

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

Si la IA propone una fecha que no puede verificarse mediante OCR:

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

Una web adicional será opcional.

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

Render será el proveedor objetivo de despliegue.

Flujo previsto:

```text
GitHub
   ↓
Render
   ↓
Docker build
   ↓
TicketValidator.Api
```

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
- GPT-4.1 pueda interpretar el contenido OCR.
- Exista análisis visual de indicios de manipulación.
- Fecha y total puedan compararse entre OCR e IA.
- Las discrepancias produzcan `REVIEW_REQUIRED`.
- La IA no pueda introducir como evidencia datos ausentes en OCR.
- Las reglas se ejecuten mediante código.
- El caso `CEREZAS` no sea rechazado como alcohol.
- Una cerveza real pueda provocar rechazo.
- Existan pruebas xUnit representativas.
- Los tests de reglas no necesiten OpenAI real.
- Existan logs técnicos.
- No se almacenen imágenes como requisito del MVP.
- La API pueda ejecutarse mediante Docker.
- El proyecto quede preparado para desplegarse en Render.
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

OCR:
Fuente principal de evidencia textual

IA:
Interpretación semántica

Decisión:
Motor de reglas en código

Discrepancias:
REVIEW_REQUIRED

Web demo:
Opcional
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
OCR = evidencia

IA = interpretación

Código = decisión
```

El objetivo del proyecto no es conseguir que la IA proporcione siempre una respuesta.

El objetivo es conseguir que el sistema **no tome una decisión no respaldada por evidencia suficiente**.
