using EBData;
using EBEmail;
using EBRepositories;
using EBRepositories.Interfaces;
using EBServices;
using EBServices.Interfaces;
using EmbassyBusinessBack.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using EBServices.Interfaces.Notifications;
using EBServices.Notifications;

var builder = WebApplication.CreateBuilder(args);

// ===== Servicios =====
builder.Services.AddControllers();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// DI
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ICatalogosService, CatalogosService>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IFuenteOrigenService, FuenteOrigenService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IReferidoService, ReferidoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<ISeguimientoReferidoService, SeguimientoReferidoService>();
builder.Services.AddScoped<IBancoUsuarioService, BancoUsuarioService>();
builder.Services.AddScoped<IGrupoService, GrupoService>();
builder.Services.AddScoped<IEmpresaGrupoService, EmpresaGrupoService>();
builder.Services.AddScoped<IPeriodoService, PeriodoService>();

// DB
builder.Services.AddDbContext<EBDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SQLConexion")));

// HTTPClient / SignalR / Swagger
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFcmSender, FcmSender>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.CustomSchemaIds(t => t.FullName); });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", cors =>
    {
        cors.SetIsOriginAllowed(origin =>
            !string.IsNullOrEmpty(origin) &&
            (origin.StartsWith("capacitor://localhost") ||
             origin.StartsWith("ionic://localhost") ||
             origin.StartsWith("https://localhost") ||
             origin.StartsWith("http://localhost") ||
             origin.StartsWith("http://localhost:4200") ||
             origin.StartsWith("http://localhost:54697") ||
             origin.Contains("embassyen.com") ||
             origin.Contains("bithub.com.mx")))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// Memoria caché si la usan algunos controladores
builder.Services.AddMemoryCache();

var app = builder.Build();

// ===== Middleware =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            await context.Response.WriteAsJsonAsync(new
            {
                mensaje = "Error interno",
                detalle = feature?.Error?.Message
            });
        });
    });
}

app.UseHttpsRedirection();

// ✅ sirve /wwwroot (p. ej. /public/brand/embassy-logo.png)
app.UseStaticFiles();

// mapping extra para QR físicos C:\tmp\qr -> /public/qr
var qrPhysicalPath = @"C:\tmp\qr";
Directory.CreateDirectory(qrPhysicalPath);

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".png"] = "image/png";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(qrPhysicalPath),
    RequestPath = "/public/qr",
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800";
    }
});

app.UseRouting();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

// Rutas de depuración opcionales
app.MapGet("/__debug/fiscal-ctors", () =>
{
    var ctors = typeof(FiscalController).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    return Results.Ok(ctors.Select(c => c.ToString()).ToArray());
});

app.MapGet("/__debug/fiscal-asm", () =>
{
    var asm = typeof(FiscalController).Assembly;
    var path = asm.Location;
    return Results.Ok(new
    {
        asm = asm.FullName,
        path,
        lastWrite = System.IO.File.Exists(path) ? System.IO.File.GetLastWriteTime(path) : (DateTime?)null,
        runtime = RuntimeInformation.FrameworkDescription,
        process = Environment.ProcessPath
    });
});

app.MapControllers();

// (debug) listar rutas
app.MapGet("/_routes", (IEnumerable<EndpointDataSource> sources) =>
{
    var routes = sources.SelectMany(s => s.Endpoints)
                        .OfType<RouteEndpoint>()
                        .Select(e => $"{e.RoutePattern.RawText} [{string.Join(",", e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? new[] { "(any)" })}]");
    return Results.Ok(routes);
});

app.Run();