// ===== CONFIG & STATE =====
const API_BASE = '/api';
let currentState = {
    activeView: 'view-menu',
    // Inventario State
    inventarios: [],
    selectedInventario: null,
    scannedInvArticle: null,
    // Recepción State
    ocs: [],
    selectedOC: null,
    ocItems: [], // Local copy of items for real-time comparison
    // Etiquetas State
    scannedLabelArticle: null
};

// ===== INITIALIZATION =====
document.addEventListener('DOMContentLoaded', () => {
    // Interceptar scanner en Inventario
    setupScannerInput('txt-inv-barcode', handleInventarioScan);
    // Interceptar scanner en Recepción
    setupScannerInput('txt-oc-barcode', handleOCScan);
    // Interceptar scanner en Etiquetas
    setupScannerInput('txt-lbl-barcode', handleEtiquetaScan);
    // Interceptar scanner en Agregar al Stock
    setupScannerInput('txt-stock-barcode', handleStockScan);

    // Health check periódico
    checkApiHealth();
    setInterval(checkApiHealth, 10000);
});

// ===== VIEWS SYSTEM =====
function switchView(viewId) {
    if (typeof stopCameraScanner === 'function') {
        stopCameraScanner();
    }
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    document.getElementById(viewId).classList.add('active');
    currentState.activeView = viewId;

    // Ejecutar carga inicial correspondiente
    if (viewId === 'view-inventario') {
        if (!currentState.selectedInventario) {
            cargarInventariosActivos();
        } else {
            setTimeout(() => document.getElementById('txt-inv-barcode').focus(), 150);
        }
    } else if (viewId === 'view-recepcion') {
        if (!currentState.selectedOC) {
            cargarOCsPendientes();
        } else {
            setTimeout(() => document.getElementById('txt-oc-barcode').focus(), 150);
        }
    } else if (viewId === 'view-etiquetas') {
        setTimeout(() => document.getElementById('txt-lbl-barcode').focus(), 150);
    } else if (viewId === 'view-ingreso-stock') {
        cargarOpcionesSucursales();
        setTimeout(() => document.getElementById('txt-stock-barcode').focus(), 150);
    }
}

// ===== API HELPERS =====
async function checkApiHealth() {
    try {
        const res = await fetch(`${API_BASE}/health`);
        const badge = document.getElementById('txt-status');
        if (res.ok) {
            badge.textContent = 'Conectado';
            badge.className = 'status-badge online';
        } else {
            throw new Error();
        }
    } catch {
        const badge = document.getElementById('txt-status');
        badge.textContent = 'Sin Conexión';
        badge.className = 'status-badge offline';
    }
}

function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `<span>${message}</span>`;
    container.appendChild(toast);

    // Vibrar dispositivo si está soportado
    if (navigator.vibrate) {
        if (type === 'error') navigator.vibrate([100, 50, 100]);
        else if (type === 'success') navigator.vibrate(80);
    }

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(10px)';
        setTimeout(() => toast.remove(), 300);
    }, 2500);
}

// Configurar inputs de scanner para atrapar lecturas (captura "Enter")
function setupScannerInput(inputId, handlerFn) {
    const input = document.getElementById(inputId);
    if (!input) return;

    input.addEventListener('keydown', async (e) => {
        if (e.key === 'Enter' || e.keyCode === 13) {
            e.preventDefault();
            const val = input.value.trim();
            if (val) {
                input.disabled = true;
                await handlerFn(val);
                input.value = '';
                input.disabled = false;
                input.focus();
            }
        }
    });

    // Auto-enfocar al hacer click en el card o contenedor
    input.parentElement.addEventListener('click', () => input.focus());
}

// ===== 📦 MODULO: CONTEO DE STOCK =====
async function cargarInventariosActivos() {
    const container = document.getElementById('list-inventarios');
    container.innerHTML = '<p class="loading">Buscando inventarios...</p>';

    try {
        const res = await fetch(`${API_BASE}/inventarios`);
        if (!res.ok) throw new Error('Error al obtener inventarios');
        const list = await res.json();
        
        // Filtrar activos (Estado = 0 -> EnProceso)
        const activos = list.filter(i => i.estado === 0);

        if (activos.length === 0) {
            container.innerHTML = '<p class="loading">No hay inventarios abiertos en el sistema. Creá uno en el panel central de la PC.</p>';
            return;
        }

        container.innerHTML = '';
        activos.forEach(inv => {
            const btn = document.createElement('button');
            btn.className = 'list-item';
            btn.onclick = () => seleccionarInventario(inv);
            btn.innerHTML = `
                <div style="text-align: left;">
                    <div class="list-item-title">${inv.descripcion}</div>
                    <div class="list-item-meta">Sucursal: ${inv.sucursalNombre || 'Depósito'} · Items: ${inv.articulosContados}/${inv.totalArticulos}</div>
                </div>
                <span>→</span>
            `;
            container.appendChild(btn);
        });
    } catch (err) {
        container.innerHTML = `<p class="loading" style="color:var(--danger)">${err.message}</p>`;
    }
}

