using Microsoft.EntityFrameworkCore;
using SuperPOS.AFIP;
using SuperPOS.API.Data;
using SuperPOS.API.Hubs;
using SuperPOS.API.Services;

// Solución global: Npgsql tratará todos los DateTime sin Kind como UTC
// Evita el error "Cannot write DateTime with Kind=Unspecified" en toda la API
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddDbContext<SuperPOSDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Servicio AFIP — configurado desde appsettings o con ModoDemo=true por defecto
builder.Services.AddSingleton(new AfipService(new AfipConfig
{
    CUIT = builder.Configuration["Afip:CUIT"] ?? "20000000000",
    PuntoVenta = int.Parse(builder.Configuration["Afip:PuntoVenta"] ?? "1"),
    Homologacion = bool.Parse(builder.Configuration["Afip:Homologacion"] ?? "true"),
    ModoDemo = bool.Parse(builder.Configuration["Afip:ModoDemo"] ?? "true"),
    RutaCertificado = builder.Configuration["Afip:RutaCertificado"],
    PasswordCertificado = builder.Configuration["Afip:PasswordCertificado"]
}));

// Búsqueda web opcional (Tavily / Bing en appsettings; reserva: DuckDuckGo sin clave)
builder.Services.AddHttpClient("websearch", client =>
{
    client.Timeout = TimeSpan.FromSeconds(35);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SuperPOS-API/1.0 (+websearch; compatible)");
});
builder.Services.AddSingleton<IWebSearchService, WebSearchService>();

// Servicio IA — DeepSeek (instanciado por request para acceder al DbContext scoped)
builder.Services.AddScoped<IAiService, DeepSeekAiService>();

// Servicio de importación de base de datos legacy MDB
builder.Services.AddScoped<ImportadorLegacyService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async ctx =>
        {
            ctx.Response.ContentType = "application/json; charset=utf-8";
            var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("SuperPOS.API");
            if (ex != null)
                logger.LogError(ex, "Error no controlado en {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { error = "Error interno del servidor" });
        });
    });
}

app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PosHub>("/hubs/pos");
app.MapGet("/api/health", () => Results.Json(new { ok = true, utc = DateTime.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuperPOSDbContext>();
    db.Database.Migrate();
}

app.Run();
