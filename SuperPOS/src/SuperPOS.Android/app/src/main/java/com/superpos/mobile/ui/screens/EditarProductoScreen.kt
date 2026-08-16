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
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EditarProductoScreen(
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
    
    // Editable fields
    var editDescripcion by remember { mutableStateOf("") }
    var editPrecioCosto by remember { mutableStateOf("") }
    var editPrecioVenta by remember { mutableStateOf("") }

    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }

    fun performSearch(barcode: String) {
        if (barcode.isBlank()) return
        isLoading = true
        successMessage = null
        errorMessage = null
        scannedArticle = null
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) { service.getArticleByBarcode(barcode) }
                if (response.isSuccessful && response.body() != null) {
                    val art = response.body()!!
                    scannedArticle = art
                    editDescripcion = art.descripcion
                    editPrecioCosto = art.precioCosto.toString()
                    editPrecioVenta = art.precioVenta.toString()
                } else {
                    errorMessage = "Producto con código $barcode no encontrado."
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

    fun saveChanges() {
        val article = scannedArticle ?: return
        val cost = editPrecioCosto.toDoubleOrNull()
        val price = editPrecioVenta.toDoubleOrNull()

        if (editDescripcion.isBlank()) {
            errorMessage = "La descripción no puede estar vacía."
            return
        }
        if (cost == null || cost < 0.0) {
            errorMessage = "Precio de costo inválido."
            return
        }
        if (price == null || price < 0.0) {
            errorMessage = "Precio de venta inválido."
            return
        }

        isLoading = true
        errorMessage = null
        successMessage = null
        scope.launch {
            try {
                val updatedArticle = article.copy(
                    descripcion = editDescripcion,
                    precioCosto = cost,
                    precioVenta = price
                )
                val service = ApiClient.getService(apiUrl)
                val response = withContext(Dispatchers.IO) {
                    service.updateArticle(id = article.id, article = updatedArticle, idUsuario = operatorId)
                }
                if (response.isSuccessful) {
                    successMessage = "Producto actualizado correctamente."
                    scannedArticle = null
                    barcodeInput = ""
                } else {
                    errorMessage = "Error al actualizar producto: ${response.code()}"
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
                title = { Text("Editar Producto") },
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
            Text("Escanear Código del Producto", fontSize = 12.sp, color = FluentLightGray, modifier = Modifier.fillMaxWidth())
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
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                    shape = RoundedCornerShape(8.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text("EAN: ${scannedArticle!!.codigoBarras.ifBlank { scannedArticle!!.codigoInterno }}", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(12.dp))

                        // Descripcion
                        Text("Descripción", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = editDescripcion,
                            onValueChange = { editDescripcion = it },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        // Precio Costo
                        Text("Precio Costo", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = editPrecioCosto,
                            onValueChange = { editPrecioCosto = it },
                            singleLine = true,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        // Precio Venta
                        Text("Precio Venta", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = editPrecioVenta,
                            onValueChange = { editPrecioVenta = it },
                            singleLine = true,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )

                        Spacer(modifier = Modifier.height(20.dp))

                        Button(
                            onClick = { saveChanges() },
                            modifier = Modifier.fillMaxWidth(),
                            colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = Color.White),
                            shape = RoundedCornerShape(4.dp)
                        ) {
                            Text("Guardar Cambios", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            } else {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(200.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "Escaneá un artículo para ver y editar su descripción, costo y precio de venta.",
                        color = FluentLightGray,
                        fontSize = 14.sp,
                        textAlign = TextAlign.Center
                    )
                }
            }
        }
    }
}