function seleccionarInventario(inv) {
    currentState.selectedInventario = inv;
    document.getElementById('lbl-inv-name').textContent = inv.descripcion;
    document.getElementById('step-inv-select').classList.add('hidden');
    document.getElementById('step-inv-scan').classList.remove('hidden');
    document.getElementById('txt-inv-barcode').focus();
}

function abandonarSesionInventario() {
    currentState.selectedInventario = null;
    currentState.scannedInvArticle = null;
    document.getElementById('inv-article-info').classList.add('hidden');
    document.getElementById('step-inv-scan').classList.add('hidden');
    document.getElementById('step-inv-select').classList.remove('hidden');
    cargarInventariosActivos();
}

async function handleInventarioScan(barcode) {
    const infoCard = document.getElementById('inv-article-info');
    infoCard.classList.add('hidden');

    try {
        const res = await fetch(`${API_BASE}/articulos/buscarPorCodigoBarras/${barcode}`);
        if (!res.ok) {
            if (confirm('El artículo no existe en el catálogo. ¿Deseás crearlo ahora?')) {
                abrirModalNuevoArticulo(barcode, handleInventarioScan);
            }
            return;
        }

        const art = await res.json();
        currentState.scannedInvArticle = art;

        // Rellenar UI
        document.getElementById('lbl-inv-art-desc').textContent = art.descripcion;
        document.getElementById('lbl-inv-art-ean').textContent = art.codigoBarras || art.codigoInterno;
        document.getElementById('lbl-inv-art-stock').textContent = parseFloat(art.stockActual).toFixed(2);
        
        // Resetear spinner de cantidad
        document.getElementById('num-inv-qty').value = "1";

        infoCard.classList.remove('hidden');
        document.getElementById('num-inv-qty').select();
    } catch (err) {
        showToast('Error al buscar artículo: ' + err.message, 'error');
    }
}

function adjustCount(val) {
    const input = document.getElementById('num-inv-qty');
    let current = parseFloat(input.value) || 0;
    current += val;
    if (current < 0) current = 0;
    input.value = current;
}

async function guardarConteo(acumulativo) {
    if (!currentState.selectedInventario || !currentState.scannedInvArticle) return;

    const qty = parseFloat(document.getElementById('num-inv-qty').value) || 0;
    const invId = currentState.selectedInventario.id;
    const artId = currentState.scannedInvArticle.id;

    try {
        const res = await fetch(`${API_BASE}/inventarios/${invId}/contar`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idArticulo: artId,
                stockContado: qty,
                observaciones: 'Contado desde terminal Zebra',
                acumulativo: acumulativo
            })
        });

        if (!res.ok) throw new Error('Error al registrar conteo');

        showToast(`${currentState.scannedInvArticle.descripcion} cargado correctamente.`, 'success');
        
        // Limpiar info y reenfocar scanner
        document.getElementById('inv-article-info').classList.add('hidden');
        currentState.scannedInvArticle = null;
        document.getElementById('txt-inv-barcode').focus();
    } catch (err) {
        showToast(err.message, 'error');
    }
}

