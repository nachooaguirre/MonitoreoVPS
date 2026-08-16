package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
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
import com.superpos.mobile.models.Sucursal
import com.superpos.mobile.ui.components.NuevoArticuloDialog
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

private val SECTORES_SUGERIDOS = listOf("Panadería", "Carnicería", "Verdulería", "Fiambrería", "Rotisería")

private enum class TipoMovimiento { EGRESO, INGRESO }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EgresosIngresosScreen(
    operatorId: Int,
    onNavigateBack: () -> Unit,
    registerScanCallback: ((String) -> Unit) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    var tipoMovimiento by remember { mutableStateOf(TipoMovimiento.EGRESO) }
    var sector by remember { mutableStateOf("") }

    var barcodeInput by remember { mutableStateOf("") }
    var scannedArticle by remember { mutableStateOf<Article?>(null) }
    var showQtyDialog by remember { mutableStateOf(false) }

    var sucursales by remember { mutableStateOf<List<Sucursal>>(emptyList()) }
    var selectedSucursal by remember { mutableStateOf<Sucursal?>(null) }
    var showSucMenu by remember { mutableStateOf(false) }

    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }

    var showCreateDialog by remember { mutableStateOf(false) }
    var pendingBarcode by remember { mutableStateOf("") }

    LaunchedEffect(Unit) {
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getSucursales() }
                if (response.isSuccessful) {
                    sucursales = response.body() ?: emptyList()
                    selectedSucursal = sucursales.find { it.esCentral } ?: sucursales.firstOrNull()
                }
            } catch (e: Exception) {
                errorMessage = "Error al cargar sucursales: ${e.message}"
            }
        }
    }

    fun performSearch(barcode: String) {
        if (barcode.isBlank()) return
        if (sector.isBlank()) {
            errorMessage = "Elegí o escribí el sector de destino antes de escanear."
            return
        }
        isLoading = true
        successMessage = null
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(barcode) }
                if (response.isSuccessful && response.body() != null) {
                    scannedArticle = response.body()
                    showQtyDialog = true
                } else {
                    pendingBarcode = barcode
                    showCreateDialog = true
                }
            } catch (e: Exception) {
                errorMessage = "Error de red: ${e.message}"
            } finally {
                isLoading = false
            }
        }
    }

    LaunchedEffect(Unit) {
        registerScanCallback { code ->
            barcodeInput = code
            performSearch(code)
        }
    }

    fun submitMovimiento(qty: Double) {
        val article = scannedArticle ?: return
        val sucursal = selectedSucursal ?: return

        if (qty <= 0) {
            errorMessage = "La cantidad debe ser mayor que cero."
            return
        }

        val delta = if (tipoMovimiento == TipoMovimiento.EGRESO) -qty else qty
        val motivo = if (tipoMovimiento == TipoMovimiento.EGRESO) "Egreso a $sector" else "Ingreso desde $sector"

        isLoading = true
        errorMessage = null
        successMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) {
                    service.adjustStock(
                        id = article.id,
                        delta = delta,
                        idSucursal = sucursal.id,
                        motivo = motivo,
                        idUsuario = operatorId
                    )
                }
                if (response.isSuccessful) {
                    val verbo = if (tipoMovimiento == TipoMovimiento.EGRESO) "descontaron" else "sumaron"
                    successMessage = "Se $verbo $qty unidades de ${article.descripcion} ($sector)."
                    scannedArticle = null
                    barcodeInput = ""
                } else {
                    errorMessage = response.errorBody()?.string()?.trim('"')
                        ?: "Error al registrar el movimiento: ${response.code()}"
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
                title = { Text("Egresos / Ingresos") },
                navigationIcon = {
                    TextButton(onClick = onNavigateBack) {
                        Text("Volver", color = FluentBlue, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = FluentDarkBg, titleContentColor = FluentWhite)
            )
        },
        containerColor = FluentDarkBg
    ) { paddingValues ->
        Column(
            modifier = modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp),
            verticalArrangement = Arrangement.Top,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Tipo de movimiento
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(FluentDivider.copy(alpha = 0.3f), shape = RoundedCornerShape(8.dp))
                    .padding(4.dp),
                horizontalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                listOf(TipoMovimiento.EGRESO to "Egreso", TipoMovimiento.INGRESO to "Ingreso").forEach { (tipo, label) ->
                    val selected = tipoMovimiento == tipo
                    Box(
                        modifier = Modifier
                            .weight(1f)
                            .clickable { tipoMovimiento = tipo }
                            .background(
                                if (selected) FluentBlue else FluentDarkCard,
                                shape = RoundedCornerShape(6.dp)
                            )
                            .padding(vertical = 10.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            label,
                            color = if (selected) FluentDarkBg else FluentWhite,
                            fontWeight = FontWeight.Bold,
                            fontSize = 14.sp
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                if (tipoMovimiento == TipoMovimiento.EGRESO) "Sector de destino (sale del depósito)" else "Sector de origen (vuelve al depósito)",
                fontSize = 12.sp, color = FluentLightGray, modifier = Modifier.fillMaxWidth()
            )
            Spacer(modifier = Modifier.height(4.dp))
            OutlinedTextField(
                value = sector,
                onValueChange = { sector = it },
                placeholder = { Text("Ej. Panadería") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedTextColor = FluentWhite,
                    unfocusedTextColor = FluentWhite,
                    focusedBorderColor = FluentBlue,
                    unfocusedBorderColor = FluentDivider
                )
            )
            Spacer(modifier = Modifier.height(8.dp))
            LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                items(SECTORES_SUGERIDOS) { s ->
                    AssistChip(
                        onClick = { sector = s },
                        label = { Text(s, fontSize = 12.sp) }
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text("Sucursal / Depósito", fontSize = 12.sp, color = FluentLightGray, modifier = Modifier.fillMaxWidth())
            Spacer(modifier = Modifier.height(4.dp))
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(50.dp)
                    .background(FluentDivider.copy(alpha = 0.5f), shape = RoundedCornerShape(4.dp))
                    .clickable { showSucMenu = true }
                    .padding(horizontal = 12.dp, vertical = 14.dp)
            ) {
                Text(
                    text = selectedSucursal?.nombre ?: "Seleccionar sucursal...",
                    color = FluentWhite,
                    fontSize = 14.sp
                )
                DropdownMenu(
                    expanded = showSucMenu,
                    onDismissRequest = { showSucMenu = false },
                    modifier = Modifier.fillMaxWidth(0.9f).background(FluentDarkCard)
                ) {
                    sucursales.forEach { suc ->
                        DropdownMenuItem(
                            text = { Text(suc.nombre, color = FluentWhite) },
                            onClick = {
                                selectedSucursal = suc
                                showSucMenu = false
                            }
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text("Escanear Producto", fontSize = 12.sp, color = FluentLightGray, modifier = Modifier.fillMaxWidth())
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

            successMessage?.let { msg ->
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
                Spacer(modifier = Modifier.height(12.dp))
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
                Spacer(modifier = Modifier.height(12.dp))
            }

            Box(
                modifier = Modifier.fillMaxWidth().height(150.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "Elegí el sector, escaneá el producto y confirmá la cantidad que sale (o vuelve) del depósito.",
                    color = FluentLightGray,
                    fontSize = 14.sp,
                    textAlign = TextAlign.Center
                )
            }
        }
    }

    // Modal para ingresar cantidad
    if (showQtyDialog && scannedArticle != null) {
        val article = scannedArticle!!
        val focusRequester = remember { FocusRequester() }
        var qtyInput by remember { mutableStateOf(TextFieldValue("1.0", TextRange(0, 3))) }

        LaunchedEffect(Unit) { focusRequester.requestFocus() }

        Dialog(onDismissRequest = {
            showQtyDialog = false
            scannedArticle = null
        }) {
            Card(
                modifier = Modifier.fillMaxWidth(0.95f).wrapContentHeight(),
                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(modifier = Modifier.fillMaxWidth().padding(16.dp)) {
                    Text(
                        text = if (tipoMovimiento == TipoMovimiento.EGRESO) "Egreso a $sector" else "Ingreso desde $sector",
                        fontWeight = FontWeight.Bold,
                        fontSize = 18.sp,
                        color = FluentWhite
                    )
                    Spacer(modifier = Modifier.height(10.dp))
                    Text(article.descripcion, fontWeight = FontWeight.SemiBold, fontSize = 15.sp, color = FluentWhite)
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = "EAN: ${article.codigoBarras.ifBlank { article.codigoInterno }}",
                        fontSize = 12.sp, color = FluentLightGray
                    )
                    Text(
                        text = "Stock Actual: ${String.format(java.util.Locale.US, "%.2f", article.stockActual)}",
                        fontSize = 12.sp, color = FluentLightGray
                    )
                    Spacer(modifier = Modifier.height(16.dp))

                    Text("Cantidad", fontSize = 12.sp, color = FluentLightGray)
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
                            modifier = Modifier.size(46.dp).background(FluentDivider, shape = RoundedCornerShape(4.dp))
                        ) {
                            Text("-", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                        }

                        OutlinedTextField(
                            value = qtyInput,
                            onValueChange = { qtyInput = it },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number, imeAction = ImeAction.Done),
                            keyboardActions = KeyboardActions(
                                onDone = {
                                    val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                    if (qty > 0.0) {
                                        submitMovimiento(qty)
                                        showQtyDialog = false
                                    } else {
                                        errorMessage = "La cantidad debe ser mayor que cero."
                                    }
                                }
                            ),
                            singleLine = true,
                            modifier = Modifier.weight(1f).focusRequester(focusRequester),
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
                            modifier = Modifier.size(46.dp).background(FluentDivider, shape = RoundedCornerShape(4.dp))
                        ) {
                            Text("+", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                        }
                    }

                    Spacer(modifier = Modifier.height(20.dp))

                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                        Button(
                            onClick = { showQtyDialog = false; scannedArticle = null },
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentDivider, contentColor = FluentWhite),
                            shape = RoundedCornerShape(4.dp)
                        ) { Text("Cancelar") }

                        Button(
                            onClick = {
                                val qty = qtyInput.text.toDoubleOrNull() ?: 0.0
                                if (qty > 0.0) {
                                    submitMovimiento(qty)
                                    showQtyDialog = false
                                } else {
                                    errorMessage = "La cantidad debe ser mayor que cero."
                                }
                            },
                            modifier = Modifier.weight(1.5f),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                            shape = RoundedCornerShape(4.dp)
                        ) { Text("Confirmar", fontWeight = FontWeight.Bold) }
                    }
                }
            }
        }
    }

    if (showCreateDialog) {
        NuevoArticuloDialog(
            barcode = pendingBarcode,
            operatorId = operatorId,
            onDismiss = { showCreateDialog = false },
            onSaveSuccess = { newArt ->
                showCreateDialog = false
                scannedArticle = newArt
                showQtyDialog = true
            }
        )
    }
}
