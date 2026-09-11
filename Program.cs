using Microsoft.EntityFrameworkCore;
using Spa.Data;
using Spa.Interfaces;
using Spa.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<ICalculadorDescuento, DescuentoVIP>();
builder.Services.AddScoped<INotificador, NotificadorConsola>();
builder.Services.AddScoped<IPromocionService, PromocionService>();

builder.Services.AddDbContext<SpaDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("SpaConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("SpaConnection")
        )
    );
});

// Sesión para login de clientes
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession(); // debe ir antes de UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Bienvenida}/{id?}")
    .WithStaticAssets();

app.Run();