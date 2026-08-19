# Arquitectura de TicketValidator

## 1. Propósito

Este documento describe la arquitectura técnica de **TicketValidator**, un servicio web REST para el análisis y validación de tickets y facturas de gasto.

La arquitectura busca cumplir cuatro objetivos principales:

- Separar claramente responsabilidades.
- Facilitar las pruebas unitarias.
- Evitar acoplamiento con proveedores externos.
- Mantener el MVP sencillo y mantenible.

El sistema sigue como principio fundamental:

```text
IA visual = fuente estructurada principal

OCR = legibilidad, RawText, fecha, total, contraste y diagnóstico

Código = decisión
```

La política se ajustó tras pruebas experimentales con tickets reales: la lectura
visual de GPT-4.1 resultó más fiable para estructurar el documento cuando
Tesseract pierde información. El código conserva OCR como fuente independiente
para fecha y total y toma la decisión final.

---

# 2. Arquitectura general

La solución se organiza en cuatro proyectos principales:

```text
TicketValidator
│
├── TicketValidator.Domain
├── TicketValidator.Application
├── TicketValidator.Infrastructure
└── TicketValidator.Api
```

Y dos proyectos de pruebas:

```text
TicketValidator.UnitTests
TicketValidator.IntegrationTests
```

La estructura completa del repositorio será:

```text
TicketValidator/
│
├── src/
│   ├── TicketValidator.Domain/
│   ├── TicketValidator.Application/
│   ├── TicketValidator.Infrastructure/
│   └── TicketValidator.Api/
│
├── tests/
│   ├── TicketValidator.UnitTests/
│   └── TicketValidator.IntegrationTests/
│
├── docs/
│   ├── REQUIREMENTS.md
│   └── ARCHITECTURE.md
│
├── samples/
│
├── AGENTS.md
├── README.md
├── .gitignore
├── Dockerfile
└── TicketValidator.sln
```

---

# 3. Dependencias entre capas

Las dependencias deben orientarse hacia el núcleo de la aplicación.

```text
                ┌──────────────────────┐
                │ TicketValidator.Api  │
                └──────────┬───────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │    Application       │
                └──────────┬───────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │       Domain         │
                └──────────────────────┘

                ┌──────────────────────┐
                │   Infrastructure     │
                └──────────┬───────────┘
                           │
                   implementa interfaces
                           │
                           ▼
                      Application
```

Dependencias permitidas:

```text
Domain
→ no depende de ningún proyecto de la solución

Application
→ Domain

Infrastructure
→ Application
→ Domain

Api
→ Application
→ Infrastructure

UnitTests
→ Domain
→ Application

IntegrationTests
→ Api
→ Infrastructure
```

No se permiten dependencias como:

```text
Domain → Infrastructure
Domain → Api
Application → Infrastructure
Application → Api
```

---

# 4. TicketValidator.Domain

## 4.1 Responsabilidad

`TicketValidator.Domain` contiene el modelo y las reglas puras del negocio.

No debe conocer:

- ASP.NET Core.
- HTTP.
- Tesseract.
- OpenAI.
- Docker.
- Render.
- Ficheros.
- Variables de entorno.
- Swagger.
- Sistemas externos.

La capa Domain debe poder compilar y probarse independientemente de cualquier infraestructura.

---

## 4.2 Estructura prevista

```text
TicketValidator.Domain/
│
├── Entities/
│   ├── TicketData.cs
│   ├── ProductData.cs
│   ├── AddressData.cs
│   ├── VatData.cs
│   └── VerificationData.cs
│
├── Enums/
│   ├── DocumentType.cs
│   ├── AnalysisStatus.cs
│   ├── ReasonCode.cs
│   ├── ExpenseType.cs
│   ├── ProductCategory.cs
│   └── EstablishmentType.cs
│
├── Rules/
│   ├── AlcoholRule.cs
│   ├── ExpenseCoherenceRule.cs
│   ├── RequiredDateRule.cs
│   ├── RequiredTotalRule.cs
│   └── ManipulationRule.cs
│
└── Results/
    ├── RuleResult.cs
    └── AnalysisDecision.cs
```