// ===== 🚚 MODULO: RECEPCIÓN DE CAMIÓN (OC) =====
async function cargarOCsPendientes() {
    const container = document.getElementById('list-ocs');
    container.innerHTML = '<p class="loading">Buscando órdenes de compra...</p>';

    try {
        const res = await fetch(`${API_BASE}/ordenescompra`);
        if (!res.ok) throw new Error('Error al obtener órdenes de compra');
        const list = await res.json();
        
        // Filtrar pendientes, enviadas o parciales (Estado = 0, 1 o 2)
        const pendientes = list.filter(o => o.estado === 0 || o.estado === 1 || o.estado === 2);

        if (pendientes.length === 0) {
            container.innerHTML = '<p class="loading">No hay órdenes de compra pendientes para recibir.</p>';
            return;
        }

        container.innerHTML = '';
        pendientes.forEach(oc => {
            const fechaStr = new Date(oc.fecha).toLocaleDateString('es-AR');
            const btn = document.createElement('button');
            btn.className = 'list-item';
            btn.onclick = () => seleccionarOC(oc);
            btn.innerHTML = `
                <div style="text-align: left;">
                    <div class="list-item-title">OC #${oc.nroOrden} · ${oc.proveedorNombre || 'Sin Proveedor'}</div>
                    <div class="list-item-meta">Fecha: ${fechaStr} · Total: $${parseFloat(oc.total).toFixed(2)}</div>
                </div>
                <span>→</span>
            `;
            container.appendChild(btn);
        });
    } catch (err) {
        container.innerHTML = `<p class="loading" style="color:var(--danger)">${err.message}</p>`;
    }
}

async function seleccionarOC(oc) {
    try {
        const res = await fetch(`${API_BASE}/ordenescompra/${oc.id}`);
        if (!res.ok) throw new Error('No se pudo cargar el detalle de la OC');
        const detalle = await res.json();

        currentState.selectedOC = detalle;
        // Mapear items locales para el conteo de recepción
        currentState.ocItems = detalle.detalles.map(d => ({
            idArticulo: d.idArticulo,
            descripcion: d.articulo?.descripcion || 'Artículo desconocido',
            codigoBarras: d.articulo?.codigoBarras,
            cantidadPedida: d.cantidadPedida,
            cantidadRecibida: d.cantidadRecibida || 0,
            precioCosto: d.precioCosto
        }));

        document.getElementById('lbl-oc-name').textContent = `OC #${detalle.nroOrden} · ${detalle.proveedorNombre || 'Proveedor'}`;
        document.getElementById('step-oc-select').classList.add('hidden');
        document.getElementById('step-oc-control').classList.remove('hidden');
        
        renderOcItems();
        document.getElementById('txt-oc-barcode').focus();
    } catch (err) {
        showToast(err.message, 'error');
    }
}

function abandonarSesionOC() {
    currentState.selectedOC = null;
    currentState.ocItems = [];
    document.getElementById('step-oc-control').classList.add('hidden');
    document.getElementById('step-oc-select').classList.remove('hidden');
    cargarOCsPendientes();
}

function renderOcItems() {
    const container = document.getElementById('list-oc-items');
    container.innerHTML = '';

    let itemsValidados = 0;
    currentState.ocItems.forEach(item => {
        const row = document.createElement('div');
        
        let badgeClass = 'pending';
        let badgeText = 'Pendiente';
        if (item.cantidadRecibida === item.cantidadPedida) {
            badgeClass = 'ok';
            badgeText = 'Completo';
            row.className = 'oc-item-row completed';
            itemsValidados++;
        } else if (item.cantidadRecibida > item.cantidadPedida) {
            badgeClass = 'wrong';
            badgeText = 'Excedido';
            row.className = 'oc-item-row excess';
            itemsValidados++;
        } else if (item.cantidadRecibida > 0) {
            badgeClass = 'pending';
            badgeText = 'Parcial';
            row.className = 'oc-item-row';
        } else {
            row.className = 'oc-item-row';
        }

        row.innerHTML = `
            <div class="oc-item-info">
                <div class="oc-item-name">${item.descripcion}</div>
                <div class="oc-item-ean">EAN: ${item.codigoBarras || 'S/C'}</div>
            </div>
            <div class="oc-item-qty">
                <div><strong>${parseFloat(item.cantidadRecibida).toFixed(2)}</strong> / ${parseFloat(item.cantidadPedida).toFixed(2)}</div>
                <span class="oc-qty-badge ${badgeClass}">${badgeText}</span>
            </div>
        `;
        container.appendChild(row);
    });

    document.getElementById('lbl-oc-progress').textContent = `${itemsValidados}/${currentState.ocItems.length} items`;
}

