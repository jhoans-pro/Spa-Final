using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spa.Data;
using Spa.Interfaces;
using Spa.Models;
using Spa.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spa.Controllers
{
    public class CitaController : Controller
    {
        private readonly ICitaService _citaService;
        private readonly SpaDbContext _context;

        public CitaController(ICitaService citaService, SpaDbContext context)
        {
            _citaService = citaService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Crear");
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var hoy = DateTime.Today;

            ViewBag.Promociones = await _context.Promociones
            .AsNoTracking()
            .Where(p =>
                p.Activa &&
                p.FechaInicio.HasValue &&
                p.FechaFin.HasValue &&
                p.FechaInicio.Value <= hoy &&
                p.FechaFin.Value >= hoy)
            .ToListAsync();

        return View("~/Views/Home/cita.cshtml");
        }

        private static readonly Dictionary<string, string> MapaServicios = new()
        {
            { "masaje-relajante", "Masaje Relajante" },
            { "masaje-piedras-calientes", "Masaje de Piedras Calientes" },
            { "masaje-descontracturante", "Masaje Descontracturante" },
            { "facial-express", "Facial Express" },
            { "facial-profundo", "Facial Profundo" },
            { "facial-rejuvenecedor", "Facial Rejuvenecedor" },
            { "camara-vapor", "Camara a Vapor" },
            { "camara-seca", "Camara Seca" },
            { "tina-hidromasaje", "Tina de Hidromasajes" }
        };

        private static readonly Dictionary<string, (string SlugBase, decimal Precio)> Promociones = new()
        {
            { "promo-mes",          ("masaje-relajante", 160.00m) },
            { "promo-cumple",       ("masaje-relajante", 260.00m) },
            { "promo-ritual-relax", ("masaje-relajante", 320.00m) }
        };

        private async Task<Servicio?> BuscarServicioEnBdAsync(string slugServicio, string duracion)
        {
            if (!MapaServicios.TryGetValue(slugServicio, out var nombreReal))
                return null;

            var candidatos = await _context.Servicios
                .AsNoTracking()
                .Where(s => s.Nombre == nombreReal)
                .ToListAsync();

            if (candidatos.Count == 0)
                return null;

            var exacto = candidatos.FirstOrDefault(s => s.Duracion == duracion);
            if (exacto != null)
                return exacto;

            return candidatos.First();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            string dni,
            string nombre,
            string apellido,
            string? tipoCliente,
            string servicio,
            string duracion,
            string fecha,
            string hora,
            int cantidadPersonas = 1)
        {
            if (string.IsNullOrWhiteSpace(dni) ||
                dni.Length != 8 ||
                !dni.All(char.IsDigit))
            {
                ModelState.AddModelError("dni", "El DNI debe contener exactamente 8 números.");
                return View("~/Views/Home/cita.cshtml");
            }

            if (string.IsNullOrWhiteSpace(servicio))
            {
                ModelState.AddModelError("servicio", "Debe seleccionar un servicio.");
                return View("~/Views/Home/cita.cshtml");
            }

            if (!DateTime.TryParse($"{fecha} {hora}", out var fechaHora))
            {
                ModelState.AddModelError("fecha", "Debe seleccionar una fecha válida.");
                ModelState.AddModelError("hora", "Debe seleccionar una hora válida.");
                return View("~/Views/Home/cita.cshtml");
            }

            if (fechaHora < DateTime.Now)
            {
                ModelState.AddModelError("fecha", "La fecha y la hora de la cita no pueden estar en el pasado.");
                return View("~/Views/Home/cita.cshtml");
            }

            var horaApertura = new TimeSpan(10, 0, 0);
            var horaCierre = new TimeSpan(21, 0, 0);

            if (fechaHora.TimeOfDay < horaApertura || fechaHora.TimeOfDay > horaCierre)
            {
                ModelState.AddModelError("hora", "El horario de atención es de 10:00 a. m. a 9:00 p. m.");
                return View("~/Views/Home/cita.cshtml");
            }

            Servicio? servicioSeleccionado;
            decimal? precioPromocion = null;

            if (servicio.StartsWith("promo-db-"))
            {
                var idTexto = servicio.Replace("promo-db-", "");

                if (int.TryParse(idTexto, out int promoId))
                {
                    var hoy = DateTime.Today;

                    var promocion = await _context.Promociones
                        .FirstOrDefaultAsync(p =>
                                p.Id == promoId &&
                                p.Activa &&
                                p.FechaInicio.HasValue &&
                                p.FechaFin.HasValue &&
                                p.FechaInicio.Value.Date <= hoy &&
                                p.FechaFin.Value.Date >= hoy);

                    if (promocion != null)
                    {
                        servicioSeleccionado = await _context.Servicios
                            .FirstOrDefaultAsync(s => s.Nombre == promocion.Nombre);

                        if (servicioSeleccionado == null)
                        {
                            servicioSeleccionado = new Servicio
                            {
                                Nombre = promocion.Nombre,
                                PrecioBase = promocion.Precio,
                                Duracion = "90 min"
                            };

                            _context.Servicios.Add(servicioSeleccionado);
                            await _context.SaveChangesAsync();
                        }

                        precioPromocion = promocion.Precio;
                    }
                    else
                    {
                        ModelState.AddModelError("servicio", "La promoción seleccionada no está disponible.");
                        return View("~/Views/Home/cita.cshtml");
                    }
                }
                else
                {
                    ModelState.AddModelError("servicio", "Promoción inválida.");
                    return View("~/Views/Home/cita.cshtml");
                }
            }
            else if (Promociones.TryGetValue(servicio, out var promo))
            {
                servicioSeleccionado = await BuscarServicioEnBdAsync(promo.SlugBase, "60 min");
                precioPromocion = promo.Precio;
            }
            else
            {
                servicioSeleccionado = await BuscarServicioEnBdAsync(servicio, duracion);
            }

            if (servicioSeleccionado == null)
            {
                ModelState.AddModelError("servicio", "El servicio seleccionado no está disponible.");
                return View("~/Views/Home/cita.cshtml");
            }

            bool existeCitaDuplicada = await _context.Citas.AnyAsync(c =>
                c.FechaHora == fechaHora &&
                c.ServicioId == servicioSeleccionado.Id &&
                c.Confirmada
            );

            if (existeCitaDuplicada)
            {
                ModelState.AddModelError(
                    "hora",
                    "Ya existe una cita para este servicio en la fecha y hora seleccionadas. Elige otro horario."
                );
                return View("~/Views/Home/cita.cshtml");
            }

            if (precioPromocion.HasValue)
            {
                servicioSeleccionado = new Servicio
                {
                    Id = servicioSeleccionado.Id,
                    Nombre = servicioSeleccionado.Nombre,
                    Duracion = servicioSeleccionado.Duracion,
                    PrecioBase = precioPromocion.Value
                };
            }

            var clienteIdSesion = HttpContext.Session.GetInt32("ClienteId");
            Cliente cliente;

            if (clienteIdSesion.HasValue)
            {
                var clienteSesion = await _context.Clientes.FindAsync(clienteIdSesion.Value);
                if (clienteSesion != null)
                {
                    cliente = clienteSesion;
                }
                else
                {
                    cliente = new Cliente
                    {
                        Dni = dni,
                        Nombre = nombre,
                        Apellido = apellido,
                        TipoCliente = "Regular"
                    };
                }
            }
            else
            {
                cliente = new Cliente
                {
                    Dni = dni,
                    Nombre = nombre,
                    Apellido = apellido,
                    TipoCliente = string.IsNullOrWhiteSpace(tipoCliente) ? "Regular" : tipoCliente
                };
            }

            ICalculadorDescuento estrategia = cliente.TipoCliente == "VIP"
                ? new DescuentoVIP()
                : new SinDescuento();

            try
            {
                await _citaService.AgendarCitaAsync(
                    cliente,
                    servicioSeleccionado,
                    fechaHora,
                    cantidadPersonas,
                    estrategia
                );
            }
            catch (CitaDuplicadaException ex)
            {
                ModelState.AddModelError("hora", ex.Message);
                return View("~/Views/Home/cita.cshtml");
            }

            TempData["MensajeExito"] = "¡Cita registrada con éxito!";
            return RedirectToAction("Crear", "Cita");
        }
    }
}