Esta estructura podrá ajustarse durante la implementación si una clase no aporta valor real.

---

# 5. Modelo interno

El servicio utilizará un modelo interno propio para desacoplar:

- Resultado de Tesseract.
- Resultado de OpenAI.
- Reglas de negocio.
- Respuesta HTTP.

Esto evita que el resto de la aplicación dependa directamente del formato de un proveedor externo.

---

## 5.1 TicketData

Representa un ticket o factura interpretado.

Campos previstos:

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

`AnalyzeTicketHandler` construye `TicketData` a partir de la lectura visual:
tipo de documento, emisor, CIF, número, hora, dirección, IVA, productos, fecha y
total. OCR solo aporta evidencia independiente para contrastar fecha y total,
determinar legibilidad y facilitar diagnóstico; no completa campos visuales
ausentes.
Las líneas de producto se extraen exclusivamente de la imagen. Después,
`IProductClassifier` conserva `concept`, `normalizedText` y `amount` y añade
`category` e `isAlcohol`. Los productos clasificados se incorporan a
`TicketData` y se envían a `IExpenseCoherenceAnalyzer`.
`APPROVED` requiere un documento no clasificado como `NotDocument`,
`DateMatch = true` y `TotalMatch = true`, por lo que los dos campos críticos
deben estar corroborados por OCR e IA visual.
Con una fecha corroborada, el motor revisa anomalías temporales preventivas:
futuro respecto a la fecha UTC actual o año anterior, sin rechazo automático.
El tipo de documento también procede de la lectura visual.

---

## 5.2 ProductData

Representa una línea de producto o servicio.

Campos previstos:

```text
concept
normalizedText
amount
category
isAlcohol
```

El campo:

```text
concept
```

conserva el concepto visible de una línea realmente facturada.

El campo:

```text
normalizedText
```

representa una interpretación del contenido.

La normalización nunca debe introducir un concepto diferente del original.

Ejemplo no permitido:

```text
Concepto:
CEREZAS

Normalized:
LICOR DE CEREZAS
```

---

## 5.3 VerificationData

Representa el resultado de contrastar evidencias.

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

Este modelo podrá ampliarse únicamente cuando las pruebas reales demuestren que se necesita más información.

---

# 6. TicketValidator.Application

## 6.1 Responsabilidad

Application contiene los casos de uso y la coordinación del sistema.

Define qué servicios necesita la aplicación, pero no cómo se implementan.

Contiene:

- Casos de uso.
- Interfaces.
- DTOs internos.
- Verificación.
- Orquestación.
- Motor de reglas cuando requiera coordinación de varias reglas.

No debe contener:

- Código Tesseract.
- Llamadas HTTP concretas a OpenAI.
- Código ASP.NET.
- Escritura directa en ficheros.
- Dependencias específicas de Render.

---

## 6.2 Estructura prevista

```text
TicketValidator.Application/
│
├── Abstractions/
│   ├── IOcrService.cs
│   ├── IOcrOrientationService.cs
│   ├── IDocumentOrientationService.cs
│   ├── IVisualAnalysisService.cs
│   ├── IProductClassifier.cs
│   ├── IExpenseCoherenceAnalyzer.cs
│   ├── ITicketVerificationService.cs
│   ├── IExpenseRuleEngine.cs
│   └── IAuditLogger.cs
│
├── UseCases/
│   └── AnalyzeTicket/
│       ├── AnalyzeTicketCommand.cs
│       ├── AnalyzeTicketHandler.cs
│       └── AnalyzeTicketResult.cs
│
├── DTOs/
│   ├── OcrResult.cs
│   ├── OcrWord.cs
│   ├── OcrOrientationResult.cs
│   ├── VisualAnalysisResult.cs
│   ├── ExpenseCoherenceResult.cs
│   └── VerificationResult.cs
│
└── Services/
    ├── OcrEvidenceAnalyzer.cs
    ├── TicketVerificationService.cs
    └── ExpenseRuleEngine.cs
```

