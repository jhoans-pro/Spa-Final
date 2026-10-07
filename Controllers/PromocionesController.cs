
using Microsoft.AspNetCore.Mvc;
using Spa.Interfaces;

namespace Spa.Controllers
{
    public class PromocionesController : Controller
    {
        private readonly IPromocionService _promocionService;

        public PromocionesController(IPromocionService promocionService)
        {
            _promocionService = promocionService;
        }

        public async Task<IActionResult> Index()
        {
            // Al obtener las promociones, el servicio también
            // desactiva las que ya se encuentran vencidas.
            var promociones = await _promocionService.ObtenerTodasAsync();

            var hoy = DateTime.Today;

            var promocionesVigentes = promociones
                .Where(p =>
                    p.Activa &&
                    p.FechaInicio.HasValue &&
                    p.FechaFin.HasValue &&
                    p.FechaInicio.Value.Date <= hoy &&
                    p.FechaFin.Value.Date >= hoy)
                .ToList();

            return View("~/Views/Home/promociones.cshtml", promocionesVigentes);
        }
    }
}