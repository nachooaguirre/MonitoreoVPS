namespace SuperPOS.API.Services;

/// <summary>Resumen de resultados de internet para inyectar en el prompt (comparar con competidores, precios de mercado, etc.).</summary>
public interface IWebSearchService
{
    /// <param name="consulta">Términos a buscar o la pregunta del usuario (se acorta internamente si hace falta).</param>
    /// <returns>Texto con fragmentos o null si no hubo nada utilizable (la IA deberá limitarse a razonar sin datos frescos de web).</returns>
    Task<string?> BuscarResumenWebAsync(string consulta, CancellationToken cancellationToken = default);
}
