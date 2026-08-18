# AGENTS.md

## 1. Propósito

Este archivo define las reglas que deben seguir los asistentes de IA utilizados durante el desarrollo de **TicketValidator**.

La IA actúa como copiloto de programación. Debe implementar las tareas solicitadas respetando la arquitectura, requisitos y decisiones técnicas existentes.

No debe rediseñar el proyecto, introducir nuevas tecnologías ni modificar decisiones arquitectónicas sin una petición explícita.

---

## 2. Descripción del proyecto

TicketValidator es un servicio web REST para analizar tickets y facturas de gastos.

El flujo principal es:

```text
Imagen
  ↓
Orientación / rotación
  ↓
Tesseract OCR
  ↓
Extracción mediante IA
  ↓
Análisis visual mediante IA
  ↓
Verificación OCR / IA
  ↓
Motor de reglas
  ↓
Decisión
  ↓
Respuesta REST
```

Principio fundamental:

```text
IA visual = fuente principal de lectura

OCR = fuente independiente de contraste

IA sobre OCR = extracción y estructuración auxiliar

Código = decisión
```

Esta política se ajustó tras validar experimentalmente el MVP con tickets reales:
la lectura visual de GPT-4.1 resultó más fiable para fecha y total en documentos
donde Tesseract no obtenía todos los datos. El código conserva ambas evidencias y
mantiene la decisión final determinista.

---

## 3. Tecnologías

Las tecnologías principales están cerradas.

- Lenguaje: C#
- Framework: .NET / ASP.NET Core
- API: REST
- OCR: Tesseract
- IA: OpenAI GPT-4.1
- Tests: xUnit
- Documentación API: OpenAPI / Swagger UI
- Contenedores: Docker
- Hosting objetivo: Render
- Logging: fichero
- Formatos de entrada: JPEG y PNG

No sustituir estas tecnologías sin una petición explícita.

---

## 4. Estructura de la solución

La solución debe mantener esta estructura:

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

No crear proyectos adicionales salvo que exista una necesidad claramente justificada y haya sido solicitada.

---

## 5. Dependencias permitidas

Las dependencias deben respetar:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
Api
```

De forma concreta:

### Domain

No depende de ningún otro proyecto de la solución.

No debe conocer:

- ASP.NET Core.
- Tesseract.
- OpenAI.
- Render.
- Docker.
- Sistema de archivos.
- HTTP.
- Logging externo.

### Application

Puede depender de:

- Domain.

Contiene:

- Casos de uso.
- Interfaces.
- Servicios de aplicación.
- DTOs internos.
- Orquestación.

No debe contener implementaciones concretas de Tesseract u OpenAI.

### Infrastructure

Puede depender de:

- Application.
- Domain.

Contiene implementaciones concretas de:

- Tesseract.
- OpenAI.
- Procesamiento de imagen.
- Logging.
- Acceso a recursos externos.

### Api

Puede depender de:

- Application.
- Infrastructure.

La API debe limitarse principalmente a:

- Recibir peticiones.
- Validar datos HTTP.
- Invocar casos de uso.
- Convertir resultados a respuestas HTTP.
- Configurar dependencias.
- Exponer OpenAPI / Swagger.

No implementar reglas de negocio dentro de controllers o endpoints.

---

## 6. Principios SOLID

El proyecto debe respetar SOLID.

Especialmente:

### Single Responsibility

Cada clase debe tener una responsabilidad clara.

Evitar clases que simultáneamente:

- ejecuten OCR;
- llamen a OpenAI;
- apliquen reglas;
- escriban logs;
- construyan respuestas HTTP.

### Open/Closed

Los proveedores externos deberán poder sustituirse mediante abstracciones.

Ejemplo:

```text
IOcrService
    ↓
TesseractOcrService
```

### Dependency Inversion

Las capas superiores deberán depender de interfaces y no directamente de implementaciones externas.

No instanciar directamente servicios de infraestructura dentro de casos de uso.

Usar inyección de dependencias.

---

## 7. Interfaces principales previstas

La arquitectura contempla inicialmente las siguientes abstracciones:

```text
IOcrService
IAiTicketExtractor
IVisualAnalysisService
IDocumentOrientationService
ITicketVerificationService
IExpenseRuleEngine
IAuditLogger
```

No crear nuevas abstracciones sin necesidad.

Evitar interfaces creadas únicamente por formalismo si solo añaden complejidad.

---

## 8. Caso de uso principal

El caso de uso principal será el análisis de un ticket.

Responsabilidad conceptual:

```text
AnalyzeTicket
```

Flujo esperado:

1. Validar entrada.
2. Comprobar formato.
3. Corregir orientación si procede.
4. Ejecutar OCR.
5. Conservar evidencia OCR.
6. Ejecutar extracción estructurada mediante IA.
7. Ejecutar análisis visual de manipulación.
8. Comparar OCR e IA en campos críticos.
9. Aplicar reglas de negocio.
10. Generar decisión.
11. Registrar información técnica.
12. Devolver resultado.

Evitar introducir lógica HTTP dentro de este caso de uso.

---

## 9. Reglas sobre OCR

Tesseract constituye una fuente independiente de evidencia textual y contraste.

El resultado OCR debe conservar, cuando sea posible:

- Texto completo.
- Palabras.
- Confianza.
- Posición del texto si Tesseract la proporciona de forma útil.

No establecer inicialmente un umbral fijo de confianza OCR.

Los umbrales se determinarán posteriormente mediante experimentación con tickets reales.

La configuración de confianza deberá mantenerse fuera de las reglas de negocio.

`OcrReadable` solo significa que existe evidencia textual OCR. Distinguir:

```text
OCR parcial: OcrReadable = true aunque falten fecha o total
→ la lectura visual se conserva, pero sin corroboración requiere REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

