using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SuperPOS.AFIP;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.API.Hubs;
using SuperPOS.API.Services;

// Solución global: Npgsql tratará todos los DateTime sin Kind como UTC
// Evita el error "Cannot write DateTime with Kind=Unspecified" en toda la API
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// Permite instalar la API como Windows Service (sc create) para que sobreviva a reinicios/logout
// en la PC del cliente. No-op cuando se corre normal (dotnet run, consola, IIS, etc.).
builder.Host.UseWindowsService(o => o.ServiceName = "SuperPOS API");

builder.Services.AddControllers(o =>
    {
        // Requiere JWT en todos los endpoints por defecto; usar [AllowAnonymous] para excepciones (ej. login).
        o.Filters.Add(new AuthorizeFilter());
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = JwtHelper.GetSigningKey(builder.Configuration)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("esAdministrador", "True"));
});
builder.Services.AddHttpContextAccessor();

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

// Servicio de reportes contables AFIP
builder.Services.AddScoped<IContabilidadService, ContabilidadService>();

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
app.UseAuthentication();
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
