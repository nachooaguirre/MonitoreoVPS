package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.models.*
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

/// Renglón local en construcción, antes de mandarlo al server como OrdenCompraCreateItem.
private data class NuevaOcLinea(
    val idArticulo: Int,
    val descripcion: String,
    val codigoBarras: String?,
    var cantidad: Double,
    var precioCosto: Double,
    val alicuotaIva: Double
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NuevaOrdenCompraScreen(
    operatorId: Int,
    onNavigateBack: () -> Unit,
    registerScanCallback: ((String) -> Unit) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    var proveedores by remember { mutableStateOf<List<Proveedor>>(emptyList()) }
    var proveedorFiltro by remember { mutableStateOf("") }
    var proveedorElegido by remember { mutableStateOf<Proveedor?>(null) }

    val lineas = remember { mutableStateListOf<NuevaOcLinea>() }
    var barcodeInput by remember { mutableStateOf("") }
    var nombreInput by remember { mutableStateOf("") }
    var resultadosBusqueda by remember { mutableStateOf<List<Article>>(emptyList()) }
    var observaciones by remember { mutableStateOf("") }

    var articuloParaAgregar by remember { mutableStateOf<Article?>(null) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var guardadoOk by remember { mutableStateOf(false) }

    fun loadProveedores() {
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getProveedores() }
                if (response.isSuccessful) {
                    proveedores = response.body()?.items?.filter { it.activo } ?: emptyList()
                } else {
                    errorMessage = "Error al cargar proveedores: ${response.code()}"
                }
            } catch (e: Exception) {
                errorMessage = "Fallo de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    LaunchedEffect(Unit) { loadProveedores() }

    fun buscarPorCodigo(codigo: String) {
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(codigo) }
                if (response.isSuccessful && response.body() != null) {
                    articuloParaAgregar = response.body()
                } else {
                    errorMessage = "No se encontró un artículo con ese código."
                }
            } catch (e: Exception) {
                errorMessage = "Error al buscar: ${e.message}"
            }
        }
    }

    fun buscarPorNombre(nombre: String) {
        if (nombre.isBlank()) { resultadosBusqueda = emptyList(); return }
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.searchArticles(nombre) }
                resultadosBusqueda = if (response.isSuccessful) response.body()?.items ?: emptyList() else emptyList()
            } catch (e: Exception) {
                errorMessage = "Error al buscar: ${e.message}"
            }
        }
    }

    fun agregarLinea(art: Article, cantidad: Double, precio: Double) {
        val idx = lineas.indexOfFirst { it.idArticulo == art.id }
        if (idx != -1) {
            lineas[idx] = lineas[idx].copy(cantidad = lineas[idx].cantidad + cantidad)
        } else {
            lineas.add(NuevaOcLinea(art.id, art.descripcion, art.codigoBarras, cantidad, precio, art.alicuotaIva))
        }
        articuloParaAgregar = null
        barcodeInput = ""
        nombreInput = ""
        resultadosBusqueda = emptyList()
    }

    LaunchedEffect(Unit) {
        registerScanCallback { code -> buscarPorCodigo(code) }
    }

    fun guardarBorrador() {
        val prov = proveedorElegido ?: return
        if (lineas.isEmpty()) { errorMessage = "Agregá al menos un artículo."; return }
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val req = OrdenCompraCreateRequest(
                    idProveedor = prov.id,
                    idUsuario = operatorId,
                    observaciones = observaciones.ifBlank { null },
                    detalles = lineas.map {
                        OrdenCompraCreateItem(
                            idArticulo = it.idArticulo,
                            cantidadPedida = it.cantidad,
                            precioCosto = it.precioCosto,
                            alicuotaIva = it.alicuotaIva,
                            subtotal = Math.round(it.cantidad * it.precioCosto * 100) / 100.0
                        )
                    }
                )
                val response = withContext(Dispatchers.IO) { service.createOrdenCompra(req) }
                if (response.isSuccessful) {
                    guardadoOk = true
                } else {
                    errorMessage = "Error al guardar: ${response.code()}"
                }
            } catch (e: Exception) {
                errorMessage = "Error de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (proveedorElegido == null) "Nueva Orden de Compra" else "OC · ${proveedorElegido!!.razonSocial}") },
                navigationIcon = {
                    TextButton(onClick = {
                        if (proveedorElegido != null) {
                            proveedorElegido = null
                            lineas.clear()
                        } else {
                            onNavigateBack()
                        }
                    }) {
                        Text("Volver", color = FluentBlue, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = FluentDarkBg, titleContentColor = FluentWhite)
            )
        },
        containerColor = FluentDarkBg
    ) { paddingValues ->
        Box(modifier = modifier.fillMaxSize().padding(paddingValues).padding(16.dp)) {
            if (guardadoOk) {
                Column(modifier = Modifier.fillMaxSize(), verticalArrangement = Arrangement.Center, horizontalAlignment = Alignment.CenterHorizontally) {
                    Text("¡Orden guardada como borrador!", color = FluentSuccess, fontWeight = FontWeight.Bold, fontSize = 18.sp, textAlign = TextAlign.Center)
                    Spacer(modifier = Modifier.height(8.dp))
                    Text("Confirmala desde el sistema en la PC para enviarla al proveedor.", color = FluentLightGray, fontSize = 13.sp, textAlign = TextAlign.Center)
                    Spacer(modifier = Modifier.height(20.dp))
                    Button(onClick = onNavigateBack, colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg)) {
                        Text("Volver al menú")
                    }
                }
            } else if (proveedorElegido == null) {
                // PASO 1: elegir proveedor
                Column(modifier = Modifier.fillMaxSize()) {
                    Text("Elegí el proveedor", color = FluentWhite, fontSize = 16.sp, fontWeight = FontWeight.Bold)
                    Spacer(modifier = Modifier.height(12.dp))
                    OutlinedTextField(
                        value = proveedorFiltro,
                        onValueChange = { proveedorFiltro = it },
                        placeholder = { Text("Filtrar por nombre...") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        )
                    )
                    Spacer(modifier = Modifier.height(12.dp))

                    if (isLoading && proveedores.isEmpty()) {
                        CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally), color = FluentBlue)
                    } else {
                        val filtrados = proveedores.filter {
                            proveedorFiltro.isBlank() || it.razonSocial.contains(proveedorFiltro, ignoreCase = true)
                        }
                        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(filtrados) { prov ->
                                Card(
                                    modifier = Modifier.fillMaxWidth().clickable { proveedorElegido = prov },
                                    colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                                    shape = RoundedCornerShape(8.dp)
                                ) {
                                    Column(modifier = Modifier.padding(14.dp)) {
                                        Text(prov.razonSocial, fontWeight = FontWeight.Bold, color = FluentWhite, fontSize = 15.sp)
                                        if (!prov.cuit.isNullOrBlank()) {
                                            Text("CUIT: ${prov.cuit}", color = FluentLightGray, fontSize = 12.sp)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } else {
                // PASO 2: armar los renglones de la orden
                Column(modifier = Modifier.fillMaxSize()) {
                    errorMessage?.let { err ->
                        Text(
                            text = err, color = FluentError, fontSize = 13.sp, fontWeight = FontWeight.Bold,
                            modifier = Modifier.fillMaxWidth().background(FluentError.copy(alpha = 0.1f), RoundedCornerShape(4.dp)).padding(8.dp)
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                    }

                    OutlinedTextField(
                        value = barcodeInput,
                        onValueChange = { barcodeInput = it },
                        label = { Text("Escanear código de barras", fontSize = 12.sp) },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        ),
                        trailingIcon = {
                            TextButton(onClick = { if (barcodeInput.isNotBlank()) buscarPorCodigo(barcodeInput) }) {
                                Text("Buscar", color = FluentBlue, fontWeight = FontWeight.Bold)
                            }
                        }
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    OutlinedTextField(
                        value = nombreInput,
                        onValueChange = { nombreInput = it; buscarPorNombre(it) },
                        label = { Text("Buscar por nombre", fontSize = 12.sp) },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        )
                    )

                    if (resultadosBusqueda.isNotEmpty()) {
                        Spacer(modifier = Modifier.height(4.dp))
                        Column(
                            modifier = Modifier.fillMaxWidth().heightIn(max = 180.dp)
                                .background(FluentDarkCard, RoundedCornerShape(6.dp))
                        ) {
                            resultadosBusqueda.forEach { art ->
                                Text(
                                    text = "${art.descripcion}  ·  $${String.format(java.util.Locale.US, "%.2f", art.precioCosto)}",
                                    color = FluentWhite, fontSize = 13.sp,
                                    modifier = Modifier.fillMaxWidth()
                                        .clickable { articuloParaAgregar = art }
                                        .padding(10.dp)
                                )
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))
                    Text("Artículos (${lineas.size})", color = FluentWhite, fontWeight = FontWeight.Bold, fontSize = 14.sp)
                    Spacer(modifier = Modifier.height(6.dp))

                    LazyColumn(modifier = Modifier.weight(1f).fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                        items(lineas) { linea ->
                            Card(
                                modifier = Modifier.fillMaxWidth(),
                                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                                shape = RoundedCornerShape(6.dp)
                            ) {
                                Row(
                                    modifier = Modifier.padding(12.dp).fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Column(modifier = Modifier.weight(1f)) {
                                        Text(linea.descripcion, color = FluentWhite, fontWeight = FontWeight.Bold, fontSize = 13.sp)
                                        Text(
                                            "${String.format(java.util.Locale.US, "%.1f", linea.cantidad)} x $${String.format(java.util.Locale.US, "%.2f", linea.precioCosto)}",
                                            color = FluentLightGray, fontSize = 12.sp
                                        )
                                    }
                                    TextButton(onClick = { lineas.remove(linea) }) {
                                        Text("Quitar", color = FluentError, fontSize = 12.sp)
                                    }
                                }
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(8.dp))
                    OutlinedTextField(
                        value = observaciones,
                        onValueChange = { observaciones = it },
                        label = { Text("Observaciones (opcional)", fontSize = 12.sp) },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        )
                    )

                    Spacer(modifier = Modifier.height(10.dp))
                    Button(
                        onClick = { guardarBorrador() },
                        enabled = !isLoading && lineas.isNotEmpty(),
                        modifier = Modifier.fillMaxWidth().height(50.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                        shape = RoundedCornerShape(8.dp)
                    ) {
                        Text(if (isLoading) "Guardando..." else "Guardar como borrador", fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }

    // Diálogo para cargar cantidad y costo del artículo elegido (por código o por nombre)
    articuloParaAgregar?.let { art ->
        var cantidadTxt by remember(art.id) { mutableStateOf("1") }
        var precioTxt by remember(art.id) { mutableStateOf(if (art.precioCosto > 0) String.format(java.util.Locale.US, "%.2f", art.precioCosto) else "") }

        Dialog(onDismissRequest = { articuloParaAgregar = null }) {
            Card(
                modifier = Modifier.fillMaxWidth(0.95f),
                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(art.descripcion, fontWeight = FontWeight.Bold, fontSize = 16.sp, color = FluentWhite)
                    Spacer(modifier = Modifier.height(4.dp))
                    Text("EAN: ${art.codigoBarras.ifBlank { "S/C" }}", color = FluentLightGray, fontSize = 12.sp)
                    Spacer(modifier = Modifier.height(14.dp))

                    OutlinedTextField(
                        value = cantidadTxt, onValueChange = { cantidadTxt = it },
                        label = { Text("Cantidad") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        singleLine = true, modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        )
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    OutlinedTextField(
                        value = precioTxt, onValueChange = { precioTxt = it },
                        label = { Text("Costo unitario") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        singleLine = true, modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite, unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue, unfocusedBorderColor = FluentDivider
                        )
                    )

                    Spacer(modifier = Modifier.height(16.dp))
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                        Button(
                            onClick = { articuloParaAgregar = null },
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentDivider, contentColor = FluentWhite),
                            shape = RoundedCornerShape(4.dp)
                        ) { Text("Cancelar") }

                        Button(
                            onClick = {
                                val cant = cantidadTxt.toDoubleOrNull() ?: 0.0
                                val precio = precioTxt.toDoubleOrNull() ?: 0.0
                                if (cant > 0.0) agregarLinea(art, cant, precio)
                            },
                            modifier = Modifier.weight(1.5f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                            shape = RoundedCornerShape(4.dp)
                        ) { Text("Agregar", fontWeight = FontWeight.Bold) }
                    }
                }
            }
        }
    }
}