---

## 6.3 Interfaces principales

Inicialmente se contemplan:

```text
IOcrService
IOcrOrientationService
IDocumentOrientationService
IVisualAnalysisService
IProductClassifier
IExpenseCoherenceAnalyzer
ITicketVerificationService
IExpenseRuleEngine
IAuditLogger
```

---

## 6.4 IOcrService

Responsable de obtener la evidencia textual del documento.

Conceptualmente:

```text
Imagen
  ↓
IOcrService
  ↓
OcrResult
```

`OcrResult` podrá contener:

```text
RawText
MeanConfidence
Words
```

Cuando resulte útil, cada palabra podrá contener:

```text
Text
Confidence
BoundingBox
```

No se fija inicialmente ningún umbral de confianza.

Los umbrales se determinarán posteriormente mediante pruebas con tickets reales.

`IOcrService` aporta exclusivamente `RawText`, palabras y confianza para
legibilidad, orientación, diagnóstico y extracción determinista de fecha y
total. No construye `TicketData` ni completa proveedor, CIF, dirección, número,
hora, IVA, productos o tipo de documento.

---

## 6.5 Extracción OCR residual

La extracción estructurada mediante IA sobre texto OCR no forma parte del flujo
activo. `IAiTicketExtractor`, `AiTicketExtraction` y `OpenAiTicketExtractor`
permanecen como componentes residuales pendientes de retirada, pero
`AnalyzeTicketHandler` no los invoca ni utiliza sus resultados. OCR nunca
completa campos ausentes de la lectura visual.

---

## 6.6 IVisualAnalysisService

Responsable de la lectura estructurada principal del MVP.

Clasifica explícitamente la imagen como ticket, factura, no documento o
desconocida. Extrae directamente tipo de documento, proveedor/emisor, tipo de
establecimiento, CIF/NIF/VAT del proveedor, dirección del proveedor, número,
fecha, hora, total, IVA, líneas facturadas e indicios de manipulación. Solo fecha
y total se contrastan con OCR. `Unknown` no equivale a `NotDocument`.

Analizará posibles indicios de:

- Tachaduras.
- Sobrescrituras.
- Correcciones manuales.
- Corrector.
- Modificaciones visibles del contenido impreso.

No constituye una herramienta forense.

No certifica la autenticidad del documento.

---

## 6.7 IDocumentOrientationService

Responsable exclusivamente de detectar la orientación del documento y aplicar la rotación necesaria.

El MVP contempla:

```text
0°
90°
180°
270°
```

La implementación `TesseractDocumentOrientationService` ejecuta Tesseract OSD
con `osd.traineddata` y `PageSegMode.OsdOnly`. Solo rota cuando la confianza
técnica de OSD alcanza 15, valor recomendado por el wrapper como razonablemente
confiable. Este valor no forma parte de la evidencia OCR ni de las reglas de
negocio.
Si OSD no dispone de evidencia suficiente para detectar la orientación, se
conserva la imagen original y el OCR normal continúa. Los demás errores técnicos
de Tesseract se propagan.

`FallbackOcrOrientationService` coordina OSD y OCR antes del caso de uso. Si el
primer OCR no tiene texto útil, reconoce menos de tres palabras, no obtiene fecha
o no obtiene total, reutiliza ese resultado como candidato 0° y evalúa además
90°, 180° y 270° en sentido horario respecto a la imagen producida por OSD.
Selecciona la mayor evidencia mediante palabras, confianza media y bonificaciones
por fecha y total detectables. En empates conserva el candidato inicial y,
después, la menor rotación. La imagen y el `OcrResult` seleccionados se entregan
juntos al handler, que envía esa misma imagen al análisis visual. Es un fallback
de robustez y no se activa cuando OCR aporta texto, fecha y total suficientes.

No forma parte del MVP:

- Corrección fina de inclinación/skew.
- OpenCV/OpenCvSharp para el preprocesamiento de imagen.
- Corrección de perspectiva.
- Contraste avanzado.
- Restauración.
- Eliminación de ruido.
- Mejora mediante IA.

El fixture sintético de ticket inclinado se conserva como caso observado de una
limitación conocida del OCR. No activa corrección fina de inclinación en esta
versión del MVP.

---

## 6.8 ITicketVerificationService

Responsable de comparar la evidencia OCR con la lectura independiente de la IA visual.
La lectura visual es la fuente principal de fecha y total; OCR es el contraste.
Una coincidencia marca `Match = true`, una ausencia de OCR deja `Match = null`
y una discrepancia marca `Match = false`.
`OcrReadable` solo expresa la existencia de texto OCR. Con OCR parcial, la
ausencia de fecha o total OCR conserva los valores visuales, pero requiere
`REVIEW_REQUIRED / OCR_LOW_CONFIDENCE`. Con OCR nulo, una lectura visual
suficiente también evita `ERR_NO_LEGIBLE` y requiere revisión al no existir
contraste independiente. `OcrReadable` no es suficiente para aprobar.

Campos críticos iniciales:

```text
Fecha
Total
Tipo de documento
Conceptos que puedan provocar rechazo
```

Ejemplo:

```text
OCR fecha:
14/08/2026

IA visual fecha:
14/08/2026

→ Match
```

Ejemplo:

```text
OCR fecha:
14/08/2026

IA visual fecha:
17/08/2026

→ DATE_MISMATCH
→ REVIEW_REQUIRED
```

---

## 6.9 IExpenseRuleEngine

Responsable de ejecutar las reglas y determinar el resultado final.

La coherencia semántica se obtiene previamente mediante `IExpenseCoherenceAnalyzer`. El motor recibe esa señal y decide de forma determinista `ERR_TIPO_GASTO_INCOHERENTE` solo cuando la mayoría de la compra es claramente incoherente.

El flujo de productos es único: `IVisualAnalysisService` extrae las líneas,
`IProductClassifier` añade categoría y alcohol,
`IExpenseCoherenceAnalyzer` evalúa el conjunto y `IExpenseRuleEngine` toma la
decisión final junto con las demás reglas.

La IA nunca selecciona directamente:

```text
APPROVED
REJECTED
REVIEW_REQUIRED
UNREADABLE
PROCESSING_ERROR
```

El motor de reglas lo determina mediante código.

---

# 7. Caso de uso principal

El caso de uso principal será:

```text
AnalyzeTicket
```

Se implementará conceptualmente mediante:

```text
AnalyzeTicketHandler
```

o una clase equivalente.

Su responsabilidad es coordinar el proceso completo.

---

## 7.1 Flujo del caso de uso

```text
Imagen
  ↓
Validación
  ↓
Tesseract OSD
  ↓
OCR inicial
  ↓
Si falta evidencia crítica: fallback 0°/90°/180°/270°
  ↓
Imagen seleccionada + OcrResult seleccionado
  ↓
IA visual estructurada sobre la misma imagen
  ↓
ProductClassifier
  ↓
TicketData + ExpenseCoherenceAnalyzer
  ↓
Verificación OCR/visual de fecha y total
  ↓
ExpenseRuleEngine
  ↓
Decisión, auditoría y respuesta
```

---

# 8. Flujo de datos

## 8.1 Entrada

La API recibe:

```text
file
expenseType
```

El fichero deberá ser:

```text
JPEG
PNG
```

---

## 8.2 Orientación y OCR

El fichero pasa por:

```text
IOcrOrientationService
```

y obtiene la imagen seleccionada junto con su `OcrResult`. Primero delega la
orientación en `IDocumentOrientationService` (OSD) y ejecuta OCR. Solo ante OCR
insuficiente prueba las cuatro rotaciones ortogonales y conserva la de mejor
evidencia. No corrige inclinación fina, perspectiva ni aplica OpenCV.

---

## 8.3 OCR

La selección de orientación procesa las imágenes candidatas mediante:

```text
IOcrService
```

implementado inicialmente por:

```text
TesseractOcrService
```

Salida:

```text
OcrResult
```

---

## 8.4 Evidencia OCR

El texto OCR no alimenta un extractor estructurado dentro del flujo activo. Se
conserva como `RawText` y `OcrEvidenceAnalyzer` obtiene de forma determinista
`OcrDate` y `OcrTotal`. Esta evidencia se utiliza para legibilidad, selección de
orientación, corroboración de fecha y total y diagnóstico; nunca completa
`TicketData`.

---

## 8.5 Análisis visual mediante IA

La imagen se envía de forma independiente a:

```text
IVisualAnalysisService
```

implementado por:

```text
OpenAiVisualAnalysisService
```

Esta operación analiza indicios visibles de manipulación y extrae directamente
tipo de documento, proveedor, CIF/NIF, dirección, número, fecha, hora, total,
IVA y productos como fuente principal. Sus productos se envían a
`IProductClassifier`; solo fecha y total se contrastan con OCR.

No aplica reglas de gasto ni decide el estado final.

---

# 9. Separación de prompts

No se utilizará un único prompt para resolver todas las responsabilidades.

La infraestructura dispondrá de prompts separados.

```text
Infrastructure/
└── AI/
    └── Prompts/
        ├── VisualAnalysisPrompt.cs
        ├── ProductClassificationPrompt.cs
        └── ExpenseCoherencePrompt.cs
```

---

## 9.1 VisualAnalysisPrompt

Responsable de la lectura visual estructurada y de los indicios de manipulación.
No clasifica semánticamente los productos ni decide la aceptación.

---

## 9.2 ProductClassificationPrompt

Responsable únicamente de clasificar conceptos cuando sea necesaria interpretación semántica.

Ejemplo:

```text
CEREZAS
→ FOOD
```

Ejemplo:

```text
CERVEZA MAHOU
→ ALCOHOL
```

La clasificación no puede modificar el texto original.

---

## 9.3 ExpenseCoherencePrompt

Responsable de evaluar los productos ya clasificados frente al tipo de gasto. No
aplica estados, códigos ni prioridades.

---

# 10. TicketValidator.Infrastructure

## 10.1 Responsabilidad

Infrastructure contiene las implementaciones concretas de las interfaces definidas en Application.

Aquí residen las dependencias externas.

---

## 10.2 Estructura prevista

```text
TicketValidator.Infrastructure/
│
├── OCR/
│   └── TesseractOcrService.cs
│
├── AI/
│   ├── OpenAiOptions.cs
│   ├── OpenAiVisualAnalysisService.cs
│   ├── OpenAiProductClassifier.cs
│   ├── OpenAiExpenseCoherenceAnalyzer.cs
│   └── Prompts/
│       ├── VisualAnalysisPrompt.cs
│       ├── ProductClassificationPrompt.cs
│       └── ExpenseCoherencePrompt.cs
│
├── ImageProcessing/
│   ├── TesseractDocumentOrientationService.cs
│   ├── FallbackOcrOrientationService.cs
│   └── OrthogonalImageRotation.cs
│
├── Logging/
│   └── FileAuditLogger.cs
│
└── DependencyInjection/
    └── InfrastructureServiceCollectionExtensions.cs
```

---

# 11. Tesseract OCR

La primera implementación de:

```text
IOcrService
```

será:

```text
TesseractOcrService
```

Tesseract se utilizará como fuente independiente de evidencia textual y
contraste para la lectura visual principal de fecha y total.

Se configurará inicialmente para idioma español.

La implementación intentará obtener:

- Texto completo.
- Confianza global.
- Confianza por palabra cuando resulte accesible.
- Posiciones cuando sean útiles.

No se utilizará inicialmente una confianza mínima fija.

---

# 12. OpenAI

La primera implementación de IA utilizará:

```text
GPT-4.1
```

La clave deberá obtenerse mediante:

```text
OPENAI_API_KEY
```

Nunca debe encontrarse en:

- Código.
- Git.
- README.
- appsettings versionado.
- Tests.
- Logs.

---

# 13. TicketValidator.Api

## 13.1 Responsabilidad

La API será una capa fina.

Responsabilidades:

- Recibir HTTP.
- Validar parámetros HTTP.
- Invocar Application.
- Mapear resultados.
- Devolver HTTP/JSON.
- Configurar inyección de dependencias.
- Publicar OpenAPI y Swagger.

No debe contener reglas de negocio.

La API puede servir una interfaz estática auxiliar desde `wwwroot` para pruebas
manuales y demostración. Esta interfaz solo consume el endpoint REST existente;
no forma parte del núcleo funcional ni contiene lógica de negocio.
Como ayuda diagnóstica temporal, la respuesta HTTP puede incluir el texto OCR
original en `verification.ocrRawText`; no se utiliza para reglas, persistencia
ni auditoría.

---

## 13.2 Estructura prevista

```text
TicketValidator.Api/
│
├── Controllers/
│   └── TicketsController.cs
│
├── Contracts/
│   ├── Requests/
│   │   └── AnalyzeTicketRequest.cs
│   └── Responses/
│       ├── AnalyzeTicketResponse.cs
│       ├── TicketResponse.cs
│       └── VerificationResponse.cs
│
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
│
├── Configuration/
│   ├── OcrOptions.cs
│   └── ValidationOptions.cs
│
├── Program.cs
└── appsettings.json
```

La estructura podrá simplificarse si alguna carpeta resulta innecesaria durante el MVP.

---

# 14. Endpoint principal

El endpoint principal será:

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

Formatos:

```text
image/jpeg
image/png
```

---

# 15. Respuesta REST

La respuesta tendrá conceptualmente esta estructura:

```json
{
  "status": "APPROVED",
  "reasonCode": "OK",
  "message": null,
  "ticket": {
    "documentType": "TICKET",
    "establishmentName": "Restaurante Ejemplo",
    "establishmentType": "Restaurante",
    "taxId": "B12345678",
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
    "manipulationDetected": false,
    "ocrRawText": "..."
  }
}
```

---

# 16. Estados generales

```text
APPROVED
REJECTED
REVIEW_REQUIRED
UNREADABLE
PROCESSING_ERROR
```

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

# 18. Política de discrepancias

## 18.1 Coincidencia

```text
IA visual
+
OCR coincide

→ campo verificado
```

---

## 18.2 Discrepancia

```text
OCR
+
IA visual diferente

→ REVIEW_REQUIRED
```

Ejemplo:

```text
OCR:
14/08/2026

IA visual:
17/08/2026

→ DATE_MISMATCH
```

---

## 18.3 OCR parcial no detecta el dato

```text
IA visual obtiene el dato
+
OCR obtiene texto, pero no ese campo

→ usar la lectura visual principal
→ `Match = null`, sin corroboración OCR
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

## 18.4 OCR nulo con lectura visual

```text
OCR no obtiene texto ni palabras
+
IA visual identifica ticket/factura y obtiene evidencia suficiente

→ conservar valores visuales
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

## 18.5 Solo OCR

