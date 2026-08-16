package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.ui.theme.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ConfigScreen(
    onNavigateBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }

    var apiUrl by remember { mutableStateOf(sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL) }
    var connectionStatus by remember { mutableStateOf("Desconocido") }
    var statusColor by remember { mutableStateOf(FluentDivider) }
    var isLoading by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun verifyConnection(urlToCheck: String) {
        isLoading = true
        connectionStatus = "Probando conexión..."
        statusColor = FluentWarning
        scope.launch {
            try {
                val service = ApiClient.getService(urlToCheck)
                val response = withContext(Dispatchers.IO) { service.checkHealth() }
                if (response.isSuccessful) {
                    connectionStatus = "Conectado correctamente"
                    statusColor = FluentStatusOnline
                } else {
                    connectionStatus = "Error HTTP: ${response.code()}"
                    statusColor = FluentStatusOffline
                }
            } catch (e: Exception) {
                connectionStatus = "Fallo de conexión: ${e.message}"
                statusColor = FluentStatusOffline
            } finally {
                isLoading = false
            }
        }
    }

    // Probar conexión inicial
    LaunchedEffect(Unit) {
        verifyConnection(apiUrl)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Configuración", fontWeight = FontWeight.Bold) },
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
                .padding(16.dp),
            verticalArrangement = Arrangement.Top,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "Conectividad del Servidor",
                color = FluentWhite,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.fillMaxWidth()
            )
            
            Spacer(modifier = Modifier.height(6.dp))
            
            Text(
                text = "Establecé la URL o IP de la computadora donde está corriendo el servidor de SuperPOS.",
                color = FluentLightGray,
                fontSize = 14.sp,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(modifier = Modifier.height(20.dp))

            // Campo de Input URL
            OutlinedTextField(
                value = apiUrl,
                onValueChange = { apiUrl = it },
                label = { Text("URL Base de la API") },
                placeholder = { Text("Ej. http://192.168.1.50:5075/api") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = FluentBlue,
                    unfocusedBorderColor = FluentDivider,
                    focusedTextColor = FluentWhite,
                    unfocusedTextColor = FluentWhite,
                    focusedLabelColor = FluentBlue,
                    unfocusedLabelColor = FluentLightGray
                )
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Tarjeta de estado de conexión
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                shape = RoundedCornerShape(8.dp)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Column {
                        Text("Estado del Servidor", fontSize = 12.sp, color = FluentLightGray)
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = connectionStatus,
                            fontSize = 15.sp,
                            fontWeight = FontWeight.Bold,
                            color = FluentWhite
                        )
                    }

                    // Circulo de color de estado
                    Box(
                        modifier = Modifier
                            .size(16.dp)
                            .background(statusColor, shape = RoundedCornerShape(8.dp))
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Botones de acción
            Button(
                onClick = { verifyConnection(apiUrl) },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = FluentBlueDark, contentColor = Color.White),
                shape = RoundedCornerShape(8.dp),
                enabled = !isLoading
            ) {
                if (isLoading) {
                    CircularProgressIndicator(modifier = Modifier.size(20.dp), color = Color.White)
                } else {
                    Text("Verificar Conexión", fontWeight = FontWeight.Bold)
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Button(
                onClick = {
                    sharedPrefs.edit().putString(ApiConfig.KEY_API_URL, apiUrl).apply()
                    onNavigateBack()
                },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                shape = RoundedCornerShape(8.dp)
            ) {
                Text("Guardar y Volver", fontWeight = FontWeight.Bold)
            }
        }
    }
}