async function handleOCScan(barcode) {
    // Buscar artículo en la orden de compra local
    const item = currentState.ocItems.find(i => i.codigoBarras === barcode);
    
    if (item) {
        item.cantidadRecibida += 1; // Sumar uno al escaneo rápido
        showToast(`+1 ${item.descripcion}`, 'success');
        renderOcItems();
    } else {
        // Artículo no está en la OC. Buscar en base general para ver si es un artículo válido
        try {
            const res = await fetch(`${API_BASE}/articulos/buscarPorCodigoBarras/${barcode}`);
            if (!res.ok) {
                if (confirm('El artículo no está en la OC ni en el catálogo. ¿Deseás registrarlo ahora como ítem excedente?')) {
                    abrirModalNuevoArticulo(barcode, handleOCScan);
                }
                return;
            }

            const art = await res.json();
            // Confirmación para agregar item extra que no venía en la OC
            if (confirm(`El artículo "${art.descripcion}" no pertenece a esta Orden de Compra. ¿Deseás recibirlo de todas formas como ítem excedente?`)) {
                currentState.ocItems.push({
                    idArticulo: art.id,
                    descripcion: art.descripcion,
                    codigoBarras: art.codigoBarras,
                    cantidadPedida: 0,
                    cantidadRecibida: 1,
                    precioCosto: art.precioCosto
                });
                showToast(`Excedente: +1 ${art.descripcion}`, 'warning');
                renderOcItems();
            }
        } catch {
            showToast('Error de escaneo.', 'error');
        }
    }
}

async function finalizarRecepcionOC() {
    if (!currentState.selectedOC) return;

    if (!confirm('¿Confirmar e ingresar el camión al stock del depósito?')) return;

    const ocId = currentState.selectedOC.id;
    const payload = {
        idUsuario: 1, // ID operador de referencia
        idSucursalDestino: null, // Asume central por defecto
        items: currentState.ocItems.map(i => ({
            idArticulo: i.idArticulo,
            cantidadRecibida: i.cantidadRecibida,
            precioCosto: i.precioCosto
        }))
    };

    try {
        const res = await fetch(`${API_BASE}/ordenescompra/${ocId}/recibir`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) throw new Error('Error al confirmar recepción en servidor.');

        showToast('¡Recepción finalizada con éxito! Stock ingresado.', 'success');
        abandonarSesionOC();
    } catch (err) {
        showToast(err.message, 'error');
    }
}

// ===== 🏷️ MODULO: ENCOLAR ETIQUETAS =====
async function handleEtiquetaScan(barcode) {
    const infoCard = document.getElementById('lbl-article-info');
    infoCard.classList.add('hidden');

    try {
        const res = await fetch(`${API_BASE}/articulos/buscarPorCodigoBarras/${barcode}`);
        if (!res.ok) {
            if (confirm('El artículo no existe en el catálogo. ¿Deseás crearlo ahora?')) {
                abrirModalNuevoArticulo(barcode, handleEtiquetaScan);
            }
            return;
        }

        const art = await res.json();
        currentState.scannedLabelArticle = art;

        // Rellenar UI
        const precio = parseFloat(art.precioVenta) || 0;
        const integerPart = Math.floor(precio);
        const decimalPart = (precio - integerPart).toFixed(2).substring(1); // ej: ".99"
        
        document.getElementById('ticket-art-desc').textContent = art.descripcion;
        document.getElementById('ticket-price-whole').textContent = integerPart;
        document.getElementById('ticket-price-decimal').textContent = decimalPart;
        document.getElementById('ticket-code-internal').textContent = `INT: ${art.codigoInterno || art.id}`;
        document.getElementById('ticket-date-str').textContent = new Date().toLocaleDateString('es-AR');
        document.getElementById('ticket-barcode-svg').innerHTML = generateMockBarcodeSVG(art.codigoBarras || art.codigoInterno || '0000000000000');
        
        document.getElementById('num-label-qty').value = "1";

        infoCard.classList.remove('hidden');
        document.getElementById('num-label-qty').focus();
    } catch (err) {
        showToast('Error al buscar: ' + err.message, 'error');
    }
}

function adjustLabelQty(val) {
    const input = document.getElementById('num-label-qty');
    let current = parseInt(input.value) || 0;
    current += val;
    if (current < 1) current = 1;
    input.value = current;
}

