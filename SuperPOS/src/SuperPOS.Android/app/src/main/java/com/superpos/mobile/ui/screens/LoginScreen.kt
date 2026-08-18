package com.superpos.mobile.ui.screens

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.superpos.mobile.BuildConfig
import com.superpos.mobile.data.api.ApiClient
import com.superpos.mobile.data.api.ApiConfig
import com.superpos.mobile.models.LoginRequest
import com.superpos.mobile.models.User
import com.superpos.mobile.ui.theme.*
import com.superpos.mobile.util.AppUpdater
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.net.HttpURLConnection
import java.net.URL

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoginScreen(
    onLoginSuccess: (User) -> Unit,
    onNavigateToConfig: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences(ApiConfig.PREFS_NAME, Context.MODE_PRIVATE) }
    
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var isPasswordVisible by remember { mutableStateOf(false) }
    
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    
    val scope = rememberCoroutineScope()

    // Read API URL from preferences
    val apiUrl = remember { mutableStateOf("") }
    var updateDisponible by remember { mutableStateOf(false) }
    var updateApkUrl by remember { mutableStateOf("") }
    var descargandoUpdate by remember { mutableStateOf(false) }
    var progresoDescarga by remember { mutableStateOf(0) }
    var errorUpdate by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        apiUrl.value = sharedPrefs.getString(ApiConfig.KEY_API_URL, ApiConfig.DEFAULT_BASE_URL) ?: ApiConfig.DEFAULT_BASE_URL

        val baseServidor = apiUrl.value.removeSuffix("/").removeSuffix("/api")
        withContext(Dispatchers.IO) {
            try {
                val conn = URL("$baseServidor/downloads/mobile-version.txt").openConnection() as HttpURLConnection
                conn.connectTimeout = 4000
                conn.readTimeout = 4000
                val remoteVersion = conn.inputStream.bufferedReader().readText().trim()
                if (remoteVersion.isNotBlank() && remoteVersion != BuildConfig.APP_VERSION) {
                    updateApkUrl = "$baseServidor/downloads/SuperPOS-Mobile.apk"
                    updateDisponible = true
                }
            } catch (_: Exception) {
                // Sin conexion o servidor caido: no molestar en el login.
            }
        }
    }

    if (updateDisponible) {
        AlertDialog(
            onDismissRequest = { if (!descargandoUpdate) updateDisponible = false },
            title = { Text("Actualización disponible") },
            text = {
                Column {
                    Text(
                        if (descargandoUpdate) "Descargando actualización... $progresoDescarga%"
                        else "Hay una nueva versión de SuperPOS Mobile. Se va a descargar e instalar."
                    )
                    if (descargandoUpdate) {
                        Spacer(modifier = Modifier.height(12.dp))
                        LinearProgressIndicator(
                            progress = { progresoDescarga / 100f },
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                    errorUpdate?.let {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(it, color = FluentError, fontSize = 12.sp)
                    }
                }
            },
            confirmButton = {
                TextButton(
                    enabled = !descargandoUpdate,
                    onClick = {
                        descargandoUpdate = true
                        errorUpdate = null
                        scope.launch {
                            try {
                                withContext(Dispatchers.IO) {
                                    AppUpdater.downloadAndInstall(context, updateApkUrl) { pct ->
                                        progresoDescarga = pct
                                    }
                                }
                                updateDisponible = false
                            } catch (e: Exception) {
                                errorUpdate = "No se pudo descargar: ${e.message}"
                            } finally {
                                descargandoUpdate = false
                            }
                        }
                    }
                ) { Text("Descargar e instalar") }
            },
            dismissButton = {
                TextButton(enabled = !descargandoUpdate, onClick = { updateDisponible = false }) { Text("Ahora no") }
            }
        )
    }

    Scaffold(
        containerColor = FluentDarkBg
    ) { paddingValues ->
        Column(
            modifier = modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(24.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header Title (Icon-less, Minimalist Typography)
            Text(
                text = "SuperPOS",
                color = FluentBlue,
                fontSize = 36.sp,
                fontWeight = FontWeight.Black,
                letterSpacing = 1.sp
            )
            Text(
                text = "Terminal Colectora Zebra",
                color = FluentWhite,
                fontSize = 18.sp,
                fontWeight = FontWeight.Medium,
                modifier = Modifier.padding(top = 4.dp)
            )
            Text(
                text = "Iniciá sesión para realizar operaciones en salón y depósito.",
                color = FluentLightGray,
                fontSize = 13.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = 8.dp, bottom = 32.dp)
            )

            // Card Form
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = FluentDarkCard),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    Text(
                        text = "IDENTIFICACIÓN DEL OPERADOR",
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = FluentLightGray
                    )

                    // Error Alert
                    errorMessage?.let { err ->
                        Text(
                            text = err,
                            color = FluentError,
                            fontSize = 13.sp,
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(FluentError.copy(alpha = 0.1f), shape = RoundedCornerShape(4.dp))
                                .padding(10.dp),
                            textAlign = TextAlign.Center
                        )
                    }

                    // Username Input
                    OutlinedTextField(
                        value = username,
                        onValueChange = { username = it },
                        label = { Text("Nombre de usuario") },
                        placeholder = { Text("Ej. adm01") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite,
                            unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue,
                            unfocusedBorderColor = FluentDivider
                        )
                    )

                    // Password Input
                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it },
                        label = { Text("Contraseña") },
                        placeholder = { Text("Ingresá tu clave") },
                        singleLine = true,
                        visualTransformation = if (isPasswordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                        trailingIcon = {
                            TextButton(onClick = { isPasswordVisible = !isPasswordVisible }) {
                                Text(
                                    text = if (isPasswordVisible) "Ocultar" else "Mostrar",
                                    color = FluentBlue,
                                    fontSize = 12.sp,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                        },
                        modifier = Modifier.fillMaxWidth(),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = FluentWhite,
                            unfocusedTextColor = FluentWhite,
                            focusedBorderColor = FluentBlue,
                            unfocusedBorderColor = FluentDivider
                        )
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    // Login Button
                    Button(
                        onClick = {
                            if (username.isBlank() || password.isBlank()) {
                                errorMessage = "Por favor, completá usuario y contraseña."
                                return@Button
                            }
                            isLoading = true
                            errorMessage = null
                            scope.launch {
                                try {
                                    val service = ApiClient.getService(apiUrl.value)
                                    val req = LoginRequest(nombreUsuario = username.trim(), password = password)
                                    val response = withContext(Dispatchers.IO) { service.login(req) }
                                    if (response.isSuccessful && response.body() != null) {
                                        val loginResult = response.body()!!
                                        ApiClient.authToken = loginResult.token
                                        onLoginSuccess(loginResult.usuario)
                                    } else {
                                        val errorMsg = if (response.code() == 401) {
                                            "Usuario no autorizado o credenciales inválidas."
                                        } else {
                                            "Error de inicio de sesión: ${response.code()}"
                                        }
                                        errorMessage = errorMsg
                                    }
                                } catch (e: Exception) {
                                    errorMessage = "Error de conexión: ${e.message}"
                                } finally {
                                    isLoading = false
                                }
                            }
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = FluentBlue, contentColor = FluentDarkBg),
                        shape = RoundedCornerShape(6.dp),
                        enabled = !isLoading
                    ) {
                        if (isLoading) {
                            CircularProgressIndicator(modifier = Modifier.size(18.dp), color = FluentDarkBg)
                        } else {
                            Text("Ingresar", fontWeight = FontWeight.Bold, fontSize = 15.sp)
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Server Config Indicator
            Text(
                text = "Servidor: ${apiUrl.value}",
                color = FluentLightGray,
                fontSize = 12.sp,
                textAlign = TextAlign.Center
            )
            TextButton(
                onClick = onNavigateToConfig,
                colors = ButtonDefaults.textButtonColors(contentColor = FluentBlue)
            ) {
                Text("Configurar Servidor", fontWeight = FontWeight.SemiBold, fontSize = 13.sp)
            }
        }
    }
}
