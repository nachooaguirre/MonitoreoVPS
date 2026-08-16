package com.superpos.mobile.ui.components

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.models.*
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NuevoArticuloDialog(
    barcode: String,
    operatorId: Int,
    onDismiss: () -> Unit,
    onSaveSuccess: (Article) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL }
    val scope = rememberCoroutineScope()

    // Form fields
    var descripcion by remember { mutableStateOf("") }
    var precioCosto by remember { mutableStateOf("0.00") }
    var precioVenta by remember { mutableStateOf("0.00") }
    var stockInicial by remember { mutableStateOf("1.00") }

    // Selectors state
    var selectedProveedor by remember { mutableStateOf<Proveedor?>(null) }
    var selectedDepto by remember { mutableStateOf<Departamento?>(null) }
    var selectedFamilia by remember { mutableStateOf<Familia?>(null) }
    var selectedMarca by remember { mutableStateOf<Marca?>(null) }

    // Lists loaded from API
    var proveedores by remember { mutableStateOf<List<Proveedor>>(emptyList()) }
    var departamentos by remember { mutableStateOf<List<Departamento>>(emptyList()) }
    var familias by remember { mutableStateOf<List<Familia>>(emptyList()) }
    var marcas by remember { mutableStateOf<List<Marca>>(emptyList()) }

    // Dropdown search queries
    var provQuery by remember { mutableStateOf("") }
    var deptoQuery by remember { mutableStateOf("") }
    var marcaQuery by remember { mutableStateOf("") }

    // Search dialog visibility
    var showProvSearch by remember { mutableStateOf(false) }
    var showDeptoSearch by remember { mutableStateOf(false) }
    var showMarcaSearch by remember { mutableStateOf(false) }

    var isSaving by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    // Load initial data
    LaunchedEffect(Unit) {
        scope.launch {
            try {
                val service = ApiClient.getService(apiUrl)
                // Load suppliers
                val resProv = withContext(Dispatchers.IO) { service.getProveedores() }
                if (resProv.isSuccessful) {
                    proveedores = resProv.body()?.items ?: emptyList()
                }
                // Load departments
                val resDepto = withContext(Dispatchers.IO) { service.getDepartamentos() }
                if (resDepto.isSuccessful) {
                    departamentos = resDepto.body() ?: emptyList()
                }
                // Load brands
                val resMarca = withContext(Dispatchers.IO) { service.getMarcas() }
                if (resMarca.isSuccessful) {
                    marcas = resMarca.body() ?: emptyList()
                }
            } catch (e: Exception) {
                errorMessage = "Error al cargar listas: ${e.message}"
            }
        }
    }

    // Load families when department changes
    LaunchedEffect(selectedDepto) {
        selectedDepto?.let { depto ->
            try {
                val service = ApiClient.getService(apiUrl)
                val resFam = withContext(Dispatchers.IO) { service.getFamilias(depto.id) }
                if (resFam.isSuccessful) {
                    familias = resFam.body() ?: emptyList()
                }
            } catch (e: Exception) {
                errorMessage = "Error al cargar familias: ${e.message}"
            }
        } ?: run {
            familias = emptyList()
            selectedFamilia = null
        }
    }

    Dialog(
        onDismissRequest = { if (!isSaving) onDismiss() },
        properties = DialogProperties(usePlatformDefaultWidth = false)
    ) {
        Surface(
            modifier = modifier
                .fillMaxSize()
                .background(FluentDarkBg),
            color = FluentDarkBg
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp)
            ) {
                // Header
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "Crear Nuevo Artículo",
                        color = FluentWhite,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold
                    )
                    IconButton(onClick = onDismiss, enabled = !isSaving) {
                        Text("✕", color = FluentLightGray, fontSize = 20.sp)
                    }
                }

                Divider(color = FluentDivider, modifier = Modifier.padding(vertical = 8.dp))

                // Error alert
                errorMessage?.let { error ->
                    Text(
                        text = error,
                        color = FluentError,
                        fontSize = 13.sp,
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(FluentError.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                            .padding(8.dp)
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                }

                // Scrollable Form
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(rememberScrollState())
                ) {
                    // EAN Code (Read Only)
                    Text("Código de Barras", fontSize = 11.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(2.dp))
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                            .border(1.dp, FluentDivider, shape = RoundedCornerShape(4.dp))
                            .padding(12.dp)
                    ) {
                        Text(barcode, color = FluentLightGray, fontSize = 14.sp)
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Description (Required)
                    OutlinedTextField(
                        value = descripcion,
                        onValueChange = { descripcion = it },
                        label = { Text("Descripción *") },
                        placeholder = { Text("Ej. YERBA AMANDA 1KG") },
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

                    // Row: Cost & Sale Prices
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        OutlinedTextField(
                            value = precioCosto,
                            onValueChange = { precioCosto = it },
                            label = { Text("Costo ($)") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )
                        OutlinedTextField(
                            value = precioVenta,
                            onValueChange = { precioVenta = it },
                            label = { Text("Venta ($)") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Row: Initial Stock
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        OutlinedTextField(
                            value = stockInicial,
                            onValueChange = { stockInicial = it },
                            label = { Text("Stock Inicial") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = FluentWhite,
                                unfocusedTextColor = FluentWhite,
                                focusedBorderColor = FluentBlue,
                                unfocusedBorderColor = FluentDivider
                            )
                        )
                        
                        // Selector: Proveedor (with popup search)
                        Column(modifier = Modifier.weight(1f)) {
                            Text("Proveedor *", fontSize = 11.sp, color = FluentLightGray)
                            Spacer(modifier = Modifier.height(2.dp))
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(56.dp)
                                    .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                                    .border(1.dp, FluentDivider, shape = RoundedCornerShape(4.dp))
                                    .clickable { showProvSearch = true }
                                    .padding(horizontal = 12.dp, vertical = 16.dp)
                            ) {
                                Text(
                                    text = selectedProveedor?.razonSocial ?: "Seleccionar...",
                                    color = if (selectedProveedor != null) FluentWhite else FluentLightGray,
                                    fontSize = 14.sp,
                                    maxLines = 1
                                )
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Selector: Departamento
                    Text("Departamento *", fontSize = 11.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(2.dp))
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp)
                            .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                            .border(1.dp, FluentDivider, shape = RoundedCornerShape(4.dp))
                            .clickable { showDeptoSearch = true }
                            .padding(horizontal = 12.dp, vertical = 16.dp)
                    ) {
                        Text(
                            text = selectedDepto?.nombre ?: "Seleccionar...",
                            color = if (selectedDepto != null) FluentWhite else FluentLightGray,
                            fontSize = 14.sp
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Selector: Familia (depends on department)
                    Text("Familia *", fontSize = 11.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(2.dp))
                    var showFamMenu by remember { mutableStateOf(false) }
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp)
                            .background(
                                if (selectedDepto == null) FluentDarkBg else FluentDarkCard, 
                                shape = RoundedCornerShape(4.dp)
                            )
                            .border(1.dp, FluentDivider, shape = RoundedCornerShape(4.dp))
                            .clickable(enabled = selectedDepto != null) { showFamMenu = true }
                            .padding(horizontal = 12.dp, vertical = 16.dp)
                    ) {
                        Text(
                            text = if (selectedDepto == null) "Seleccioná depto primero..." else (selectedFamilia?.nombre ?: "Seleccionar..."),
                            color = if (selectedFamilia != null) FluentWhite else FluentLightGray,
                            fontSize = 14.sp
                        )
                        DropdownMenu(
                            expanded = showFamMenu,
                            onDismissRequest = { showFamMenu = false },
                            modifier = Modifier
                                .fillMaxWidth(0.9f)
                                .background(FluentDarkCard)
                        ) {
                            familias.forEach { fam ->
                                DropdownMenuItem(
                                    text = { Text(fam.nombre, color = FluentWhite) },
                                    onClick = {
                                        selectedFamilia = fam
                                        showFamMenu = false
                                    }
                                )
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Selector: Marca
                    Text("Marca *", fontSize = 11.sp, color = FluentLightGray)
                    Spacer(modifier = Modifier.height(2.dp))
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp)
                            .background(FluentDarkCard, shape = RoundedCornerShape(4.dp))
                            .border(1.dp, FluentDivider, shape = RoundedCornerShape(4.dp))
                            .clickable { showMarcaSearch = true }
                            .padding(horizontal = 12.dp, vertical = 16.dp)
                    ) {
                        Text(
                            text = selectedMarca?.nombre ?: "Seleccionar...",
                            color = if (selectedMarca != null) FluentWhite else FluentLightGray,
                            fontSize = 14.sp
                        )
                    }

                    Spacer(modifier = Modifier.height(20.dp))
                }

                // Footer Buttons
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Button(
                        onClick = onDismiss,
                        modifier = Modifier.weight(1f),
                        colors = ButtonDefaults.buttonColors(containerColor = FluentDarkCard, contentColor = FluentWhite),
                        shape = RoundedCornerShape(8.dp),
                        enabled = !isSaving
                    ) {
                        Text("Cancelar")
                    }

                    Button(
                        onClick = {
                            if (descripcion.isBlank() || selectedProveedor == null || selectedDepto == null || selectedFamilia == null || selectedMarca == null) {
                                errorMessage = "Por favor, completa todos los campos requeridos (*)."
                                return@Button
                            }
                            isSaving = true
                            errorMessage = null
                            scope.launch {
                                try {
                                    val service = ApiClient.getService(apiUrl)
                                    val req = ArticleCreateRequest(
                                        codigoBarras = barcode,
                                        codigoInterno = barcode,
                                        descripcion = descripcion.trim().uppercase(),
                                        descripcionCorta = descripcion.trim().take(20).uppercase(),
                                        precioCosto = precioCosto.toDoubleOrNull() ?: 0.0,
                                        precioVenta = precioVenta.toDoubleOrNull() ?: 0.0,
                                        stockActual = stockInicial.toDoubleOrNull() ?: 1.0,
                                        idProveedor = selectedProveedor!!.id,
                                        idDepartamento = selectedDepto!!.id,
                                        idFamilia = selectedFamilia!!.id,
                                        idMarca = selectedMarca!!.id
                                    )
                                    val response = withContext(Dispatchers.IO) { service.createArticle(req, idUsuario = operatorId) }
                                    if (response.isSuccessful && response.body() != null) {
                                        onSaveSuccess(response.body()!!)
                                    } else {
                                        errorMessage = response.errorBody()?.string() ?: "Error al guardar el artículo"
                                        isSaving = false
                                    }
                                } catch (e: Exception) {
                                    errorMessage = "Error de red: ${e.message}"
                                    isSaving = false
                                }
                            }
                        },
                        modifier = Modifier.weight(1.5f),
                        colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                        shape = RoundedCornerShape(8.dp),
                        enabled = !isSaving
                    ) {
                        if (isSaving) {
                            CircularProgressIndicator(modifier = Modifier.size(18.dp), color = FluentDarkBg)
                        } else {
                            Text("Crear Artículo", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }

    // Modal Search: Proveedor
    if (showProvSearch) {
        SearchSelectorModal(
            title = "Seleccionar Proveedor",
            query = provQuery,
            onQueryChange = { provQuery = it },
            items = proveedores.filter { it.razonSocial.contains(provQuery, ignoreCase = true) },
            itemLabel = { it.razonSocial },
            onSelect = {
                selectedProveedor = it
                showProvSearch = false
            },
            onDismiss = { showProvSearch = false }
        )
    }

    // Modal Search: Departamento
    if (showDeptoSearch) {
        SearchSelectorModal(
            title = "Seleccionar Departamento",
            query = deptoQuery,
            onQueryChange = { deptoQuery = it },
            items = departamentos.filter { it.nombre.contains(deptoQuery, ignoreCase = true) },
            itemLabel = { it.nombre },
            onSelect = {
                selectedDepto = it
                selectedFamilia = null // reset family
                showDeptoSearch = false
            },
            onDismiss = { showDeptoSearch = false }
        )
    }

    // Modal Search: Marca
    if (showMarcaSearch) {
        SearchSelectorModal(
            title = "Seleccionar Marca",
            query = marcaQuery,
            onQueryChange = { marcaQuery = it },
            items = marcas.filter { it.nombre.contains(marcaQuery, ignoreCase = true) },
            itemLabel = { it.nombre },
            onSelect = {
                selectedMarca = it
                showMarcaSearch = false
            },
            onDismiss = { showMarcaSearch = false }
        )
    }
}

@Composable
fun <T> SearchSelectorModal(
    title: String,
    query: String,
    onQueryChange: (String) -> Unit,
    items: List<T>,
    itemLabel: (T) -> String,
    onSelect: (T) -> Unit,
    onDismiss: () -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth(0.95f)
                .fillMaxHeight(0.85f),
            colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
            shape = RoundedCornerShape(12.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp)
            ) {
                Text(title, fontWeight = FontWeight.Bold, fontSize = 16.sp, color = FluentWhite)
                
                Spacer(modifier = Modifier.height(10.dp))

                OutlinedTextField(
                    value = query,
                    onValueChange = onQueryChange,
                    placeholder = { Text("Buscar...") },
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

                LazyColumn(
                    modifier = Modifier.weight(1f)
                ) {
                    items(items) { item ->
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onSelect(item) }
                                .padding(vertical = 12.dp, horizontal = 4.dp)
                        ) {
                            Text(
                                text = itemLabel(item),
                                color = FluentWhite,
                                fontSize = 14.sp
                            )
                        }
                        Divider(color = FluentDivider.copy(alpha = 0.5f))
                    }
                }

                Spacer(modifier = Modifier.height(10.dp))

                Button(
                    onClick = onDismiss,
                    modifier = Modifier.align(Alignment.End),
                    colors = ButtonDefaults.buttonColors(containerColor = FluentDivider, contentColor = FluentWhite),
                    shape = RoundedCornerShape(4.dp)
                ) {
                    Text("Cerrar")
                }
            }
        }
    }
}