async function encolarEtiquetaApi() {
    if (!currentState.scannedLabelArticle) return;

    const qty = parseInt(document.getElementById('num-label-qty').value) || 1;
    const artId = currentState.scannedLabelArticle.id;

    try {
        const res = await fetch(`${API_BASE}/etiquetas/encolar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idArticulo: artId,
                cantidad: qty
            })
        });

        if (!res.ok) throw new Error('Error al encolar etiqueta');

        showToast(`Etiqueta encolada: ${currentState.scannedLabelArticle.descripcion} (x${qty})`, 'success');
        
        // Limpiar
        document.getElementById('lbl-article-info').classList.add('hidden');
        currentState.scannedLabelArticle = null;
        document.getElementById('txt-lbl-barcode').focus();
    } catch (err) {
        showToast(err.message, 'error');
    }
}

// ===== SISTEMA DE ESCANEO POR CÁMARA (MOBILE) =====
let html5QrcodeScanner = null;
let activeCameraReaderId = null;
let isStoppingScanner = false;

async function toggleCameraScanner(readerId, inputId) {
    const readerDiv = document.getElementById(readerId);
    
    if (html5QrcodeScanner && activeCameraReaderId === readerId) {
        await stopCameraScanner();
        return;
    }
    
    if (html5QrcodeScanner) {
        await stopCameraScanner();
    }
    
    readerDiv.classList.remove('hidden');
    activeCameraReaderId = readerId;
    
    const supported = window.Html5QrcodeSupportedFormats || (window.Html5Qrcode && window.Html5Qrcode.SupportedFormats);
    const formats = supported ? [
        supported.EAN_13,
        supported.EAN_8,
        supported.CODE_128,
        supported.CODE_39,
        supported.UPC_A,
        supported.UPC_E,
        supported.QR_CODE
    ] : undefined;

    html5QrcodeScanner = new Html5Qrcode(readerId, formats ? { formatsToSupport: formats } : undefined);
    
    try {
        const config = { 
            fps: 15, 
            qrbox: (width, height) => {
                // Caja más ancha y alta optimizada para códigos de barra
                return { width: Math.min(width * 0.85, 290), height: Math.min(height * 0.35, 115) };
            },
            aspectRatio: 1.7777778, // Forzar formato 16:9 para mayor nitidez
            videoConstraints: {
                facingMode: "environment", // Forzar cámara trasera
                width: { min: 640, ideal: 1280, max: 1920 },
                height: { min: 480, ideal: 720, max: 1080 }
            }
        };
        
        await html5QrcodeScanner.start(
            { facingMode: { exact: "environment" } }, 
            config,
            async (decodedText) => {
                if (isStoppingScanner) return;
                isStoppingScanner = true;
                
                if (navigator.vibrate) navigator.vibrate(80);
                
                const input = document.getElementById(inputId);
                input.value = decodedText;
                
                await stopCameraScanner();
                isStoppingScanner = false;
                
                // Disparar Enter
                const event = new KeyboardEvent('keydown', {
                    key: 'Enter',
                    code: 'Enter',
                    keyCode: 13,
                    which: 13,
                    bubbles: true
                });
                input.dispatchEvent(event);
            },
            (errorMessage) => {
                // Silenciar lecturas intermedias
            }
        );
        showToast("Cámara iniciada. Apuntá al código de barras.", "success");
    } catch (err) {
        showToast("No se pudo iniciar la cámara: " + err, "error");
        readerDiv.classList.add('hidden');
        html5QrcodeScanner = null;
        activeCameraReaderId = null;
    }
}

async function stopCameraScanner() {
    if (!html5QrcodeScanner) return;
    
    try {
        await html5QrcodeScanner.stop();
    } catch (err) {
        console.error(err);
    }
    
    if (activeCameraReaderId) {
        document.getElementById(activeCameraReaderId).classList.add('hidden');
    }
    
    html5QrcodeScanner = null;
    activeCameraReaderId = null;
}

// ===== 🆕 MODAL: CREAR NUEVO ARTÍCULO DESDE EL MÓVIL =====
let pendingScanBarcode = null;
let pendingScanHandler = null;
let dropdownsLoaded = false;

async function abrirModalNuevoArticulo(barcode, handlerFn) {
    pendingScanBarcode = barcode;
    pendingScanHandler = handlerFn;

    document.getElementById('new-art-ean').value = barcode;
    document.getElementById('new-art-desc').value = '';
    document.getElementById('new-art-costo').value = '0.00';
    document.getElementById('new-art-venta').value = '0.00';
    document.getElementById('new-art-stock').value = '1.00';

    // Abrir modal visualmente
    document.getElementById('modal-nuevo-articulo').classList.remove('hidden');

    // Cargar listas desplegables la primera vez
    if (!dropdownsLoaded) {
        await cargarOpcionesArticulo();
        dropdownsLoaded = true;
    }
}

function cerrarModalNuevoArticulo() {
    document.getElementById('modal-nuevo-articulo').classList.add('hidden');
    pendingScanBarcode = null;
    pendingScanHandler = null;
}

async function cargarOpcionesArticulo() {
    try {
        // Cargar Proveedores
        const resProv = await fetch(`${API_BASE}/proveedores?pageSize=500`);
        if (resProv.ok) {
            const data = await resProv.json();
            const select = document.getElementById('new-art-proveedor');
            let html = '<option value="">Seleccionar...</option>';
            data.items.forEach(p => {
                html += `<option value="${p.id}">${p.razonSocial}</option>`;
            });
            select.innerHTML = html;
        }

        // Cargar Departamentos
        const resDepto = await fetch(`${API_BASE}/articulos/departamentos`);
        if (resDepto.ok) {
            const deptos = await resDepto.json();
            const select = document.getElementById('new-art-depto');
            let html = '<option value="">Seleccionar...</option>';
            deptos.forEach(d => {
                html += `<option value="${d.id}">${d.nombre}</option>`;
            });
            select.innerHTML = html;
        }

        // Cargar Marcas
        const resMarca = await fetch(`${API_BASE}/articulos/marcas`);
        if (resMarca.ok) {
            const marcas = await resMarca.json();
            const select = document.getElementById('new-art-marca');
            let html = '<option value="">Seleccionar...</option>';
            marcas.forEach(m => {
                html += `<option value="${m.id}">${m.nombre}</option>`;
            });
            select.innerHTML = html;
        }
    } catch (err) {
        showToast('Error al cargar listas: ' + err.message, 'error');
    }
}

async function onDepartamentoChanged() {
    const deptoId = document.getElementById('new-art-depto').value;
    const selectFam = document.getElementById('new-art-familia');
    selectFam.innerHTML = '<option value="">Seleccionar...</option>';

    if (!deptoId) {
        selectFam.innerHTML = '<option value="">Seleccioná depto primero...</option>';
        return;
    }

    try {
        const res = await fetch(`${API_BASE}/articulos/familias?idDepartamento=${deptoId}`);
        if (res.ok) {
            const familias = await res.json();
            let html = '<option value="">Seleccionar...</option>';
            familias.forEach(f => {
                html += `<option value="${f.id}">${f.nombre}</option>`;
            });
            selectFam.innerHTML = html;
        }
    } catch (err) {
        showToast('Error al cargar familias: ' + err.message, 'error');
    }
}

async function guardarNuevoArticulo(event) {
    event.preventDefault();

    const payload = {
        codigoBarras: document.getElementById('new-art-ean').value,
        codigoInterno: document.getElementById('new-art-ean').value, // Por defecto usamos el código de barras
        codigoProveedor: '',
        descripcion: document.getElementById('new-art-desc').value.trim().toUpperCase(),
        descripcionCorta: document.getElementById('new-art-desc').value.trim().substring(0, 20).toUpperCase(),
        precioCosto: parseFloat(document.getElementById('new-art-costo').value) || 0,
        precioVenta: parseFloat(document.getElementById('new-art-venta').value) || 0,
        stockActual: parseFloat(document.getElementById('new-art-stock').value) || 0,
        idProveedor: parseInt(document.getElementById('new-art-proveedor').value),
        idDepartamento: parseInt(document.getElementById('new-art-depto').value),
        idFamilia: parseInt(document.getElementById('new-art-familia').value),
        idMarca: parseInt(document.getElementById('new-art-marca').value),
        activo: true,
        aplicaIva: true,
        alicuotaIva: 21
    };

    try {
        const res = await fetch(`${API_BASE}/articulos`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            const errText = await res.text();
            throw new Error(errText || 'Error al guardar el artículo');
        }

        showToast('¡Artículo creado correctamente!', 'success');
        cerrarModalNuevoArticulo();

        // Si hay una función pendiente, re-escanear para seguir el flujo
        if (pendingScanHandler && pendingScanBarcode) {
            const handler = pendingScanHandler;
            const barcode = pendingScanBarcode;
            setTimeout(() => handler(barcode), 250);
        }
    } catch (err) {
        showToast('Error al guardar: ' + err.message, 'error');
    }
}

// ===== ➕ MODULO: INGRESO DE STOCK DIRECTO =====
let sucursalesLoaded = false;

async function cargarOpcionesSucursales() {
    if (sucursalesLoaded) return;
    
    try {
        const res = await fetch(`${API_BASE}/sucursales`);
        if (!res.ok) throw new Error('Error al cargar sucursales');
        const list = await res.json();
        
        const select = document.getElementById('sel-stock-sucursal');
        let html = '';
        list.forEach(s => {
            html += `<option value="${s.id}" ${s.esCentral ? 'selected' : ''}>${s.nombre} ${s.esCentral ? '(Central)' : ''}</option>`;
        });
        select.innerHTML = html;
        sucursalesLoaded = true;
    } catch (err) {
        showToast('Error al cargar sucursales: ' + err.message, 'error');
    }
}

async function handleStockScan(barcode) {
    const infoCard = document.getElementById('stock-article-info');
    infoCard.classList.add('hidden');
    currentState.scannedStockArticle = null;

    try {
        const res = await fetch(`${API_BASE}/articulos/buscarPorCodigoBarras/${barcode}`);
        if (!res.ok) {
            if (confirm('El artículo no existe en el catálogo. ¿Deseás crearlo ahora?')) {
                abrirModalNuevoArticulo(barcode, handleStockScan);
            }
            return;
        }

        const art = await res.json();
        currentState.scannedStockArticle = art;

        // Rellenar UI
        document.getElementById('lbl-stock-art-desc').textContent = art.descripcion;
        document.getElementById('lbl-stock-art-ean').textContent = art.codigoBarras || art.codigoInterno;
        document.getElementById('lbl-stock-art-current').textContent = parseFloat(art.stockActual).toFixed(2);
        
        document.getElementById('num-stock-qty').value = "1";

        infoCard.classList.remove('hidden');
        document.getElementById('num-stock-qty').focus();
        document.getElementById('num-stock-qty').select();
    } catch (err) {
        showToast('Error al buscar artículo: ' + err.message, 'error');
    }
}

function adjustStockAddQty(val) {
    const input = document.getElementById('num-stock-qty');
    let current = parseFloat(input.value) || 0;
    current += val;
    if (current < 0) current = 0;
    input.value = current;
}

async function guardarIngresoStockDirecto() {
    if (!currentState.scannedStockArticle) return;

    const qty = parseFloat(document.getElementById('num-stock-qty').value) || 0;
    if (qty <= 0) {
        showToast('La cantidad a ingresar debe ser mayor que cero.', 'error');
        return;
    }

    const artId = currentState.scannedStockArticle.id;
    const sucId = document.getElementById('sel-stock-sucursal').value;

    try {
        const res = await fetch(`${API_BASE}/articulos/${artId}/ajustar-stock?delta=${qty}&idSucursal=${sucId}`, {
            method: 'PUT'
        });

        if (!res.ok) throw new Error('Error al ingresar el stock en el servidor.');

        showToast(`Se agregaron ${qty} unidades a ${currentState.scannedStockArticle.descripcion}.`, 'success');
        
        // Limpiar info y reenfocar
        document.getElementById('stock-article-info').classList.add('hidden');
        currentState.scannedStockArticle = null;
        document.getElementById('txt-stock-barcode').focus();
    } catch (err) {
        showToast(err.message, 'error');
    }
}

// ===== BARCODE SVG GENERATOR FOR PREVIEWS =====
function generateMockBarcodeSVG(code) {
    let lines = '';
    let x = 6;
    const len = 34; // número de líneas de barras
    for (let i = 0; i < len; i++) {
        // Alternar anchos de líneas de forma pseudo-aleatoria pero consistente por código para parecer real
        const charCode = code.charCodeAt(i % code.length) || 0;
        const width = (charCode % 3 === 0) ? 2 : 1;
        const gap = (charCode % 2 === 0) ? 2 : 1;
        lines += `<rect x="${x}" y="3" width="${width}" height="24" fill="black" />`;
        x += width + gap;
    }
    // Asegurar que el SVG sea responsivo
    return `<svg width="100%" height="100%" viewBox="0 0 110 38" preserveAspectRatio="none">
        ${lines}
        <text x="55" y="35" font-family="monospace" font-size="8" text-anchor="middle" fill="black">${code}</text>
    </svg>`;
}


