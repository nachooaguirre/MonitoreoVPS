namespace SuperPOS.API.Helpers;

public static class DateTimeExtensions
{
    /// <summary>
    /// Convierte un DateTime a UTC independientemente de su Kind.
    /// Cuando viene del query string, el Kind es Unspecified — PostgreSQL solo acepta UTC.
    /// </summary>
    public static DateTime ToUtc(this DateTime dt)
        => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    public static DateTime? ToUtc(this DateTime? dt)
        => dt.HasValue ? dt.Value.ToUtc() : null;
}
