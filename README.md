# TicketValidator

TicketValidator es un servicio web REST desarrollado como Trabajo de Fin de
Máster para analizar imágenes de tickets y facturas de gasto. Su objetivo es
extraer datos del documento, contrastar fecha y total mediante dos fuentes
independientes y aplicar reglas deterministas para devolver una decisión
explicable: `APPROVED`, `REJECTED`, `REVIEW_REQUIRED` o `UNREADABLE`.

## Demo pública

El servicio final está desplegado como Render Web Service mediante Docker y la
rama `main`.

- Web: https://ticketvalidator-juo1.onrender.com
- Swagger: https://ticketvalidator-juo1.onrender.com/swagger

La instancia gratuita de Render puede necesitar unos segundos para arrancar
después de un periodo de inactividad.

## Tecnologías

- C# y .NET 9 / ASP.NET Core.
- API REST con OpenAPI y Swagger UI.
- Tesseract para OCR y orientación OSD.
- OpenAI GPT-4.1 para lectura visual y análisis semántico.
- xUnit para pruebas automatizadas.
- Docker como unidad de ejecución y despliegue.
- Render como hosting público.
- HTML, CSS y JavaScript sin frameworks para la web de demostración auxiliar.

## Arquitectura

La solución conserva cuatro capas:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
Api
```

`Domain` contiene modelos y decisiones puras; `Application` coordina el caso de
uso mediante abstracciones; `Infrastructure` implementa Tesseract, OpenAI,
rotación y logging; `Api` valida HTTP, configura dependencias y expone el
resultado. La decisión final siempre corresponde al código.

## Flujo de procesamiento

```text
Imagen
  ↓
Tesseract OSD
  ↓
OCR inicial
  ↓
Si falta texto, palabras, fecha o total: fallback 0°/90°/180°/270°
  ↓
Selección conjunta de mejor imagen y OcrResult
  ↓
GPT-4.1 visual sobre la misma imagen seleccionada
  ↓
Clasificación de productos
  ↓
Análisis de coherencia con el tipo de gasto
  ↓
Corroboración OCR/visual de fecha y total
  ↓
Motor de reglas y decisión
```

La IA visual es la fuente estructurada principal para tipo de documento,
proveedor, CIF/NIF, dirección, número, fecha, hora, total, IVA, productos e
indicios de manipulación. OCR se limita a legibilidad, texto bruto, detección
independiente de fecha y total, corroboración y diagnóstico.

## Reglas principales

- `APPROVED / OK` exige un documento válido y fecha y total visuales
  corroborados por OCR.
- Una discrepancia produce `DATE_MISMATCH` o `TOTAL_MISMATCH` y revisión manual.
- Un campo crítico sin corroboración produce `OCR_LOW_CONFIDENCE`.
- La manipulación visible, el alcohol confirmado y una compra claramente
  incoherente pueden producir rechazo.
- `Meals`, `Diet`, `Breakfast`, `Lunch`, `Dinner` y `Material` requieren CIF/NIF.
- La falta de CIF/NIF no bloquea por sí sola `Parking`, `Highway`, `Taxi`,
  `Fuel`, `Accommodation`, `Other` ni `Unknown`.
- Una fecha corroborada futura o de un año anterior requiere revisión.
- La IA nunca decide el estado ni la prioridad de los códigos.

## Ejecución local

Requisitos: SDK de .NET 9 y una clave de OpenAI configurada fuera del
repositorio.

```powershell
dotnet restore
dotnet run --project src/TicketValidator.Api --launch-profile http
```

La web queda disponible en `http://localhost:5008/` y Swagger en
`http://localhost:5008/swagger`.

## Configuración local de OpenAI

El modelo inicial se configura en `src/TicketValidator.Api/appsettings.json` mediante la sección `OpenAI` y no contiene secretos.

Para configurar la clave únicamente en el equipo local, ejecutar desde `src/TicketValidator.Api`:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "<TU_API_KEY>"
```

Los User Secrets se almacenan fuera del repositorio. No se debe añadir la clave a `appsettings.json`, al código fuente, a logs ni a archivos incluidos en Docker.

La configuración acepta `OpenAI__ApiKey` mediante el sistema estándar de
ASP.NET Core y también `OPENAI_API_KEY`. En Render se utiliza
`OpenAI__ApiKey`. Nunca debe almacenarse una clave real en el repositorio.

## Estados y códigos

Los estados públicos son `APPROVED`, `REJECTED`, `REVIEW_REQUIRED`,
`UNREADABLE` y `PROCESSING_ERROR`. El motivo concreto se expresa mediante:

```text
OK
ERR_NO_DOCUMENTO
ERR_NO_LEGIBLE
ERR_DOCUMENTO_MANIPULADO
ERR_BEBIDA_ALCOHOLICA
ERR_TIPO_GASTO_INCOHERENTE
ERR_SIN_TOTAL
ERR_SIN_FECHA
ERR_SIN_CIF
ERR_FECHA_ANTIGUA
ERR_FECHA_FUTURA
DOCUMENT_TYPE_MISMATCH
DATE_MISMATCH
TOTAL_MISMATCH
OCR_LOW_CONFIDENCE
```

## Swagger / OpenAPI

Con la API ejecutada mediante el perfil `http`, abrir
`http://localhost:5008/swagger`.

1. Abrir `POST /api/v1/tickets/analyze`.
2. Seleccionar **Try it out**.
3. Elegir una imagen `.jpg`, `.jpeg` o `.png` en `file`.
4. Indicar el `expenseType`.
5. Ejecutar la petición con **Execute**.

Swagger UI permanece disponible también en Render como interfaz técnica de
demostración del MVP académico.