OCR nulo: OcrReadable = false y no hay texto ni palabras
→ con ticket/factura y evidencia visual suficiente, REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
→ sin evidencia visual suficiente, UNREADABLE / ERR_NO_LEGIBLE
```

---

## 10. Reglas sobre Inteligencia Artificial

GPT-4.1 visual es la fuente principal de lectura de los datos semánticos,
productos, fecha y total directamente de la imagen. OCR se utiliza para
contrastar fecha y total, determinar legibilidad y facilitar diagnóstico.

No debe utilizarse como sustituto del OCR cuando se necesite evidencia textual.

La IA podrá utilizarse para:

- Estructurar texto OCR.
- Identificar campos.
- Normalizar conceptos.
- Clasificar productos.
- Interpretar contexto.
- Analizar visualmente posibles indicios de manipulación.

La IA no deberá decidir directamente el estado final del ticket.

No crear un único prompt que realice todas las responsabilidades del sistema.

Los prompts deberán estar separados por función.

Inicialmente:

```text
TicketExtractionPrompt

ProductClassificationPrompt

VisualAnalysisPrompt
```

---

## 11. Prevención de alucinaciones

Nunca reconstruir ni completar información que ninguna fuente haya leído. Para
fecha y total, la lectura visual es el valor principal y OCR aporta contraste:

```text
Visual + OCR coinciden
→ dato corroborado

Visual existe + OCR no existe
→ conservar valor visual; Match = null; REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

Visual existe + OCR nulo
→ conservar valor visual; REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

Visual y OCR discrepan
→ REVIEW_REQUIRED y código de discrepancia

Ambos no existen
→ campo ausente

Solo OCR existe
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

Para `APPROVED`, `DateMatch` y `TotalMatch` deben ser ambos `true`.
La lectura visual sigue siendo el valor principal, pero OCR debe corroborar ambos
campos críticos para aprobar automáticamente.

Para `Meals`, `Diet`, `Breakfast`, `Lunch`, `Dinner` y `Material`, `TaxId` es
obligatorio para aprobar. Su ausencia produce `REVIEW_REQUIRED / ERR_SIN_CIF`
cuando no existe una regla de mayor prioridad.

Sobre una fecha corroborada (`DateMatch = true`), una fecha posterior al día
actual produce `REVIEW_REQUIRED / ERR_FECHA_FUTURA`; una fecha de un año anterior
produce `REVIEW_REQUIRED / ERR_FECHA_ANTIGUA`. Un documento del año actual no se
considera antiguo por esta regla.

Campos críticos iniciales:

- Fecha.
- Total.
- Tipo de documento.
- Productos que puedan provocar rechazo.

---

## 12. Productos

Cada producto deberá conservar, cuando sea posible:

```text
concept
normalizedText
amount
category
isAlcohol
```

`concept` representa el texto visible de una línea real de compra.

`normalizedText` es una interpretación.

Nunca alterar el significado esencial del concepto visible.

Ejemplo incorrecto:

```text
Concepto:
CEREZAS

Interpretación:
LICOR DE CEREZAS
```

Esto no está permitido.

Una asociación semántica no constituye evidencia.

---

## 13. Bebidas alcohólicas

Un producto solo podrá provocar rechazo por alcohol cuando:

1. Corresponda a una línea real de compra.
2. Exista evidencia textual suficiente.
3. La clasificación como bebida alcohólica sea razonablemente segura.

No considerar productos alcohólicos textos pertenecientes a:

- Nombre del establecimiento.
- Empleado.
- Camarero.
- Cliente.
- Mesa.
- Dirección.
- Información administrativa.

Casos importantes:

```text
CEREZAS
→ NO alcohol

Bar La Cerveza
→ NO alcohol por el nombre del establecimiento

Empleado: Vino
→ NO alcohol

CERVEZA MAHOU
como línea de compra
→ alcohol
```

