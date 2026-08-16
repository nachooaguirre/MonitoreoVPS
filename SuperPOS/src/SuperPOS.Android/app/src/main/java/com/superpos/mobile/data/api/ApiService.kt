package com.superpos.mobile.data.api

import com.superpos.mobile.models.*
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.*

interface ApiService {
    @GET("health")
    suspend fun checkHealth(): Response<ResponseBody>

    @POST("usuarios/login")
    suspend fun login(
        @Body request: LoginRequest,
        @Query("esZebra") esZebra: Boolean = true
    ): Response<LoginResponse>

    // --- ARTICULOS ---
    @GET("articulos/buscarPorCodigoBarras/{codigo}")
    suspend fun getArticleByBarcode(@Path("codigo") codigo: String): Response<Article>

    @GET("articulos/{id}")
    suspend fun getArticleById(@Path("id") id: Int): Response<Article>

    @POST("articulos")
    suspend fun createArticle(
        @Body article: ArticleCreateRequest,
        @Query("idUsuario") idUsuario: Int? = null,
        @Query("idSucursal") idSucursal: Int? = null
    ): Response<Article>

    @PUT("articulos/{id}/ajustar-stock")
    suspend fun adjustStock(
        @Path("id") id: Int,
        @Query("delta") delta: Double,
        @Query("idSucursal") idSucursal: Int,
        @Query("motivo") motivo: String? = null,
        @Query("idUsuario") idUsuario: Int? = null
    ): Response<ResponseBody>

    @PUT("articulos/{id}")
    suspend fun updateArticle(
        @Path("id") id: Int,
        @Body article: Article,
        @Query("idUsuario") idUsuario: Int? = null,
        @Query("idSucursal") idSucursal: Int? = null
    ): Response<ResponseBody>

    @GET("articulos/departamentos")
    suspend fun getDepartamentos(): Response<List<Departamento>>

    @GET("articulos/familias")
    suspend fun getFamilias(@Query("idDepartamento") idDepartamento: Int): Response<List<Familia>>

    @GET("articulos/marcas")
    suspend fun getMarcas(): Response<List<Marca>>

    // --- INVENTARIOS ---
    @GET("inventarios")
    suspend fun getInventarios(): Response<List<Inventario>>

    @PUT("inventarios/{id}/contar")
    suspend fun recordInventoryCount(
        @Path("id") id: Int,
        @Body request: InventarioConteoRequest
    ): Response<ResponseBody>

    // --- ORDENES COMPRA ---
    @GET("ordenescompra")
    suspend fun getOrdenesCompra(): Response<List<OrdenCompra>>

    @GET("ordenescompra/{id}")
    suspend fun getOrdenCompraDetail(@Path("id") id: Int): Response<OrdenCompra>

    @PUT("ordenescompra/{id}/recibir")
    suspend fun receiveOrdenCompra(
        @Path("id") id: Int,
        @Body request: OrdenCompraRecepcionRequest
    ): Response<ResponseBody>

    // --- ETIQUETAS ---
    @POST("etiquetas/encolar")
    suspend fun enqueueLabel(@Body request: EtiquetaEncolarRequest): Response<ResponseBody>

    // --- AUXILIARES ---
    @GET("sucursales")
    suspend fun getSucursales(): Response<List<Sucursal>>

    @GET("proveedores")
    suspend fun getProveedores(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 500
    ): Response<PaginatedResponse<Proveedor>>
}
