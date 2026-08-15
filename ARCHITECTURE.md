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
OCR = evidencia textual

IA = interpretación

Código = decisión
```

La Inteligencia Artificial no constituye por sí sola una fuente de verdad para los campos críticos del documento.

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

## 5.2 ProductData

Representa una línea de producto o servicio.

Campos previstos:

```text
ocrText
normalizedText
amount
category
isAlcohol
```

El campo:

```text
ocrText
```

conserva la evidencia original obtenida del documento.

El campo:

```text
normalizedText
```

representa una interpretación del contenido.

La normalización nunca debe introducir un concepto diferente del original.

Ejemplo no permitido:

```text
OCR:
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

dateMatch
ocrDate
aiDate

totalMatch
ocrTotal
aiTotal

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
│   ├── IAiTicketExtractor.cs
│   ├── IVisualAnalysisService.cs
│   ├── IDocumentOrientationService.cs
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
│   ├── AiTicketExtraction.cs
│   ├── VisualAnalysisResult.cs
│   └── VerificationResult.cs
│
└── Services/
    ├── TicketVerificationService.cs
    └── ExpenseRuleEngine.cs
```

---

## 6.3 Interfaces principales

Inicialmente se contemplan:

```text
IOcrService
IAiTicketExtractor
IVisualAnalysisService
IDocumentOrientationService
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

---

## 6.5 IAiTicketExtractor

Responsable de transformar el texto OCR en una representación estructurada.

Entrada principal:

```text
texto OCR
```

Salida:

```text
AiTicketExtraction
```

La IA deberá devolver `null` cuando un dato no pueda identificarse con suficiente seguridad.

No debe completar datos ausentes.

---

## 6.6 IVisualAnalysisService

Responsable del análisis visual limitado del MVP.

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

No forma parte del MVP:

- Corrección de perspectiva.
- Contraste avanzado.
- Restauración.
- Eliminación de ruido.
- Mejora mediante IA.

---

## 6.8 ITicketVerificationService

Responsable de comparar la evidencia OCR y la interpretación IA.

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

IA fecha:
14/08/2026

→ Match
```

Ejemplo:

```text
OCR fecha:
14/08/2026

IA fecha:
17/08/2026

→ DATE_MISMATCH
→ REVIEW_REQUIRED
```

---

## 6.9 IExpenseRuleEngine

Responsable de ejecutar las reglas y determinar el resultado final.

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
AnalyzeTicket
     │
     ▼
Validar entrada
     │
     ▼
Orientar imagen
     │
     ▼
Ejecutar OCR
     │
     ▼
Conservar evidencia OCR
     │
     ├───────────────────────────┐
     │                           │
     ▼                           ▼
Extracción IA             Análisis visual IA
     │                           │
     └────────────┬──────────────┘
                  ▼
             Verificación
                  │
                  ▼
            Motor de reglas
                  │
                  ▼
              Decisión
                  │
                  ▼
              Auditoría
                  │
                  ▼
             Resultado
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

## 8.2 Orientación

El fichero pasa por:

```text
IDocumentOrientationService
```

y se obtiene una imagen correctamente orientada para OCR.

---

## 8.3 OCR

La imagen orientada se procesa mediante:

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

## 8.4 Extracción mediante IA

El texto OCR se envía a:

```text
IAiTicketExtractor
```

implementado por:

```text
OpenAiTicketExtractor
```

La entrada de esta operación será principalmente texto.

Esta llamada no debe realizar análisis visual del documento.

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

Esta operación tendrá un prompt específico dedicado a detectar indicios visibles de manipulación.

No debe extraer productos ni aplicar reglas de gasto.

---

# 9. Separación de prompts

No se utilizará un único prompt para resolver todas las responsabilidades.

La infraestructura dispondrá de prompts separados.

```text
Infrastructure/
└── AI/
    └── Prompts/
        ├── TicketExtractionPrompt.cs
        ├── ProductClassificationPrompt.cs
        └── VisualManipulationPrompt.cs
```

---

## 9.1 TicketExtractionPrompt

Responsable de:

- Extraer empresa.
- CIF.
- Número de ticket o factura.
- Fecha.
- Hora.
- Total.
- Dirección.
- IVA.
- Líneas de productos.

Trabaja principalmente sobre texto OCR.

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

## 9.3 VisualManipulationPrompt

Responsable exclusivamente del análisis visual de indicios de manipulación.

No debe decidir la aceptación del ticket.

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
│   ├── OpenAiTicketExtractor.cs
│   ├── OpenAiVisualAnalysisService.cs
│   └── Prompts/
│       ├── TicketExtractionPrompt.cs
│       ├── ProductClassificationPrompt.cs
│       └── VisualManipulationPrompt.cs
│
├── ImageProcessing/
│   └── DocumentOrientationService.cs
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

Tesseract se utilizará como fuente principal de evidencia textual.

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
│   ├── OpenAiOptions.cs
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

DATE_MISMATCH
TOTAL_MISMATCH
OCR_LOW_CONFIDENCE
```

---

# 18. Política de discrepancias

## 18.1 Coincidencia

```text
OCR claro
+
IA coincide

→ campo verificado
```

---

## 18.2 Discrepancia

```text
OCR claro
+
IA diferente

→ REVIEW_REQUIRED
```

Ejemplo:

```text
OCR:
14/08/2026

IA:
17/08/2026

→ DATE_MISMATCH
```

---

## 18.3 OCR insuficiente

```text
OCR no puede determinar el dato
+
IA propone un valor

→ no utilizar automáticamente el valor de IA
```

---

# 19. Motor de reglas

El motor de reglas debe ser determinista.

Orden inicial de prioridad:

```text
1. ERR_NO_DOCUMENTO
2. ERR_NO_LEGIBLE
3. ERR_DOCUMENTO_MANIPULADO
4. ERR_BEBIDA_ALCOHOLICA
5. ERR_TIPO_GASTO_INCOHERENTE
6. ERR_SIN_TOTAL
7. ERR_SIN_FECHA
8. OK
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

Permitirá:

1. Seleccionar una imagen.
2. Seleccionar el tipo de gasto.
3. Ejecutar el análisis.
4. Consultar el JSON obtenido.

Una aplicación web adicional será opcional.

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

Render será el destino inicial de despliegue.

El despliegue utilizará el contenedor Docker.

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

---

# 28. Principios SOLID aplicados

## 28.1 S — Single Responsibility Principle

Cada componente debe tener una única responsabilidad principal.

Ejemplo:

```text
TesseractOcrService
→ OCR

OpenAiTicketExtractor
→ extracción IA

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
new OpenAiTicketExtractor();
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
- Frontend completo.
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
OCR = evidencia

IA = interpretación

Código = decisión
```