```text
IA visual no obtiene el dato
+
OCR lo obtiene

→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

## 18.6 Fecha temporalmente sospechosa

Solo con `DateMatch = true`, el motor compara `VisualDate` con la fecha UTC que
proporciona `TimeProvider`: una fecha futura produce `ERR_FECHA_FUTURA` y una
fecha de un año anterior produce `ERR_FECHA_ANTIGUA`. Ambas requieren revisión;
no se aplica una ventana de días o meses.

---

# 19. Motor de reglas

El motor de reglas debe ser determinista.

Orden inicial de prioridad:

```text
1. DOCUMENT_TYPE_MISMATCH, ante una clasificación visual `NotDocument` y un `TicketData` marcado como ticket o factura
2. ERR_NO_DOCUMENTO, ante `VisualDocumentType = NotDocument`
3. ERR_NO_LEGIBLE, solo sin OCR y sin lectura visual suficiente
4. ERR_DOCUMENTO_MANIPULADO
5. ERR_BEBIDA_ALCOHOLICA
6. ERR_TIPO_GASTO_INCOHERENTE
7. DATE_MISMATCH
8. TOTAL_MISMATCH
9. ERR_SIN_TOTAL, solo sin total visual ni OCR
10. ERR_SIN_FECHA, solo sin fecha visual ni OCR
11. OCR_LOW_CONFIDENCE, con un campo crítico solo OCR o OCR nulo con evidencia visual suficiente
12. ERR_FECHA_FUTURA, solo con DateMatch = true
13. ERR_FECHA_ANTIGUA, solo con DateMatch = true y año anterior
14. ERR_SIN_CIF, para Meals/Diet/Breakfast/Lunch/Dinner/Material sin TaxId
15. OK
```

`REVIEW_REQUIRED` se utilizará para discrepancias donde no exista una causa de rechazo de prioridad superior.

---

# 20. Regla de alcohol

La clasificación de alcohol solo podrá causar rechazo cuando:

1. Existe una línea real de producto.
2. Existe evidencia textual.
3. El concepto corresponde razonablemente a una bebida alcohólica.

No provocar rechazo:

```text
CEREZAS
```

```text
Bar La Cerveza
```

```text
Empleado: Vino
```

Sí puede provocar rechazo:

```text
1 CERVEZA MAHOU 3,50 €
```

---

# 21. Logging

El MVP utilizará logging en fichero.
`FileAuditLogger` escribe una línea por análisis en una ruta configurable; por
defecto usa `logs/ticket-validator.log` relativa al directorio de ejecución.
Las escrituras se serializan dentro del proceso para evitar corrupción del
fichero. Si la escritura falla, se emite un aviso técnico mediante `ILogger` y
no se altera la decisión principal.

Datos mínimos:

```text
Timestamp
AnalysisId
ExpenseType
Status
ReasonCode
DurationMs
Error
```

No se almacenará la imagen del ticket.
Tampoco se almacenan OCR completo, prompts, respuestas de OpenAI ni secretos.

Tampoco se registrarán:

- API keys.
- Credenciales.
- Secretos.

---

# 22. Pruebas unitarias

Proyecto:

```text
TicketValidator.UnitTests
```

Las pruebas unitarias deberán cubrir principalmente:

- Motor de reglas.
- Prioridad de errores.
- Alcohol.
- Coherencia del gasto.
- Fecha.
- Total.
- Comparación OCR / IA.
- Casos de alucinación conocidos.

Las pruebas no deberán depender de:

- Internet.
- OpenAI real.
- Tesseract real cuando se prueben reglas.

Se utilizarán mocks o fakes.

---

# 23. Pruebas de integración

Proyecto:

```text
TicketValidator.IntegrationTests
```

Casos previstos:

- Endpoint REST.
- Validación multipart.
- Tesseract con imágenes controladas.
- Resolución de dependencias.
- Orientación de imágenes.

Las llamadas reales a OpenAI no deberán formar parte de la ejecución habitual de tests.

La suite final consta de 233 tests correctos entre pruebas unitarias y de
integración.

---

# 24. Tickets de prueba

La carpeta:

```text
samples/
```

contendrá únicamente documentos:

- Ficticios.
- Anonimizados.
- Creados expresamente para pruebas.

Los tickets reales utilizados durante la experimentación OCR permanecerán fuera del repositorio Git público.

---

# 25. Swagger

Swagger UI será la interfaz técnica obligatoria del MVP.
Se publica en `/swagger` tanto en desarrollo como en Render para facilitar la
demostración académica de la API sin autenticación.

Permitirá:

1. Seleccionar una imagen.
2. Seleccionar el tipo de gasto.
3. Ejecutar el análisis.
4. Consultar el JSON obtenido.

La aplicación incluye además una web estática auxiliar en `/`, disponible en
español, que consume el mismo endpoint REST y facilita la demostración.

---

# 26. Docker

La API deberá ejecutarse mediante Docker.

El contenedor deberá contener:

- Runtime .NET necesario.
- Tesseract.
- Datos de idioma español necesarios para OCR.
- Aplicación TicketValidator.

La clave de OpenAI se proporcionará mediante variable de entorno en tiempo de ejecución.

---

# 27. Render

El despliegue final utiliza un Render Web Service conectado a la rama `main` y
el contenedor Docker del repositorio.

Configuración esperada:

```text
GitHub
   ↓
