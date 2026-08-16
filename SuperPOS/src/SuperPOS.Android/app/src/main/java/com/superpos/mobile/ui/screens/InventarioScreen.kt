package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.models.Article
import com.superpos.mobile.models.Inventario
import com.superpos.mobile.models.InventarioConteoRequest
import com.superpos.mobile.ui.components.NuevoArticuloDialog
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InventarioScreen(
    operatorId: Int, // User ID passed from login
    onNavigateBack: () -> Unit,
    registerScanCallback: ((String) -> Unit) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    var activeInventarios by remember { mutableStateOf<List<Inventario>>(emptyList()) }
    var selectedInv by remember { mutableStateOf<Inventario?>(null) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    // Session State
    var barcodeInput by remember { mutableStateOf("") }
    var scannedArticle by remember { mutableStateOf<Article?>(null) }
    var countedQty by remember { mutableStateOf("1.0") }
    var actionMessage by remember { mutableStateOf<String?>(null) }
    var showQtyDialog by remember { mutableStateOf(false) }

    // Dialog state
    var showCreateDialog by remember { mutableStateOf(false) }
    var pendingBarcode by remember { mutableStateOf("") }

    // Fetch active inventory sessions
    fun loadInventarios() {
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getInventarios() }
                if (response.isSuccessful) {
                    val all = response.body() ?: emptyList()
                    // Filter "EnProceso = 0"
                    activeInventarios = all.filter { it.estado == 0 }
                } else {
                    errorMessage = "Error al obtener inventarios: ${response.code()}"
                }
            } catch (e: Exception) {
                errorMessage = "Fallo de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    LaunchedEffect(Unit) {
        loadInventarios()
    }

    // Article search handler
    fun performSearch(barcode: String) {
        if (barcode.isBlank() || selectedInv == null) return
        isLoading = true
        actionMessage = null
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(barcode) }
                if (response.isSuccessful && response.body() != null) {
                    scannedArticle = response.body()
                    countedQty = "1.0"
                    showQtyDialog = true
                } else {
                    // Not found, open creation dialog
                    pendingBarcode = barcode
                    showCreateDialog = true
                }
            } catch (e: Exception) {
                errorMessage = "Error de conexión: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    // Register physical hardware scan receiver
    LaunchedEffect(selectedInv) {
        if (selectedInv != null) {
            registerScanCallback { code ->
                barcodeInput = code
                performSearch(code)
            }
        }
    }

    // Save count API helper
    fun saveCount(qty: Double, acumulativo: Boolean) {
        val article = scannedArticle ?: return
        val inventory = selectedInv ?: return

        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val req = InventarioConteoRequest(
                    idArticulo = article.id,
                    stockContado = qty,
                    observaciones = "Contado desde terminal Zebra",
                    acumulativo = acumulativo
                )
                val response = withContext(Dispatchers.IO) {
                    service.recordInventoryCount(inventory.id, req)
                }
                if (response.isSuccessful) {
                    actionMessage = "${article.descripcion} guardado con éxito."
                    scannedArticle = null
                    barcodeInput = ""
                } else {
                    errorMessage = "Error al guardar conteo: ${response.code()}"
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
                title = { Text(if (selectedInv == null) "Conteo de Stock" else selectedInv!!.descripcion) },
                navigationIcon = {
                    TextButton(onClick = {
                        if (selectedInv != null) {
                            selectedInv = null
                            scannedArticle = null
                            barcodeInput = ""
                            loadInventarios()
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
        Box(
            modifier = modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp)
        ) {
            if (selectedInv == null) {
                // STEP 1: SELECT INVENTORY
                Column(modifier = Modifier.fillMaxSize()) {
                    Text(
                        text = "Seleccioná un inventario activo",
                        color = FluentWhite,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                    
                    Spacer(modifier = Modifier.height(12.dp))

                    if (isLoading && activeInventarios.isEmpty()) {
                        CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally), color = FluentBlue)
                    } else if (errorMessage != null && activeInventarios.isEmpty()) {
                        Text(errorMessage!!, color = FluentError, modifier = Modifier.fillMaxWidth(), textAlign = TextAlign.Center)
                        Spacer(modifier = Modifier.height(10.dp))
                        Button(
                            onClick = { loadInventarios() },
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlueDark),
                            modifier = Modifier.align(Alignment.CenterHorizontally)
                        ) {
                            Text("Reintentar")
                        }
                    } else if (activeInventarios.isEmpty()) {
                        Text(
                            text = "No hay inventarios activos. Creá uno en el panel del POS central.",
                            color = FluentLightGray,
                            fontSize = 14.sp,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth().padding(top = 40.dp)
                        )
                    } else {
                        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(activeInventarios) { inv ->
                                Card(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .clickable { selectedInv = inv },
                                    colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                                    shape = RoundedCornerShape(8.dp)
                                ) {
                                    Row(
                                        modifier = Modifier.padding(16.dp),
                                        horizontalArrangement = Arrangement.SpaceBetween,
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        Column(modifier = Modifier.weight(1f)) {
                                            Text(inv.descripcion, fontWeight = FontWeight.Bold, color = FluentWhite, fontSize = 15.sp)
                                            Spacer(modifier = Modifier.height(4.dp))
                                            Text(
                                                "Sucursal: ${inv.sucursalNombre ?: "Central"} · Contados: ${inv.articulosContados}/${inv.totalArticulos}",
                                                color = FluentLightGray,
                                                fontSize = 12.sp
                                            )
                                        }
                                        Text("Seleccionar", color = FluentBlue, fontWeight = FontWeight.SemiBold, fontSize = 13.sp)
                                    }
                                }
                            }
                        }
                    }
                }
            } else {
                // STEP 2: ACTIVE SESSION SCANNING
                Column(modifier = Modifier.fillMaxSize()) {
                    // Quick stats
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                            .padding(10.dp),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text("Sucursal: ${selectedInv!!.sucursalNombre ?: "Central"}", color = FluentLightGray, fontSize = 12.sp)
                        Text("Operación: Conteo Físico", color = FluentBlue, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                    }

                    Spacer(modifier = Modifier.height(16.dp))

                    // Scanner Input Container
                    Text("Escanear Producto", fontSize = 12.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(4.dp))
                    OutlinedTextField(
                        value = barcodeInput,
                        onValueChange = { barcodeInput = it },
                        placeholder = { Text("Escanear código EAN...") },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite,
                            unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue,
                            unfocusedBorderColor = FluentDivider
                        ),
                        trailingIcon = {
                            TextButton(onClick = { performSearch(barcodeInput) }) {
                                Text("Buscar", color = FluentBlue, fontWeight = FontWeight.Bold)
                            }
                        }
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // Alerts or Success messages
                    actionMessage?.let { msg ->
                        Text(
                            text = msg,
                            color = FluentSuccess,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(FluentSuccess.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                                .padding(10.dp),
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(10.dp))
                    }

                    errorMessage?.let { err ->
                        Text(
                            text = err,
                            color = FluentError,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(FluentError.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                                .padding(10.dp),
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(10.dp))
                    }

                    // Scan Hint
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .weight(1f),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "Apuntá y presioná el gatillo de tu Zebra para escanear",
                            color = FluentLightGray,
                            fontSize = 14.sp,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }
    }

    // Modal para ingresar cantidad del artículo escaneado
    if (showQtyDialog && scannedArticle != null) {
        val article = scannedArticle!!
        val focusRequester = remember { FocusRequester() }
        var qtyInput by remember { mutableStateOf(TextFieldValue("1.0", TextRange(0, 3))) }

        LaunchedEffect(Unit) {
            focusRequester.requestFocus()
        }

        Dialog(onDismissRequest = { 
            showQtyDialog = false
            scannedArticle = null 
        }) {
            Card(
                modifier = Modifier
                    .fillMaxWidth(0.95f)
                    .wrapContentHeight(),
                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp)
                ) {
                    Text(
                        text = "Ingresar Conteo",
                        fontWeight = FontWeight.Bold,
                        fontSize = 18.sp,
                        color = FluentWhite
                    )
                    Spacer(modifier = Modifier.height(10.dp))
                    Text(
                        text = article.descripcion,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 15.sp,
                        color = FluentWhite
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = "EAN: ${article.codigoBarras.ifBlank { article.codigoInterno }}",
                        fontSize = 12.sp,
                        color = FluentLightGray
                    )
                    Text(
                        text = "Stock Actual: ${String.format(java.util.Locale.US, "%.2f", article.stockActual)}",
                        fontSize = 12.sp,
                        color = FluentLightGray
                    )
                    Spacer(modifier = Modifier.height(16.dp))

                    Text("Cantidad Contada", fontSize = 12.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(6.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        IconButton(
                            onClick = {
                                val current = qtyInput.text.toDoubleOrNull() ?: 1.0
                                if (current > 0.0) {
                                    val newQty = String.format(java.util.Locale.US, "%.1f", current - 1.0)
                                    qtyInput = TextFieldValue(newQty, TextRange(newQty.length))
                                }
                            },
                            modifier = Modifier
                                .size(46.dp)
                                .background(FluentDivider, shape = RoundedCornerShape(4.dp))
                        ) {
                            Text("-", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                        }

                        OutlinedTextField(
                            value = qtyInput,
                            onValueChange = { qtyInput = it },
                            keyboardOptions = KeyboardOptions(
                                keyboardType = KeyboardType.Number,
                                imeAction = ImeAction.Done
                            ),
                            keyboardActions = KeyboardActions(
                                onDone = {
                                    val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                    if (qty > 0.0) {
                                        saveCount(qty, acumulativo = true)
                                        showQtyDialog = false
                                    } else {
                                        errorMessage = "La cantidad debe ser mayor que cero."
                                    }
                                }
                            ),
                            singleLine = true,
                            modifier = Modifier
                                .weight(1f)
                                .focusRequester(focusRequester),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            ),
                            textStyle = LocalTextStyle.current.copy(textAlign = TextAlign.Center)
                        )

                        IconButton(
                            onClick = {
                                val current = qtyInput.text.toDoubleOrNull() ?: 0.0
                                val newQty = String.format(java.util.Locale.US, "%.1f", current + 1.0)
                                qtyInput = TextFieldValue(newQty, TextRange(newQty.length))
                            },
                            modifier = Modifier
                                .size(46.dp)
                                .background(FluentDivider, shape = RoundedCornerShape(4.dp))
                        ) {
                            Text("+", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                        }
                    }

                    Spacer(modifier = Modifier.height(20.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Button(
                            onClick = {
                                showQtyDialog = false
                                scannedArticle = null
                            },
                            modifier = Modifier.weight(0.8f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentDivider, contentColor = FluentWhite),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Cancelar", fontSize = 12.sp)
                        }

                        Button(
                            onClick = {
                                val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                if (qty > 0.0) {
                                    saveCount(qty, acumulativo = true)
                                    showQtyDialog = false
                                } else {
                                    errorMessage = "La cantidad debe ser mayor que cero."
                                }
                            },
                            modifier = Modifier.weight(1.1f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlueDark, contentColor = Color.White),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Sumar", fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }

                        Button(
                            onClick = {
                                val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                if (qty > 0.0) {
                                    saveCount(qty, acumulativo = false)
                                    showQtyDialog = false
                                } else {
                                    errorMessage = "La cantidad debe ser mayor que cero."
                                }
                            },
                            modifier = Modifier.weight(1.1f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Fijar", fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }

    // Modal para crear artículo nuevo si no existe en base de datos
    if (showCreateDialog) {
        NuevoArticuloDialog(
            barcode = pendingBarcode,
            operatorId = operatorId,
            onDismiss = { showCreateDialog = false },
            onSaveSuccess = { newArt ->
                showCreateDialog = false
                scannedArticle = newArt
                countedQty = "1.0"
                showQtyDialog = true
            }
        )
    }
}
