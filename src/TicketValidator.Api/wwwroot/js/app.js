const fileInput = document.querySelector('#file-input');
const dropZone = document.querySelector('#drop-zone');
const previewPanel = document.querySelector('#preview-panel');
const previewImage = document.querySelector('#preview-image');
const fileName = document.querySelector('#file-name');
const expenseType = document.querySelector('#expense-type');
const analyzeButton = document.querySelector('#analyze-button');
const requestState = document.querySelector('#request-state');
const resultPanel = document.querySelector('#result-panel');
const statusBadge = document.querySelector('#status-badge');
const reasonCode = document.querySelector('#reason-code');
const resultMessage = document.querySelector('#result-message');
const ticketDetails = document.querySelector('#ticket-details');
const addressDetails = document.querySelector('#address-details');
const verificationDocument = document.querySelector('#verification-document');
const verificationDate = document.querySelector('#verification-date');
const verificationTotal = document.querySelector('#verification-total');
const verificationIntegrity = document.querySelector('#verification-integrity');
const ocrRawText = document.querySelector('#ocr-raw-text');
const productsBody = document.querySelector('#products-body');
const vatSection = document.querySelector('#vat-section');
const vatBody = document.querySelector('#vat-body');
const jsonResponse = document.querySelector('#json-response');

let selectedFile;
let previewUrl;

dropZone.addEventListener('click', () => fileInput.click());
dropZone.addEventListener('keydown', (event) => {
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    fileInput.click();
  }
});

fileInput.addEventListener('change', () => setSelectedFile(fileInput.files[0]));

['dragenter', 'dragover'].forEach((eventName) => {
  dropZone.addEventListener(eventName, (event) => {
    event.preventDefault();
    dropZone.classList.add('is-dragging');
  });
});

['dragleave', 'drop'].forEach((eventName) => {
  dropZone.addEventListener(eventName, (event) => {
    event.preventDefault();
    dropZone.classList.remove('is-dragging');
  });
});

dropZone.addEventListener('drop', (event) => setSelectedFile(event.dataTransfer.files[0]));
analyzeButton.addEventListener('click', analyzeTicket);
window.addEventListener('beforeunload', revokePreviewUrl);

function setSelectedFile(file) {
  if (!file) return;

  selectedFile = file;
  revokePreviewUrl();
  previewUrl = URL.createObjectURL(file);
  previewImage.src = previewUrl;
  fileName.textContent = file.name;
  previewPanel.hidden = false;
  requestState.textContent = '';
  resultPanel.hidden = true;
}

function revokePreviewUrl() {
  if (previewUrl) {
    URL.revokeObjectURL(previewUrl);
    previewUrl = undefined;
  }
}

async function analyzeTicket() {
  if (!selectedFile) {
    requestState.textContent = 'Selecciona una imagen antes de analizarla.';
    return;
  }

  analyzeButton.disabled = true;
  analyzeButton.textContent = 'Analizando...';
  requestState.textContent = 'Analizando...';
  resultPanel.hidden = true;

  try {
    const formData = new FormData();
    formData.append('file', selectedFile);
    formData.append('expenseType', expenseType.value);

    const response = await fetch('/api/v1/tickets/analyze', { method: 'POST', body: formData });
    const body = await readResponse(response);

    if (!response.ok) {
      showHttpError(response.status, body);
      return;
    }

    renderResult(body);
    requestState.textContent = '';
  } catch {
    requestState.textContent = 'No se ha podido conectar con la API.';
  } finally {
    analyzeButton.disabled = false;
    analyzeButton.textContent = 'Analizar ticket';
  }
}