---

## 14. Manipulación

En el MVP solo se realizará análisis visual de indicios de manipulación.

Posibles indicios:

- Tachaduras.
- Sobrescrituras.
- Correcciones manuales.
- Corrector.
- Modificaciones visibles de información impresa.

No se pretende realizar certificación forense.

La ausencia de indicios no garantiza que una imagen sea auténtica.

Metadatos, EXIF o C2PA quedan fuera del MVP.

---

## 15. Preprocesamiento de imagen

El MVP solo contempla:

- Detección de orientación.
- Rotación.

La orientación gruesa 0/90/180/270 se intentará primero mediante Tesseract OSD.
Si el OCR posterior es insuficiente (sin texto útil o menos de tres palabras),
se probarán 0/90/180/270 y se conservará de forma determinista la imagen con
mejor evidencia OCR. El ticket inclinado aproximadamente 10 grados permanece
como limitación conocida.

No implementar inicialmente:

- Corrección fina de inclinación/skew.
- OpenCV/OpenCvSharp para el preprocesamiento de imagen.
- Corrección de perspectiva.
- Filtros avanzados.
- Restauración.
- Mejora mediante IA.
- Procesamiento fotográfico complejo.

Estas funcionalidades quedan como posibles mejoras futuras.

---

## 16. Estados

Estados generales:

```text
APPROVED
REJECTED
REVIEW_REQUIRED
UNREADABLE
PROCESSING_ERROR
```

Códigos iniciales:

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
ERR_SIN_CIF
ERR_FECHA_ANTIGUA
ERR_FECHA_FUTURA
```

No crear nuevos estados o códigos sin una necesidad concreta.

Si fuera necesario añadir alguno, actualizar también la documentación y los tests.

---

## 17. REVIEW_REQUIRED

Usar `REVIEW_REQUIRED` cuando exista evidencia pero haya una discrepancia o una
lectura crítica exclusiva de OCR o una lectura visual sin evidencia OCR que
impida tomar una decisión fiable. En ambos casos se usa el código existente
`OCR_LOW_CONFIDENCE`; no representa un umbral numérico.

Ejemplos:

```text
OCR fecha:
14/08/2026

IA fecha:
17/08/2026

→ REVIEW_REQUIRED
→ DATE_MISMATCH
```

Una discrepancia no significa automáticamente que el ticket sea inválido.

---

## 18. Prioridad de errores

La prioridad inicial de las reglas es:

```text
1. ERR_NO_DOCUMENTO
2. ERR_NO_LEGIBLE, solo sin OCR y sin lectura visual suficiente
3. ERR_DOCUMENTO_MANIPULADO
4. ERR_BEBIDA_ALCOHOLICA
5. ERR_TIPO_GASTO_INCOHERENTE
6. DATE_MISMATCH
7. TOTAL_MISMATCH
8. ERR_SIN_TOTAL, solo sin total visual ni OCR
9. ERR_SIN_FECHA, solo sin fecha visual ni OCR
10. OCR_LOW_CONFIDENCE, con un campo crítico exclusivo de OCR o OCR nulo con evidencia visual suficiente
11. ERR_FECHA_FUTURA, solo con DateMatch = true
12. ERR_FECHA_ANTIGUA, solo con DateMatch = true y año anterior
13. ERR_SIN_CIF, para Meals/Diet/Breakfast/Lunch/Dinner/Material sin TaxId
14. OK
```

`REVIEW_REQUIRED` se utiliza para discrepancias cuando no existe una regla de mayor prioridad que determine un rechazo.

La prioridad debe implementarse mediante código.

No delegarla a GPT.

---

## 19. Modelo interno

Mantener el modelo interno simple.

Objetos previstos:

```text
TicketData
ProductData
AddressData
VatData
VerificationData
AnalysisDecision
```

Evitar modelos excesivamente complejos.

No añadir propiedades que no sean utilizadas por el caso de uso actual.

---

## 20. API REST

Endpoint principal previsto:

```text
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

Formatos permitidos:

```text
image/jpeg
image/png
```

Respuesta conceptual:

```json
{
  "status": "APPROVED",
  "reasonCode": "OK",
  "message": null,
  "ticket": {},
  "verification": {}
}
```

Mantener controllers y endpoints pequeños.

---

## 21. Swagger

La API deberá ofrecer documentación OpenAPI y Swagger UI.

Swagger deberá permitir:

- Seleccionar una imagen.
- Indicar el tipo de gasto.
- Ejecutar el endpoint.
- Consultar la respuesta JSON.

Swagger será suficiente como interfaz técnica del MVP.

Una web de demostración independiente es opcional.

---

## 22. Logging

Utilizar logs simples en fichero.

Registrar como mínimo:

```text
Timestamp
AnalysisId
ExpenseType
Status
ReasonCode
DurationMs
Error
```

No registrar:

- OPENAI_API_KEY.
- Secretos.
- Credenciales.
- Imagen del ticket.

La imagen no se persiste como requisito del MVP.

---

## 23. Seguridad

Nunca incluir secretos en:

- Código fuente.
- appsettings.json versionado.
- Tests.
- README.
- Logs.
- Commits.

La clave de OpenAI deberá obtenerse mediante variable de entorno.

Ejemplo:

```text
OPENAI_API_KEY
```

---

## 24. Testing

Framework:

```text
xUnit
```

Priorizar tests unitarios.

Las reglas de negocio deberán poder probarse sin:

- Tesseract real.
- OpenAI real.
- Internet.

Utilizar mocks/fakes para dependencias externas.

Casos prioritarios:

```text
CEREZAS no es alcohol

CERVEZA MAHOU es alcohol

Bar La Cerveza no provoca rechazo

Empleado Vino no provoca rechazo

fecha OCR ≠ fecha visual
→ REVIEW_REQUIRED

total OCR ≠ total visual
→ REVIEW_REQUIRED

fecha o total visual con OCR ausente
→ conservar el valor visual sin marcarlo como corroborado

ticket girado
→ intentar rotar antes del OCR

OCR parcial sin fecha o total, con ticket visual y campos visuales
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE

OCR vacío con ticket visual y fecha y total visuales
→ REVIEW_REQUIRED / OCR_LOW_CONFIDENCE
```

---

## 25. Tests de integración

Mantener pocos tests de integración.

Priorizar:

- Endpoint HTTP.
- Validación de petición.
- Tesseract con imágenes de prueba.
- Integración de dependencias.

No ejecutar llamadas reales a OpenAI en todos los tests.

Las pruebas reales contra OpenAI deberán ser manuales o estar claramente separadas de la suite habitual.

---

## 26. Datos de prueba

No subir tickets reales que contengan información personal o empresarial sensible al repositorio público.

La carpeta:

```text
samples/
```

deberá contener únicamente:

- Documentos ficticios.
- Documentos anonimizados.
- Imágenes creadas específicamente para tests.

Los tickets reales podrán utilizarse localmente y deberán permanecer fuera de Git.

---

## 27. Docker

La API deberá poder ejecutarse mediante Docker.

Tener especial cuidado con las dependencias nativas necesarias para Tesseract y el idioma español.

La imagen Docker deberá contener únicamente lo necesario para ejecutar el servicio.

---

## 28. Git

Realizar cambios pequeños y coherentes.

Antes de finalizar una tarea:

1. Compilar.
2. Ejecutar los tests relevantes.
3. Revisar los archivos modificados.
4. Comprobar que no se han añadido secretos.
5. Actualizar documentación si cambia un comportamiento documentado.

No realizar commits automáticamente salvo petición explícita del usuario.

No realizar push automáticamente salvo petición explícita del usuario.

---

## 29. Forma de trabajar con el asistente

Cuando se solicite una tarea:

- Implementar únicamente el alcance solicitado.
- No ampliar funcionalidades por iniciativa propia.
- No refactorizar áreas no relacionadas.
- No cambiar nombres públicos sin necesidad.
- No introducir patrones innecesarios.
- No añadir paquetes NuGet sin justificar su necesidad.
- No generar grandes cantidades de código no solicitado.
- Mantener la solución compilable.
- Mantener los tests existentes funcionando.

Si una decisión arquitectónica no está definida:

**preguntar o dejarla pendiente, no inventarla.**

---

## 30. Simplicidad del MVP

Este proyecto es un TFM con un alcance deliberadamente limitado.

Evitar:

- Overengineering.
- Microservicios.
- CQRS innecesario.
- Event sourcing.
- Mediadores si no aportan valor.
- Repositorios sin persistencia real.
- Patrones añadidos únicamente para demostrar patrones.
- Abstracciones que no resuelvan un problema concreto.

La prioridad es:

```text
claridad
+
testabilidad
+
separación de responsabilidades
+
funcionamiento
```

---

## 31. Documentación

Los documentos principales son:

```text
README.md
AGENTS.md
docs/REQUIREMENTS.md
docs/ARCHITECTURE.md
```

Si una implementación modifica:

- arquitectura;
- contrato REST;
- estados;
- códigos;
- reglas;
- tecnologías;

deberá indicarse que la documentación relacionada necesita actualizarse.

---

## 32. Regla final

Ante cualquier duda, respetar esta prioridad:

```text
1. Requisitos documentados
2. Arquitectura acordada
3. Simplicidad del MVP
4. Testabilidad
5. Implementación
```

No modificar los requisitos para acomodarlos al código.

El código debe implementar los requisitos.