Render
   ↓
Docker build
   ↓
TicketValidator.Api
```

Los secretos deberán configurarse en Render mediante variables de entorno.
La clave se configura como `OpenAI__ApiKey`.

- Web pública: https://ticketvalidator-juo1.onrender.com
- Swagger público: https://ticketvalidator-juo1.onrender.com/swagger

---

# 28. Principios SOLID aplicados

## 28.1 S — Single Responsibility Principle

Cada componente debe tener una única responsabilidad principal.

Ejemplo:

```text
TesseractOcrService
→ OCR

OpenAiVisualAnalysisService
→ lectura visual estructurada

OpenAiProductClassifier
→ clasificación de productos

TicketVerificationService
→ comparación

ExpenseRuleEngine
→ reglas

TicketsController
→ HTTP
```

---

## 28.2 O — Open/Closed Principle

Los proveedores externos se encuentran detrás de interfaces.

Ejemplo:

```text
IOcrService
   ↑
TesseractOcrService
```

Otra implementación futura podría sustituir Tesseract sin modificar el caso de uso.

---

## 28.3 L — Liskov Substitution Principle

Cualquier implementación válida de una interfaz deberá poder sustituir a otra sin alterar el comportamiento esperado del sistema.

---

## 28.4 I — Interface Segregation Principle

Las interfaces deberán ser pequeñas y específicas.

Evitar una interfaz genérica que combine:

- OCR.
- IA.
- Reglas.
- Logging.
- Procesamiento de imagen.

---

## 28.5 D — Dependency Inversion Principle

Los casos de uso dependen de abstracciones.

Nunca deben crear directamente:

```csharp
new TesseractOcrService();
new OpenAiVisualAnalysisService();
```

La composición de dependencias se realizará mediante la inyección de dependencias de ASP.NET Core.

---

# 29. Simplicidad del MVP

La arquitectura no pretende implementar Clean Architecture de forma académicamente exhaustiva.

Se prioriza:

```text
claridad
+
testabilidad
+
mantenibilidad
+
funcionamiento
```

Se evitará:

- CQRS innecesario.
- MediatR sin necesidad.
- Microservicios.
- Event sourcing.
- Repositorios sin persistencia.
- Abstracciones sin utilidad práctica.
- Patrones añadidos únicamente para aumentar complejidad.

---

# 30. Mejoras futuras

Quedan fuera de la arquitectura inicial:

- Corrección fina de inclinación/skew.
- Corrección avanzada de perspectiva.
- Mejora de contraste.
- Recorte automático.
- Restauración de imágenes.
- Metadatos EXIF.
- C2PA.
- Persistencia completa.
- Base de datos.
- Autenticación.
- Métricas avanzadas.
- Observabilidad.
- Aplicación móvil de captura guiada.
- Detección de bordes y recorte automático.
- Optimización de coste y consumo de IA.
- Calibración definitiva de confianza OCR.

Estas funcionalidades podrán incorporarse posteriormente sin alterar el núcleo de negocio siempre que se mantenga el desacoplamiento definido.

---

# 31. Regla arquitectónica principal

Ante cualquier modificación se deberá preservar:

```text
Domain
    no conoce infraestructura

Application
    coordina mediante abstracciones

Infrastructure
    implementa servicios externos

Api
    expone el sistema mediante HTTP
```

Y el principio funcional:

```text
IA visual = fuente estructurada principal

OCR = legibilidad, RawText, fecha, total, contraste y diagnóstico

Código = decisión
```
