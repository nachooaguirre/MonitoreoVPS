package com.superpos.mobile.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import com.superpos.mobile.models.Article

@Dao
interface ArticleDao {
    @Query("SELECT * FROM articles WHERE codigoBarras = :barcode OR codigoInterno = :barcode LIMIT 1")
    suspend fun getByBarcode(barcode: String): Article?

    @Query("SELECT * FROM articles WHERE id = :id LIMIT 1")
    suspend fun getById(id: Int): Article?

    @Query("""
        SELECT * FROM articles 
        WHERE descripcion LIKE '%' || :query || '%' 
           OR codigoBarras LIKE '%' || :query || '%' 
           OR codigoInterno LIKE '%' || :query || '%'
        ORDER BY descripcion ASC
    """)
    suspend fun search(query: String): List<Article>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(articles: List<Article>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(article: Article)

    @Query("DELETE FROM articles")
    suspend fun deleteAll()
}
