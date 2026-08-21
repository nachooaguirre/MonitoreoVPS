package com.superpos.mobile.models

import androidx.room.Entity
import androidx.room.PrimaryKey
import java.io.Serializable

data class Departamento(
    val id: Int,
    val nombre: String,
    val activo: Boolean = true
) : Serializable

data class Familia(
    val id: Int,
    val nombre: String,
    val idDepartamento: Int,
    val activo: Boolean = true
) : Serializable

data class Marca(
    val id: Int,
    val nombre: String,
    val activo: Boolean = true
) : Serializable

data class Proveedor(
    val id: Int,
    val razonSocial: String,
    val nombreFantasia: String? = null,
    val cuit: String = "",
    val activo: Boolean = true
) : Serializable

data class Sucursal(
    val id: Int,
    val nombre: String,
    val direccion: String? = null,
    val esCentral: Boolean = false,
    val activo: Boolean = true
) : Serializable

@Entity(tableName = "articles")
data class Article(
    @PrimaryKey val id: Int,
    val codigoBarras: String = "",
    val codigoInterno: String = "",
    val codigoProveedor: String = "",
    val descripcion: String = "",
    val descripcionCorta: String = "",
    val precioCosto: Double = 0.0,
    val precioVenta: Double = 0.0,
    val stockActual: Double = 0.0,
    val stockDeposito: Double = 0.0,
    val idProveedor: Int = 0,
    val idDepartamento: Int = 0,
    val idFamilia: Int = 0,
    val idMarca: Int = 0,
    val activo: Boolean = true,
    val aplicaIva: Boolean = true,
    val alicuotaIva: Double = 21.0,
    val esPesable: Boolean = false,
    val contenidoValor: Double = 1.0,
    val contenidoUnidad: String = "UN",
    // Auxiliares para no complicar relaciones en Room
    val departamentoNombre: String? = null,
    val familiaNombre: String? = null,
    val marcaNombre: String? = null,
    val proveedorNombre: String? = null
) : Serializable

data class PaginatedResponse<T>(
    val total: Int,
    val page: Int,
    val pageSize: Int,
    val items: List<T>
)

data class Inventario(
    val id: Int,
    val descripcion: String,
    val idSucursal: Int,
    val fechaInicio: String,
    val estado: Int,
    val totalArticulos: Int,
    val articulosContados: Int,
    val sucursalNombre: String? = null
) : Serializable

data class InventarioConteoRequest(
    val idArticulo: Int,
    val stockContado: Double,
    val observaciones: String,
    val acumulativo: Boolean
)

data class OrdenCompra(
    val id: Int,
    val idProveedor: Int,
    val nroOrden: Int,
    val fecha: String,
    val estado: Int,
    val total: Double,
    val proveedorNombre: String? = null,
    val detalles: List<OrdenCompraDetalle> = emptyList()
) : Serializable

data class OrdenCompraDetalle(
    val id: Int,
    val idOrdenCompra: Int,
    val idArticulo: Int,
    val cantidadPedida: Double,
    val cantidadRecibida: Double,
    val precioCosto: Double,
    val articulo: Article? = null,
    val observacionDiferencia: String? = null
) : Serializable

data class OrdenCompraRecepcionRequest(
    val idUsuario: Int = 1,
    val idSucursalDestino: Int? = null,
    val items: List<OrdenCompraRecepcionItem>
)

data class OrdenCompraRecepcionItem(
    val idArticulo: Int,
    val cantidadRecibida: Double,
    val precioCosto: Double,
    val observacionDiferencia: String? = null
)

data class EtiquetaEncolarRequest(
    val idArticulo: Int,
    val cantidad: Int
)

data class ArticleCreateRequest(
    val codigoBarras: String,
    val codigoInterno: String,
    val codigoProveedor: String = "",
    val descripcion: String,
    val descripcionCorta: String,
    val precioCosto: Double,
    val precioVenta: Double,
    val stockActual: Double,
    val idProveedor: Int,
    val idDepartamento: Int,
    val idFamilia: Int,
    val idMarca: Int,
    val activo: Boolean = true,
    val aplicaIva: Boolean = true,
    val alicuotaIva: Double = 21.0
)

data class LoginRequest(
    val nombreUsuario: String,
    val password: String
)

data class User(
    val id: Int,
    val nombreUsuario: String,
    val nombreCompleto: String,
    val idPerfil: Int,
    val activo: Boolean,
    val accesoZebra: Boolean
) : Serializable

data class LoginResponse(
    val usuario: User,
    val token: String
)

data class OrdenCompraCreateRequest(
    val idProveedor: Int,
    val idUsuario: Int,
    val estado: Int = 5, // Borrador: queda pendiente de confirmar desde el cliente WPF
    val observaciones: String? = null,
    val detalles: List<OrdenCompraCreateItem>
)

data class OrdenCompraCreateItem(
    val idArticulo: Int,
    val cantidadPedida: Double,
    val precioCosto: Double,
    val alicuotaIva: Double,
    val subtotal: Double
)

data class LocalOcItem(
    val idArticulo: Int,
    val descripcion: String,
    val codigoBarras: String?,
    val cantidadPedida: Double,
    val cantidadRecibida: Double,
    val precioCosto: Double,
    val observacionDiferencia: String? = null
) : Serializable

