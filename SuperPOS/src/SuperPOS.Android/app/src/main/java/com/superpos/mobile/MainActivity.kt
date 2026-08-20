package com.superpos.mobile

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.remember
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.core.content.ContextCompat
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.superpos.mobile.ui.screens.*
import com.superpos.mobile.ui.theme.SuperPOSTheme

class MainActivity : ComponentActivity() {

    private val TAG = "SuperPOS_MainActivity"

    // Zebra DataWedge Broadcast action and extras keys
    private val SCANNED_ACTION = "com.superpos.scanner.ACTION"
    private val DATAWEDGE_EXTRA_DATA_STRING = "com.symbol.datawedge.data_string"

    // Registered active scan handler from Compose screens
    private var activeScanCallback: ((String) -> Unit)? = null

    // Broadcast receiver to intercept the physical laser triggers
    private val dataWedgeReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            if (intent != null && intent.action == SCANNED_ACTION) {
                val scannedData = intent.getStringExtra(DATAWEDGE_EXTRA_DATA_STRING)
                Log.d(TAG, "Scanned hardware data received: $scannedData")
                if (!scannedData.isNullOrBlank()) {
                    runOnUiThread {
                        val callback = activeScanCallback
                        if (callback != null) {
                            callback.invoke(scannedData.trim())
                        } else {
                            Log.w(TAG, "No active scan callback registered for scanned barcode: $scannedData")
                            Toast.makeText(context, "Escaneado: $scannedData (No hay pantalla activa)", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        setContent {
            SuperPOSTheme {
                Surface(
                    modifier = Modifier.fillMaxSize()
                ) {
                    val navController = rememberNavController()
                    var loggedInUser by remember { mutableStateOf<com.superpos.mobile.models.User?>(null) }

                    // Helper to register scan actions inside Composable screens
                    val registerCallback: ((String) -> Unit) -> Unit = { callback ->
                        activeScanCallback = callback
                        Log.d(TAG, "Registered new scan callback: $callback")
                    }

                    NavHost(
                        navController = navController,
                        startDestination = "login"
                    ) {
                        composable("login") {
                            activeScanCallback = null
                            LoginScreen(
                                onLoginSuccess = { user ->
                                    loggedInUser = user
                                    navController.navigate("menu") {
                                        popUpTo("login") { inclusive = true }
                                    }
                                },
                                onNavigateToConfig = { navController.navigate("config") }
                            )
                        }
                        composable("menu") {
                            // Redirect to login if user is not authenticated
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) {
                                    navController.navigate("login") {
                                        popUpTo(0) { inclusive = true }
                                    }
                                }
                            } else {
                                activeScanCallback = null
                                MenuScreen(
                                    onNavigateToInventario = { navController.navigate("inventario") },
                                    onNavigateToRecepcion = { navController.navigate("recepcion") },
                                    onNavigateToEtiquetas = { navController.navigate("etiquetas") },
                                    onNavigateToStock = { navController.navigate("stock") },
                                    onNavigateToConfig = { navController.navigate("config") },
                                    onNavigateToEditarProducto = { navController.navigate("editar_producto") },
                                    onNavigateToEgresosIngresos = { navController.navigate("egresos_ingresos") },
                                    onNavigateToNuevaOrdenCompra = { navController.navigate("nueva_orden_compra") }
                                )
                            }
                        }
                        composable("inventario") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                InventarioScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("recepcion") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                RecepcionScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("etiquetas") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                EtiquetasScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("stock") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                StockScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("editar_producto") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                EditarProductoScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("egresos_ingresos") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                EgresosIngresosScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("nueva_orden_compra") {
                            if (loggedInUser == null) {
                                LaunchedEffect(Unit) { navController.navigate("login") { popUpTo(0) { inclusive = true } } }
                            } else {
                                NuevaOrdenCompraScreen(
                                    operatorId = loggedInUser!!.id,
                                    onNavigateBack = { navController.navigateUp() },
                                    registerScanCallback = registerCallback
                                )
                            }
                        }
                        composable("config") {
                            activeScanCallback = null
                            ConfigScreen(
                                onNavigateBack = { navController.navigateUp() }
                            )
                        }
                    }
                }
            }
        }
    }

    override fun onResume() {
        super.onResume()
        // Register the DataWedge receiver dynamically
        val filter = IntentFilter().apply {
            addAction(SCANNED_ACTION)
            addCategory(Intent.CATEGORY_DEFAULT)
        }
        ContextCompat.registerReceiver(
            this,
            dataWedgeReceiver,
            filter,
            ContextCompat.RECEIVER_EXPORTED
        )
        Log.i(TAG, "DataWedge broadcast receiver registered")
    }

    override fun onPause() {
        super.onPause()
        // Unregister to prevent leaks
        try {
            unregisterReceiver(dataWedgeReceiver)
            Log.i(TAG, "DataWedge broadcast receiver unregistered")
        } catch (e: Exception) {
            Log.e(TAG, "Error unregistering receiver: ${e.message}")
        }
    }
}
