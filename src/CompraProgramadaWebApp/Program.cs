using System;
using Microsoft.EntityFrameworkCore;
using CompraProgramadaWebApp.Data;
using System.Reflection;
using System.IO;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container and configure JSON formatting
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        // Garantir que decimais sejam serializados com 2 casas decimais
        opts.JsonSerializerOptions.Converters.Add(new CompraProgramadaWebApp.Utils.Json.DecimalTwoPlacesConverter());
        opts.JsonSerializerOptions.Converters.Add(new CompraProgramadaWebApp.Utils.Json.NullableDecimalTwoPlacesConverter());
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CompraProgramada API", Version = "v1" });
    // Include XML comments if present
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    }
    catch { }
});

// Configure Entity Framework Core with MySQL using connection string from environment variable CONEXAO_BANCO
var connectionString = Environment.GetEnvironmentVariable("CONEXAO_BANCO")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    // If no environment variable is set, the application will still start but DbContext will throw when used.
    Console.WriteLine("WARNING: CONEXAO_BANCO environment variable is not set. Database operations will fail until it's provided.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
});

// Repositories and Services
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.IClienteRepository, CompraProgramadaWebApp.Data.Repositories.ClienteRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.IClienteService, CompraProgramadaWebApp.Services.ClienteService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.ICestaRepository, CompraProgramadaWebApp.Data.Repositories.CestaRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.ICestaService, CompraProgramadaWebApp.Services.CestaService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.IContaMasterRepository, CompraProgramadaWebApp.Data.Repositories.ContaMasterRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.IContaMasterService, CompraProgramadaWebApp.Services.ContaMasterService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.IContaGraficaRepository, CompraProgramadaWebApp.Data.Repositories.ContaGraficaRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.ICustodiaRepository, CompraProgramadaWebApp.Data.Repositories.CustodiaRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.IContaGraficaService, CompraProgramadaWebApp.Services.ContaGraficaService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.IOrdemRepository, CompraProgramadaWebApp.Data.Repositories.OrdemRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.ICompraProgramadaService, CompraProgramadaWebApp.Services.CompraProgramadaService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.ICotacaoRepository, CompraProgramadaWebApp.Data.Repositories.CotacaoRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.ICotacaoService, CompraProgramadaWebApp.Services.CotacaoService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.ICotacaoImportService, CompraProgramadaWebApp.Services.CotacaoImportService>();
builder.Services.AddScoped<CompraProgramadaWebApp.Data.Repositories.IRebalanceamentoRepository, CompraProgramadaWebApp.Data.Repositories.RebalanceamentoRepository>();
builder.Services.AddScoped<CompraProgramadaWebApp.Services.IRebalanceamentoService, CompraProgramadaWebApp.Services.RebalanceamentoService>();
builder.Services.AddSingleton<CompraProgramadaWebApp.Services.IKafkaProducerService, CompraProgramadaWebApp.Services.KafkaProducerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CompraProgramada API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger
    });
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