## Auditoría en fichero

Cada análisis genera una línea técnica en `logs/ticket-validator.log` por
defecto, relativa al directorio de ejecución de la aplicación. La sección
`AuditLog` de `appsettings.json` permite configurar el directorio y el nombre
del fichero.

El log contiene el identificador de análisis, tipo de gasto, estado, código de
motivo, duración y datos básicos de error. No almacena imágenes, OCR completo,
prompts, respuestas de OpenAI ni secretos. El directorio `logs/` está excluido
de Git.

## Tests

Ejecutar toda la suite con:

```powershell
dotnet test --configuration Release
```

Los tests unitarios no llaman a OpenAI. Los tests de integración habituales
tampoco consumen la API de OpenAI ni requieren `OPENAI_API_KEY`; las pruebas
del pipeline y de la API sustituyen las dependencias externas por fakes
controlados. Las pruebas reales contra OpenAI se ejecutan manualmente y quedan
fuera de la suite normal.

La suite final contiene **233 tests correctos**.

## Política de lectura

Tras pruebas experimentales del MVP con tickets reales, la IA visual es la
fuente principal para leer directamente de la imagen los datos del emisor,
líneas facturadas, fecha y total. OCR se conserva exclusivamente como evidencia
textual independiente para contrastar fecha y total, determinar legibilidad y
facilitar diagnóstico. El código toma siempre la decisión final.

Los valores visuales se conservan siempre que existan, pero `APPROVED` requiere
que fecha y total coincidan entre OCR e IA visual (`DateMatch` y `TotalMatch`
iguales a `true`). Un campo sin corroboración, tanto con OCR parcial como nulo,
devuelve `REVIEW_REQUIRED / OCR_LOW_CONFIDENCE`. Sin OCR ni evidencia visual
suficiente se devuelve `UNREADABLE / ERR_NO_LEGIBLE`.

Además, `Meals`, `Diet`, `Breakfast`, `Lunch`, `Dinner` y `Material` requieren
un CIF/NIF (`TaxId`) para aprobar. Si falta, el resultado es
`REVIEW_REQUIRED / ERR_SIN_CIF`.

Como regla preventiva adicional, una fecha corroborada futura produce
`REVIEW_REQUIRED / ERR_FECHA_FUTURA`; una fecha corroborada de un año anterior,
`REVIEW_REQUIRED / ERR_FECHA_ANTIGUA`. Las fechas del año actual no se consideran
antiguas por esta regla y ningún caso se rechaza automáticamente.

## Orientación OCR

Tesseract OSD es el primer intento para corregir 0/90/180/270. Si el OCR inicial
no tiene texto útil, reconoce menos de tres palabras, no obtiene fecha o no
obtiene total, se prueban las cuatro rotaciones ortogonales y se conserva la de
mejor evidencia OCR. Es un fallback para OCR pobre o sin evidencia crítica: no
usa OpenCV ni aplica corrección fina de inclinación o perspectiva.

## Docker

Build:

```powershell
docker build -t ticketvalidator .
```

Run:

```powershell
docker run --rm -d --name ticketvalidator-api -p 8081:8080 -e OpenAI__ApiKey="<TU_API_KEY>" ticketvalidator
```

El contenedor expone internamente el puerto 8080; con el comando anterior la web
queda en `http://localhost:8081/` y Swagger en
`http://localhost:8081/swagger`. Para conservar los
logs opcionalmente, añadir `-v ticketvalidator-logs:/app/logs` al comando.

## Web demo

La interfaz web incluida en `wwwroot` es una herramienta auxiliar para pruebas
manuales y demostración. No forma parte del núcleo funcional de TicketValidator
ni contiene lógica de negocio.

Está disponible en español y permite:

- Cargar imágenes JPG, JPEG o PNG.
- Seleccionar el tipo de gasto.
- Consultar la decisión y los datos extraídos.
- Comparar fecha y total visuales con OCR.
- Consultar productos, texto OCR bruto y respuesta JSON.

Como ayuda de diagnóstico, la respuesta de análisis expone `ocrRawText` dentro
de `verification` y la demo permite visualizarlo. Este dato no interviene en
las reglas, no se persiste y no se registra en logs.

- Web demo: `http://localhost:<puerto>/`
- Swagger: `http://localhost:<puerto>/swagger`

## Despliegue en Render

El despliegue definitivo utiliza un Render Web Service conectado a la rama
`main`. Render construye el `Dockerfile`, ejecuta la aplicación en el puerto
interno 8080 y recibe `OpenAI__ApiKey` como variable de entorno. No se incluyen
secretos en la imagen ni en el código.

- Servicio público: https://ticketvalidator-juo1.onrender.com
- Swagger público: https://ticketvalidator-juo1.onrender.com/swagger

## Limitaciones conocidas

- El OCR es sensible a iluminación, contraste, escala y calidad de impresión.
- Los tickets térmicos deteriorados y la variedad de formatos pueden reducir la
  evidencia disponible.
- El fallback corrige orientación ortogonal, pero no inclinación fina,
  perspectiva ni recorte automático.
- OCR puede no corroborar datos que la visión identifica correctamente.
- Entradas legítimas con evidencia insuficiente o discrepante pueden requerir
  revisión manual.
- El análisis de manipulación visual no constituye certificación forense.

## Mejoras futuras

- Aplicación móvil para captura guiada.
- Detección de bordes y recorte automático del ticket.
- Corrección de perspectiva e inclinación fina.
- Mejora de brillo, contraste y preprocesamiento OCR.
- Ampliación de reglas y tipos documentales.
- Optimización del consumo y coste de IA.
- Auditoría y persistencia reforzadas si el MVP evoluciona a producto real.
