package com.superpos.mobile.ui.screens

import android.app.AlertDialog
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
import com.superpos.mobile.models.*
import com.superpos.mobile.ui.components.NuevoArticuloDialog
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RecepcionScreen(
    operatorId: Int, // Active logged-in user
    onNavigateBack: () -> Unit,
    registerScanCallback: ((String) -> Unit) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    var openOcs by remember { mutableStateOf<List<OrdenCompra>>(emptyList()) }
    var selectedOc by remember { mutableStateOf<OrdenCompra?>(null) }
    var ocItems = remember { mutableStateListOf<LocalOcItem>() }
    var barcodeInput by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var toastMessage by remember { mutableStateOf<String?>(null) }

    // Dialog flags
    var showCreateDialog by remember { mutableStateOf(false) }
    var pendingBarcode by remember { mutableStateOf("") }

    // Dialog Quantity states
    var showQtyDialog by remember { mutableStateOf(false) }
    var dialogArticle by remember { mutableStateOf<Article?>(null) }
    var dialogOcItem by remember { mutableStateOf<LocalOcItem?>(null) }

    fun loadOpenOcs() {
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getOrdenesCompra() }
                if (response.isSuccessful) {
                    val all = response.body() ?: emptyList()
                    openOcs = all.filter { it.estado == 0 || it.estado == 1 || it.estado == 2 }
                } else {
                    errorMessage = "Error al cargar órdenes de compra: ${response.code()}"
                }
            } catch (e: Exception) {
                errorMessage = "Fallo de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    LaunchedEffect(Unit) {
        loadOpenOcs()
    }

    fun loadOcDetail(ocId: Int) {
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getOrdenCompraDetail(ocId) }
                if (response.isSuccessful && response.body() != null) {
                    val detail = response.body()!!
                    selectedOc = detail
                    ocItems.clear()
                    detail.detalles.forEach { d ->
                        ocItems.add(
                            LocalOcItem(
                                idArticulo = d.idArticulo,
                                descripcion = d.articulo?.descripcion ?: "Artículo desconocido",
                                codigoBarras = d.articulo?.codigoBarras,
                                cantidadPedida = d.cantidadPedida,
                                cantidadRecibida = d.cantidadRecibida,
                                precioCosto = d.precioCosto,
                                observacionDiferencia = d.observacionDiferencia
                            )
                        )
                    }
                } else {
                    errorMessage = "Error al cargar detalle: ${response.code()}"
                }
            } catch (e: Exception) {
                errorMessage = "Fallo de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    // Scan handler for checking in items
    fun handleBarcodeScan(barcode: String) {
        if (selectedOc == null) return
        toastMessage = null
        errorMessage = null

        val index = ocItems.indexOfFirst { it.codigoBarras == barcode }
        if (index != -1) {
            val item = ocItems[index]
            dialogOcItem = item
            dialogArticle = null
            showQtyDialog = true
        } else {
            isLoading = true
            scope.launch {
                try {
                    val service = ApiClient.getService(apiUrl)
                    val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(barcode) }
                    if (response.isSuccessful && response.body() != null) {
                        val art = response.body()!!
                        dialogArticle = art
                        dialogOcItem = null
                        showQtyDialog = true
                    } else {
                        pendingBarcode = barcode
                        showCreateDialog = true
                    }
                } catch (e: Exception) {
                    errorMessage = "Error al buscar: ${e.message}"
                } finally {
                    isLoading = false
                }
            }
        }
    }

    LaunchedEffect(selectedOc) {
        if (selectedOc != null) {
            registerScanCallback { code ->
                handleBarcodeScan(code)
            }
        }
    }

    fun recordReceipt(item: LocalOcItem, qty: Double) {
        val index = ocItems.indexOfFirst { it.idArticulo == item.idArticulo }
        if (index != -1) {
            val updatedItem = item.copy(cantidadRecibida = item.cantidadRecibida + qty)
            ocItems[index] = updatedItem
            toastMessage = "+${qty} ${updatedItem.descripcion} (${updatedItem.cantidadRecibida}/${updatedItem.cantidadPedida})"
        }
    }

    fun recordExcessReceipt(art: Article, qty: Double) {
        val index = ocItems.indexOfFirst { it.idArticulo == art.id }
        if (index != -1) {
            val item = ocItems[index]
            val updatedItem = item.copy(cantidadRecibida = item.cantidadRecibida + qty)
            ocItems[index] = updatedItem
            toastMessage = "Excedente: +${qty} ${updatedItem.descripcion}"
        } else {
            ocItems.add(
                LocalOcItem(
                    idArticulo = art.id,
                    descripcion = art.descripcion,
                    codigoBarras = art.codigoBarras,
                    cantidadPedida = 0.0,
                    cantidadRecibida = qty,
                    precioCosto = art.precioCosto
                )
            )
            toastMessage = "Excedente: +${qty} ${art.descripcion}"
        }
    }

    fun submitReceipt() {
        val oc = selectedOc ?: return
        isLoading = true
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val reqItems = ocItems.map {
                    OrdenCompraRecepcionItem(
                        idArticulo = it.idArticulo,
                        cantidadRecibida = it.cantidadRecibida,
                        precioCosto = it.precioCosto,
                        observacionDiferencia = it.observacionDiferencia
                    )
                }
                val req = OrdenCompraRecepcionRequest(
                    idUsuario = operatorId, // Used logged-in operator
                    idSucursalDestino = null,
                    items = reqItems
                )
                val response = withContext(Dispatchers.IO) { service.receiveOrdenCompra(oc.id, req) }
                if (response.isSuccessful) {
                    selectedOc = null
                    ocItems.clear()
                    // El backend genera un Remito pendiente — el stock recién se actualiza cuando
                    // se confirma desde el WPF en caja, no acá.
                    toastMessage = "Escaneo enviado. Pendiente de revisión y confirmación en caja."
                    loadOpenOcs()
                } else {
                    errorMessage = "Error al enviar el escaneo: ${response.code()}"
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
                title = { Text(if (selectedOc == null) "Recepción de Camión" else "OC #${selectedOc!!.nroOrden}") },
                navigationIcon = {
                    TextButton(onClick = {
                        if (selectedOc != null) {
                            selectedOc = null
                            ocItems.clear()
                            loadOpenOcs()
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
            if (selectedOc == null) {
                // STEP 1: SELECT PENDING PURCHASE ORDER
                Column(modifier = Modifier.fillMaxSize()) {
                    Text("Seleccioná la Orden de Compra", color = FluentWhite, fontSize = 16.sp, fontWeight = FontWeight.Bold)
                    Spacer(modifier = Modifier.height(12.dp))

                    if (isLoading && openOcs.isEmpty()) {
                        CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally), color = FluentBlue)
                    } else if (errorMessage != null && openOcs.isEmpty()) {
                        Text(errorMessage!!, color = FluentError, modifier = Modifier.fillMaxWidth(), textAlign = TextAlign.Center)
                        Spacer(modifier = Modifier.height(10.dp))
                        Button(onClick = { loadOpenOcs() }, modifier = Modifier.align(Alignment.CenterHorizontally)) {
                            Text("Reintentar")
                        }
                    } else if (openOcs.isEmpty()) {
                        Text(
                            text = "No hay órdenes de compra pendientes para recibir.",
                            color = FluentLightGray,
                            fontSize = 14.sp,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth().padding(top = 40.dp)
                        )
                    } else {
                        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(openOcs) { oc ->
                                Card(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .clickable { loadOcDetail(oc.id) },
                                    colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                                    shape = RoundedCornerShape(8.dp)
                                ) {
                                    Row(
                                        modifier = Modifier.padding(16.dp),
                                        horizontalArrangement = Arrangement.SpaceBetween,
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        Column(modifier = Modifier.weight(1f)) {
                                            Text("OC #${oc.nroOrden} · ${oc.proveedorNombre ?: "Sin Proveedor"}", fontWeight = FontWeight.Bold, color = FluentWhite, fontSize = 15.sp)
                                            Spacer(modifier = Modifier.height(4.dp))
                                            Text("Fecha: ${oc.fecha.take(10)} · Total: $${String.format(java.util.Locale.US, "%.2f", oc.total)}", color = FluentLightGray, fontSize = 12.sp)
                                        }
                                        Text("Seleccionar", color = FluentBlue, fontWeight = FontWeight.SemiBold, fontSize = 13.sp)
                                    }
                                }
                            }
                        }
                    }
                }
            } else {
                // STEP 2: SCANNED LIST & CONTROL FLOW
                Column(modifier = Modifier.fillMaxSize()) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                            .padding(10.dp),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text("Proveedor: ${selectedOc!!.proveedorNombre ?: "Proveedor"}", color = FluentLightGray, fontSize = 12.sp)
                        val okItems = ocItems.count { it.cantidadRecibida == it.cantidadPedida }
                        Text("$okItems/${ocItems.size} ítems", color = FluentBlue, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                    }

                    Spacer(modifier = Modifier.height(10.dp))

                    // Notifications
                    toastMessage?.let { msg ->
                        Text(
                            text = msg,
                            color = FluentSuccess,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(FluentSuccess.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                                .padding(8.dp),
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(8.dp))
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
                                .padding(8.dp),
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                    }

                    // Barcode Search field (icon-less) full width
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text("Escanear Artículo Recibido", fontSize = 12.sp, color = FluentLightGray)
                            Spacer(modifier = Modifier.height(4.dp))
                            OutlinedTextField(
                                value = barcodeInput,
                                onValueChange = { barcodeInput = it },
                                placeholder = { Text("Código EAN...") },
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
                                    TextButton(onClick = {
                                        if (barcodeInput.isNotBlank()) {
                                            handleBarcodeScan(barcodeInput)
                                            barcodeInput = ""
                                        }
                                    }) {
                                        Text("Buscar", color = FluentBlue, fontWeight = FontWeight.Bold)
                                    }
                                }
                            )
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Scanned items details table
                    LazyColumn(
                        modifier = Modifier
                            .weight(1f)
                            .fillMaxWidth(),
                        verticalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        items(ocItems) { item ->
                            val isCompleted = item.cantidadRecibida == item.cantidadPedida
                            val isExceeded = item.cantidadRecibida > item.cantidadPedida
                            val isPartial = item.cantidadRecibida > 0 && !isCompleted && !isExceeded
                            
                            val rowBg = when {
                                isCompleted -> Color(0xFF1B321D)
                                isExceeded -> Color(0xFF381F21)
                                else -> FluentDarkCard
                            }

                            val badgeText = when {
                                isCompleted -> "Completo"
                                isExceeded -> "Excedido"
                                isPartial -> "Parcial"
                                else -> "Pendiente"
                            }
                            
                            val badgeColor = when {
                                isCompleted -> FluentSuccess
                                isExceeded -> FluentError
                                isPartial -> FluentWarning
                                else -> FluentLightGray
                            }

                            Card(
                                modifier = Modifier.fillMaxWidth(),
                                colors = CardDefaults.cardColors(containerColor = rowBg),
                                shape = RoundedCornerShape(6.dp)
                            ) {
                                Column(modifier = Modifier.padding(12.dp)) {
                                    Row(
                                        modifier = Modifier.fillMaxWidth(),
                                        horizontalArrangement = Arrangement.SpaceBetween,
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        Column(modifier = Modifier.weight(1f)) {
                                            Text(item.descripcion, fontWeight = FontWeight.Bold, color = FluentWhite, fontSize = 14.sp)
                                            Spacer(modifier = Modifier.height(2.dp))
                                            Text("EAN: ${item.codigoBarras ?: "S/C"}", color = FluentLightGray, fontSize = 11.sp)
                                        }

                                        Column(horizontalAlignment = Alignment.End) {
                                            Row(verticalAlignment = Alignment.Bottom) {
                                                Text(
                                                    text = String.format(java.util.Locale.US, "%.1f", item.cantidadRecibida),
                                                    fontWeight = FontWeight.Bold,
                                                    color = FluentWhite,
                                                    fontSize = 16.sp
                                                )
                                                Text(
                                                    text = " / ${String.format(java.util.Locale.US, "%.1f", item.cantidadPedida)}",
                                                    color = FluentLightGray,
                                                    fontSize = 12.sp,
                                                    modifier = Modifier.padding(bottom = 1.dp)
                                                )
                                            }
                                            Spacer(modifier = Modifier.height(2.dp))
                                            Box(
                                                modifier = Modifier
                                                    .background(badgeColor.copy(alpha = 0.2f), shape = RoundedCornerShape(4.dp))
                                                    .padding(horizontal = 6.dp, vertical = 2.dp)
                                            ) {
                                                Text(
                                                    text = badgeText,
                                                    color = badgeColor,
                                                    fontSize = 10.sp,
                                                    fontWeight = FontWeight.Bold
                                                )
                                            }
                                        }
                                    }

                                    if (item.cantidadRecibida != item.cantidadPedida) {
                                        Spacer(modifier = Modifier.height(8.dp))
                                        OutlinedTextField(
                                            value = item.observacionDiferencia ?: "",
                                            onValueChange = { newText ->
                                                val idx = ocItems.indexOfFirst { it.idArticulo == item.idArticulo }
                                                if (idx != -1) {
                                                    ocItems[idx] = ocItems[idx].copy(observacionDiferencia = newText)
                                                }
                                            },
                                            placeholder = { Text("Aclarar diferencia (rotos, sobrante, etc.)...", fontSize = 11.sp, color = FluentLightGray) },
                                            singleLine = true,
                                            modifier = Modifier.fillMaxWidth().height(48.dp),
                                            textStyle = LocalTextStyle.current.copy(fontSize = 12.sp),
                                            colors = OutlinedTextFieldDefaults.colors(
                                                focusedTextColor = FluentWhite,
                                                unfocusedTextColor = FluentWhite,
                                                focusedBorderColor = FluentWarning,
                                                unfocusedBorderColor = FluentDivider
                                            )
                                        )
                                    }
                                }
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(10.dp))

                    // Finalize checkin button
                    Button(
                        onClick = { submitReceipt() },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(50.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                        shape = RoundedCornerShape(8.dp)
                    ) {
                        Text("Escaneo Finalizado", fontWeight = FontWeight.Bold)
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
                dialogArticle = newArt
                dialogOcItem = null
                showQtyDialog = true
            }
        )
    }

    // Modal para ingresar cantidad recibida
    if (showQtyDialog && (dialogOcItem != null || dialogArticle != null)) {
        val focusRequester = remember { FocusRequester() }
        var qtyInput by remember { mutableStateOf(TextFieldValue("1.0", TextRange(0, 3))) }

        LaunchedEffect(Unit) {
            focusRequester.requestFocus()
        }

        val isExcess = dialogArticle != null
        val title = if (isExcess) "Artículo Excedente" else "Recibir Artículo"
        val desc = dialogOcItem?.descripcion ?: dialogArticle?.descripcion ?: ""
        val barcode = dialogOcItem?.codigoBarras ?: dialogArticle?.codigoBarras ?: ""

        Dialog(onDismissRequest = { 
            showQtyDialog = false
            dialogOcItem = null
            dialogArticle = null
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
                        text = title,
                        fontWeight = FontWeight.Bold,
                        fontSize = 18.sp,
                        color = if (isExcess) FluentWarning else FluentWhite
                    )
                    Spacer(modifier = Modifier.height(10.dp))
                    
                    if (isExcess) {
                        Text(
                            text = "Este artículo no pertenece a la Orden de Compra. Se registrará como excedente.",
                            color = FluentWarning,
                            fontSize = 13.sp,
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(FluentWarning.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                                .padding(8.dp)
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                    }

                    Text(
                        text = desc,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 15.sp,
                        color = FluentWhite
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = "EAN: $barcode",
                        fontSize = 12.sp,
                        color = FluentLightGray
                    )

                    if (!isExcess && dialogOcItem != null) {
                        val item = dialogOcItem!!
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text("Pedido: ${String.format(java.util.Locale.US, "%.1f", item.cantidadPedida)}", fontSize = 12.sp, color = FluentLightGray)
                            Text("Recibido actual: ${String.format(java.util.Locale.US, "%.1f", item.cantidadRecibida)}", fontSize = 12.sp, color = FluentLightGray)
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    Text("Cantidad a Recibir", fontSize = 12.sp, color = FluentLightGray)
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
                                        if (isExcess) {
                                            recordExcessReceipt(dialogArticle!!, qty)
                                        } else {
                                            recordReceipt(dialogOcItem!!, qty)
                                        }
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
                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        Button(
                            onClick = {
                                showQtyDialog = false
                                dialogOcItem = null
                                dialogArticle = null
                            },
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentDivider, contentColor = FluentWhite),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Cancelar")
                        }

                        Button(
                            onClick = {
                                val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                if (qty > 0.0) {
                                    if (isExcess) {
                                        recordExcessReceipt(dialogArticle!!, qty)
                                    } else {
                                        recordReceipt(dialogOcItem!!, qty)
                                    }
                                    showQtyDialog = false
                                } else {
                                    errorMessage = "La cantidad debe ser mayor que cero."
                                }
                            },
                            modifier = Modifier.weight(1.5f),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (isExcess) FluentWarning else FluentBlue, 
                                contentColor = FluentDarkBg
                            ),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text(if (isExcess) "Recibir Excedente" else "Confirmar", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }
}
