package com.superpos.mobile.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.superpos.mobile.models.Article
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun TicketPreview(
    article: Article,
    modifier: Modifier = Modifier
) {
    // Tarjeta simuladora de etiqueta térmica blanca
    Column(
        modifier = modifier
            .fillMaxWidth()
            .background(Color.White, shape = RoundedCornerShape(8.dp))
            .border(2.dp, Color.Black, shape = RoundedCornerShape(8.dp))
            .padding(12.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Encabezado
        Text(
            text = "LOS ANGELES SUPERMERCADOS",
            color = Color.Black,
            fontSize = 11.sp,
            fontWeight = FontWeight.Bold,
            fontFamily = FontFamily.Monospace,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(6.dp))

        // Descripción del artículo
        Text(
            text = article.descripcion.uppercase(),
            color = Color.Black,
            fontSize = 16.sp,
            fontWeight = FontWeight.ExtraBold,
            maxLines = 2,
            textAlign = TextAlign.Center,
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(8.dp))

        // Bloque del precio
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.Bottom
        ) {
            Text(
                text = "$",
                color = Color.Black,
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.padding(bottom = 6.dp)
            )
            
            val price = article.precioVenta
            val wholePart = price.toInt()
            val decimalPart = String.format(Locale.US, "%02d", ((price - wholePart) * 100).toInt())
            
            Text(
                text = "$wholePart",
                color = Color.Black,
                fontSize = 46.sp,
                fontWeight = FontWeight.Black,
                lineHeight = 46.sp
            )
            Text(
                text = ".$decimalPart",
                color = Color.Black,
                fontSize = 22.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.padding(bottom = 6.dp)
            )
        }

        Spacer(modifier = Modifier.height(10.dp))

        // Precio sin impuesto y precio por kilo/litro, igual que la etiqueta real que imprime el cliente WPF
        val precioSinImp = if (article.aplicaIva) article.precioVenta / (1 + article.alicuotaIva / 100) else article.precioVenta
        Text(
            text = "Precio sin Imp.: $ %.3f".format(Locale.US, precioSinImp),
            color = Color.Black,
            fontSize = 10.sp,
            fontFamily = FontFamily.Monospace,
            modifier = Modifier.fillMaxWidth(),
            textAlign = TextAlign.Center
        )

        val esPesable = article.esPesable || article.codigoBarras.startsWith("20")
        val kiloTexto = when {
            esPesable -> "Kilo: %.2f".format(Locale.US, article.precioVenta)
            article.contenidoUnidad == "G" && article.contenidoValor > 0 -> "Kilo: %.2f".format(Locale.US, article.precioVenta * 1000 / article.contenidoValor)
            article.contenidoUnidad == "KG" && article.contenidoValor > 0 -> "Kilo: %.2f".format(Locale.US, article.precioVenta / article.contenidoValor)
            article.contenidoUnidad == "ML" && article.contenidoValor > 0 -> "Litro: %.2f".format(Locale.US, article.precioVenta * 1000 / article.contenidoValor)
            article.contenidoUnidad == "L" && article.contenidoValor > 0 -> "Litro: %.2f".format(Locale.US, article.precioVenta / article.contenidoValor)
            else -> null
        }
        kiloTexto?.let {
            Text(
                text = it,
                color = Color.Black,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Monospace,
                modifier = Modifier.fillMaxWidth(),
                textAlign = TextAlign.Center
            )
        }

        Spacer(modifier = Modifier.height(6.dp))

        // Código de barras simulado vectorialmente por Canvas
        val barcodeStr = if (article.codigoBarras.isNotBlank()) article.codigoBarras else article.codigoInterno.ifBlank { "0000000000000" }
        Canvas(
            modifier = Modifier
                .fillMaxWidth(0.9f)
                .height(35.dp)
        ) {
            val width = size.width
            val height = size.height
            val lineCount = 38
            val startX = 10f
            val endX = width - 10f
            val step = (endX - startX) / lineCount

            for (i in 0 until lineCount) {
                // Alternar anchos según caracteres del código
                val charCode = if (barcodeStr.isNotEmpty()) barcodeStr[i % barcodeStr.length].code else 48
                val rectWidth = if (charCode % 3 == 0) 3.5f else 1.8f
                val offset = startX + (i * step)
                
                // Dibujar línea
                drawRect(
                    color = Color.Black,
                    topLeft = Offset(offset, 0f),
                    size = Size(rectWidth, height)
                )
            }
        }

        // Texto del código de barras
        Text(
            text = barcodeStr,
            color = Color.Black,
            fontSize = 9.sp,
            fontFamily = FontFamily.Monospace,
            letterSpacing = 2.sp,
            textAlign = TextAlign.Center
        )

        Spacer(modifier = Modifier.height(6.dp))

        // Fila de metadatos inferiores
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            val dateFormat = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault())
            Text(
                text = dateFormat.format(Date()),
                color = Color.Black,
                fontSize = 9.sp,
                fontFamily = FontFamily.Monospace
            )
            Text(
                text = "INT: ${article.codigoInterno.ifBlank { article.id.toString() }}",
                color = Color.Black,
                fontSize = 9.sp,
                fontWeight = FontWeight.Bold,
                fontFamily = FontFamily.Monospace
            )
        }
    }
}
