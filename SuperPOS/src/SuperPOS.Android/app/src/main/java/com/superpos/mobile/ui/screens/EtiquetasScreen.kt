package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.models.Article
import com.superpos.mobile.models.EtiquetaEncolarRequest
import com.superpos.mobile.ui.components.NuevoArticuloDialog
import com.superpos.mobile.ui.components.TicketPreview
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EtiquetasScreen(
    operatorId: Int,
    onNavigateBack: () -> Unit,
    registerScanCallback: ((String) -> Unit) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    var barcodeInput by remember { mutableStateOf("") }
    var scannedArticle by remember { mutableStateOf<Article?>(null) }
    var labelQty by remember { mutableStateOf("1") }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }

    // Dialog state
    var showCreateDialog by remember { mutableStateOf(false) }
    var pendingBarcode by remember { mutableStateOf("") }

    // Search article logic
    fun performSearch(barcode: String) {
        if (barcode.isBlank()) return
        isLoading = true
        successMessage = null
        errorMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(barcode) }
                if (response.isSuccessful && response.body() != null) {
                    scannedArticle = response.body()
                    labelQty = "1"
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

    // Register broadcast laser scanner action callback
    LaunchedEffect(Unit) {
        registerScanCallback { code ->
            barcodeInput = code
            performSearch(code)
        }
    }

    // Enqueue printing queue API helper
    fun queueLabel() {
        val article = scannedArticle ?: return
        val qty = labelQty.toIntOrNull() ?: 1
        if (qty < 1) {
            errorMessage = "La cantidad debe ser mayor que cero."
            return
        }

        isLoading = true
        errorMessage = null
        successMessage = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val req = EtiquetaEncolarRequest(idArticulo = article.id, cantidad = qty)
                val response = withContext(Dispatchers.IO) { service.enqueueLabel(req) }
                if (response.isSuccessful) {
                    successMessage = "Etiqueta encolada: ${article.descripcion} (x$qty)"
                    scannedArticle = null
                    barcodeInput = ""
                } else {
                    errorMessage = "Error al encolar: ${response.code()}"
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
                title = { Text("Encolar Etiquetas") },
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
                .padding(16.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.Top,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Scanner input
            Text("Escanear Artículo para Góndola", fontSize = 12.sp, color = FluentLightGray, modifier = Modifier.fillMaxWidth())
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

            // Notifications
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

            if (scannedArticle != null) {
                // High-fidelity ticket preview header
                Text(
                    text = "VISTA PREVIA DE ETIQUETA",
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Bold,
                    color = FluentLightGray,
                    modifier = Modifier.fillMaxWidth()
                )
                
                Spacer(modifier = Modifier.height(8.dp))

                // Embedded Label Preview Component
                TicketPreview(
                    article = scannedArticle!!,
                    modifier = Modifier
                        .fillMaxWidth(0.9f)
                        .padding(vertical = 8.dp)
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Quantity selector
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                    shape = RoundedCornerShape(8.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text("Cantidad de Etiquetas", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(6.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            IconButton(
                                onClick = {
                                    val current = labelQty.toIntOrNull() ?: 1
                                    if (current > 1) {
                                        labelQty = (current - 1).toString()
                                    }
                                },
                                modifier = Modifier
                                    .size(46.dp)
                                    .background(FluentDivider, shape = RoundedCornerShape(4.dp))
                            ) {
                                Text("-", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                            }

                            OutlinedTextField(
                                value = labelQty,
                                onValueChange = { labelQty = it },
                                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                singleLine = true,
                                modifier = Modifier.weight(1f),
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
                                    val current = labelQty.toIntOrNull() ?: 0
                                    labelQty = (current + 1).toString()
                                },
                                modifier = Modifier
                                    .size(46.dp)
                                    .background(FluentDivider, shape = RoundedCornerShape(4.dp))
                            ) {
                                Text("+", color = FluentWhite, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                            }
                        }

                        Spacer(modifier = Modifier.height(16.dp))

                        Button(
                            onClick = { queueLabel() },
                            modifier = Modifier.fillMaxWidth(),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Agregar a Cola de Impresión", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            } else {
                // Hint message
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(300.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "Escaneá artículos en la góndola para encolar sus etiquetas si el precio cambió o la etiqueta está dañada.",
                        color = FluentLightGray,
                        fontSize = 14.sp,
                        textAlign = TextAlign.Center
                    )
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
                labelQty = "1"
            }
        )
    }
}
