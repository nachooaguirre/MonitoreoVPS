package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MenuScreen(
    onNavigateToInventario: () -> Unit,
    onNavigateToRecepcion: () -> Unit,
    onNavigateToEtiquetas: () -> Unit,
    onNavigateToStock: () -> Unit,
    onNavigateToConfig: () -> Unit,
    onNavigateToEditarProducto: () -> Unit,
    onNavigateToEgresosIngresos: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    val apiUrl = remember { mutableStateOf("") }

    var isOnline by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    // Cargar url actual y consultar salud de la api periódicamente
    LaunchedEffect(Unit) {
        while (true) {
            apiUrl.value = sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL
            try {
                val service = ApiClient.getService(apiUrl.value)
                val response = withContext(Dispatchers.IO) { service.checkHealth() }
                isOnline = response.isSuccessful
            } catch (e: Exception) {
                isOnline = false
            }
            delay(10000) // Verificar cada 10 segundos
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween,
                        modifier = Modifier.fillMaxWidth().padding(end = 8.dp)
                    ) {
                        Text("SuperPOS Mobile", fontWeight = FontWeight.Bold, fontSize = 20.sp)
                        
                        // Badge de Conexión
                        Box(
                            modifier = Modifier
                                .background(
                                    if (isOnline) FluentStatusOnline.copy(alpha = 0.2f) else FluentStatusOffline.copy(alpha = 0.2f),
                                    shape = RoundedCornerShape(12.dp)
                                )
                                .padding(horizontal = 10.dp, vertical = 4.dp)
                        ) {
                            Text(
                                text = if (isOnline) "Conectado" else "Sin Conexión",
                                color = if (isOnline) FluentSuccess else FluentError,
                                fontSize = 11.sp,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                },
                actions = {
                    TextButton(onClick = onNavigateToConfig) {
                        Text("Configurar", color = FluentBlue, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = FluentDarkBg,
                    titleContentColor = FluentWhite
                )
            )
        },
        containerColor = FluentDarkBg
    ) { paddingValues ->
        Column(
            modifier = modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(horizontal = 16.dp, vertical = 8.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(8.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "Terminal de Depósito y Salón",
                color = FluentWhite,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.fillMaxWidth()
            )
            Text(
                text = "Seleccioná la operación a realizar con tu terminal Zebra",
                color = FluentLightGray,
                fontSize = 13.sp,
                modifier = Modifier.fillMaxWidth().padding(top = 2.dp)
            )

            Spacer(modifier = Modifier.height(4.dp))

            MenuCard(
                title = "Conteo de Stock",
                desc = "Registrar existencias de salón o depósito",
                onClick = onNavigateToInventario
            )

            MenuCard(
                title = "Recepción Camión",
                desc = "Controlar remitos contra orden de compra",
                onClick = onNavigateToRecepcion
            )

            MenuCard(
                title = "Encolar Etiquetas",
                desc = "Escanear para imprimir etiquetas de góndola",
                onClick = onNavigateToEtiquetas
            )

            MenuCard(
                title = "Agregar al Stock",
                desc = "Incrementar existencias directas de producto",
                onClick = onNavigateToStock
            )

            MenuCard(
                title = "Editar Producto",
                desc = "Escanear código y modificar descripción, costo y venta",
                onClick = onNavigateToEditarProducto
            )

            MenuCard(
                title = "Egresos / Ingresos",
                desc = "Mover mercadería del depósito a panadería, carnicería, etc.",
                onClick = onNavigateToEgresosIngresos
            )
        }
    }
}

@Composable
fun MenuCard(
    title: String,
    desc: String,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .height(80.dp)
            .clickable { onClick() },
        colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
        shape = RoundedCornerShape(8.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.Start
        ) {
            Text(
                text = title,
                fontSize = 15.sp,
                fontWeight = FontWeight.Bold,
                color = FluentWhite
            )
            Spacer(modifier = Modifier.height(2.dp))
            Text(
                text = desc,
                fontSize = 12.sp,
                lineHeight = 14.sp,
                color = FluentLightGray
            )
        }
    }
}
