package com.superpos.mobile.util

import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import java.io.File
import java.net.HttpURLConnection
import java.net.URL

/**
 * Descarga el APK de actualizacion dentro de la app (cacheDir/updates) y dispara
 * el instalador nativo de Android via FileProvider. El usuario todavia tiene que
 * confirmar "Instalar" en el dialogo del sistema (Android no permite instalacion
 * silenciosa fuera de Google Play), pero ya no hace falta ir al navegador.
 */
object AppUpdater {

    suspend fun downloadAndInstall(context: Context, apkDownloadUrl: String, onProgress: (Int) -> Unit) {
        val updatesDir = File(context.cacheDir, "updates").apply { mkdirs() }
        val apkFile = File(updatesDir, "SuperPOS-Mobile.apk")

        val conn = URL(apkDownloadUrl).openConnection() as HttpURLConnection
        conn.connectTimeout = 8000
        conn.readTimeout = 15000
        conn.connect()

        val totalBytes = conn.contentLength
        conn.inputStream.use { input ->
            apkFile.outputStream().use { output ->
                val buffer = ByteArray(8 * 1024)
                var bytesRead: Int
                var totalRead = 0L
                while (input.read(buffer).also { bytesRead = it } != -1) {
                    output.write(buffer, 0, bytesRead)
                    totalRead += bytesRead
                    if (totalBytes > 0) {
                        onProgress(((totalRead * 100) / totalBytes).toInt())
                    }
                }
            }
        }

        val apkUri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", apkFile)
        val installIntent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(apkUri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(installIntent)
    }
}