async function readResponse(response) {
  const text = await response.text();
  if (!text) return null;

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function showHttpError(status, body) {
  resultPanel.hidden = true;
  if (status === 500) {
    requestState.textContent = 'Se ha producido un error técnico durante el análisis.';
    return;
  }

  requestState.textContent = typeof body === 'string'
    ? body
    : body?.detail || body?.title || 'La solicitud no es válida.';
}

function renderResult(result) {
  statusBadge.textContent = statusValue(result.status);
  statusBadge.className = `status-badge status-${String(result.status || '').toLowerCase().replaceAll('_', '-')}`;
  reasonCode.textContent = value(result.reasonCode);
  resultMessage.textContent = result.message || '';

  renderDetails(ticketDetails, [
    ['Tipo de documento', documentTypeValue(result.ticket?.documentType)],
    ['Establecimiento', result.ticket?.establishmentName],
    ['Tipo de establecimiento', establishmentTypeValue(result.ticket?.establishmentType)],
    ['CIF/NIF', result.ticket?.taxId],
    ['Número de factura', result.ticket?.invoiceNumber],
    ['Fecha', result.ticket?.date],
    ['Hora', result.ticket?.time],
    ['Total', result.ticket?.total]
  ]);

  const address = result.ticket?.address;
  renderDetails(addressDetails, address ? [
    ['Dirección', address.street],
    ['Ciudad', address.city],
    ['Código postal', address.postalCode],
    ['País', address.country]
  ] : []);

  renderDetails(verificationDocument, [
    ['OCR legible', booleanValue(result.verification?.ocrReadable)],
    ['Tipo visual del documento', documentTypeValue(result.verification?.visualDocumentType)]
  ], true);
  renderDetails(verificationDate, [
    ['Fecha visual', result.verification?.visualDate],
    ['Fecha OCR', result.verification?.ocrDate],
    ['Coincidencia de fecha', booleanValue(result.verification?.dateMatch)]
  ], true);
  renderDetails(verificationTotal, [
    ['Total visual', result.verification?.visualTotal],
    ['Total OCR', result.verification?.ocrTotal],
    ['Coincidencia de total', booleanValue(result.verification?.totalMatch)]
  ], true);
  renderDetails(verificationIntegrity, [
    ['Manipulación detectada', booleanValue(result.verification?.manipulationDetected)]
  ], true);
  const rawText = result.verification?.ocrRawText;
  ocrRawText.textContent = rawText === undefined || rawText === null || rawText === ''
    ? 'No se ha obtenido texto OCR.'
    : rawText;

  renderProducts(result.ticket?.products || []);
  renderVat(result.ticket?.vatDetails || []);
  jsonResponse.textContent = JSON.stringify(result, null, 2);
  resultPanel.hidden = false;
}

function renderDetails(container, items, showMissing = false) {
  container.replaceChildren();
  const renderedItems = showMissing
    ? items
    : items.filter(([, detail]) => detail !== undefined && detail !== null && detail !== '');
  renderedItems.forEach(([label, detail]) => {
    const wrapper = document.createElement('div');
    const term = document.createElement('dt');
    const description = document.createElement('dd');
    term.textContent = label;
    description.textContent = showMissing ? displayValue(detail) : value(detail);
    wrapper.append(term, description);
    container.append(wrapper);
  });
}

function renderProducts(products) {
  productsBody.replaceChildren();
  if (!products.length) {
    productsBody.append(createEmptyRow(5, 'No se han devuelto productos.'));
    return;
  }

  products.forEach((product) => {
    const row = document.createElement('tr');
    [product.concept, product.normalizedText, product.amount, productCategoryValue(product.category), booleanValue(product.isAlcohol)]
      .forEach((item) => {
        const cell = document.createElement('td');
        cell.textContent = value(item);
        row.append(cell);
      });
    productsBody.append(row);
  });
}

function renderVat(vatDetails) {
  vatBody.replaceChildren();
  vatSection.hidden = vatDetails.length === 0;
  vatDetails.forEach((vat) => {
    const row = document.createElement('tr');
    [vat.rate, vat.taxableAmount, vat.amount].forEach((item) => {
      const cell = document.createElement('td');
      cell.textContent = value(item);
      row.append(cell);
    });
    vatBody.append(row);
  });
}

function createEmptyRow(columnCount, text) {
  const row = document.createElement('tr');
  const cell = document.createElement('td');
  cell.colSpan = columnCount;
  cell.className = 'empty-row';
  cell.textContent = text;
  row.append(cell);
  return row;
}

function booleanValue(valueToFormat) {
  if (valueToFormat === true) return 'Sí';
  if (valueToFormat === false) return 'No';
  return 'Desconocido';
}

function statusValue(status) {
  return {
    APPROVED: 'APROBADO',
    REJECTED: 'RECHAZADO',
    REVIEW_REQUIRED: 'REVISIÓN REQUERIDA',
    UNREADABLE: 'NO LEGIBLE',
    PROCESSING_ERROR: 'ERROR DE PROCESAMIENTO'
  }[status] || value(status);
}

function documentTypeValue(documentType) {
  if (documentType === undefined || documentType === null || documentType === '') return documentType;

  return {
    TICKET: 'Ticket',
    Receipt: 'Ticket',
    FACTURA: 'Factura',
    Invoice: 'Factura',
    NO_DOCUMENTO: 'No es un documento válido',
    NotDocument: 'No es un documento válido',
    UNKNOWN: 'Desconocido',
    Unknown: 'Desconocido'
  }[documentType] || value(documentType);
}

function displayValue(valueToFormat) {
  return valueToFormat === undefined || valueToFormat === null || valueToFormat === ''
    ? 'Desconocido'
    : String(valueToFormat);
}

function establishmentTypeValue(establishmentType) {
  return {
    Restaurant: 'Restaurante',
    Hotel: 'Hotel',
    Transport: 'Transporte',
    Other: 'Otro',
    Unknown: 'Desconocido'
  }[establishmentType] || value(establishmentType);
}

function productCategoryValue(category) {
  return {
    Food: 'Comida',
    NonAlcoholicBeverage: 'Bebida sin alcohol',
    AlcoholicBeverage: 'Bebida alcohólica',
    NonFood: 'No alimentario',
    Other: 'Otro',
    Unknown: 'Desconocido'
  }[category] || value(category);
}

function value(valueToFormat) {
  return valueToFormat === undefined || valueToFormat === null || valueToFormat === '' ? '—' : String(valueToFormat);
}
