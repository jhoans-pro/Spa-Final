using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spa.Data;
using Spa.Interfaces;
using Spa.Models;
using System.Threading.Tasks;

namespace Spa.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly SpaDbContext _context;
        private readonly IPromocionService _promocionService;

        public AdministradorController(SpaDbContext context, IPromocionService promocionService)
        {
            _context = context;
            _promocionService = promocionService;
        }

        public async Task<IActionResult> Panel()
        {
            ViewBag.TotalClientes = await _context.Clientes.CountAsync();
            ViewBag.TotalCitas = await _context.Citas.CountAsync();
            ViewBag.TotalPromociones = await _context.Promociones.CountAsync();
            ViewBag.TotalServicios = await _context.Servicios.CountAsync();

            var citas = await _context.Citas
                .AsNoTracking()
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            ViewBag.Clientes = await _context.Clientes
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(citas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCita(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita != null)
            {
                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Cita eliminada correctamente.";
            }
            return RedirectToAction("Panel");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                var citasCliente = await _context.Citas.Where(c => c.ClienteId == id).ToListAsync();
                _context.Citas.RemoveRange(citasCliente);
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Cliente y sus citas eliminados correctamente.";
            }
            return RedirectToAction("Panel");
        }

        [HttpGet]
        public async Task<IActionResult> Promociones()
        {
            var promociones = await _promocionService.ObtenerTodasAsync();
            return View(promociones);
        }

        [HttpGet]
        public IActionResult CrearPromocion() => View(new Promocion());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPromocion(Promocion promocion)
        {
            if (promocion.FechaInicio.HasValue &&
            promocion.FechaFin.HasValue &&
            promocion.FechaFin.Value.Date < promocion.FechaInicio.Value.Date)
        {
            ModelState.AddModelError(
                "FechaFin",
                "La fecha de fin no puede ser anterior a la fecha de inicio."
            );
        }

            if (!ModelState.IsValid)
        {
            return View(promocion);
        }

            await _promocionService.CrearAsync(promocion);

            return RedirectToAction("Promociones");
        }

        [HttpGet]
        public async Task<IActionResult> EditarPromocion(int id)
        {
            var promo = await _promocionService.ObtenerPorIdAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPromocion(Promocion promocion)
        {
            if (promocion.FechaInicio.HasValue &&
                promocion.FechaFin.HasValue &&
                promocion.FechaFin.Value.Date < promocion.FechaInicio.Value.Date)
        {
            ModelState.AddModelError(
                "FechaFin",
                "La fecha de fin no puede ser anterior a la fecha de inicio."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(promocion);
        }

            await _promocionService.ActualizarAsync(promocion);

            return RedirectToAction("Promociones");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPromocion(int id)
        {
            await _promocionService.EliminarAsync(id);
            return RedirectToAction("Promociones");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usuario, string password)
        {
            var admin = await _context.Administradores
                .FirstOrDefaultAsync(a =>
                    a.Usuario == usuario &&
                    a.Password == password);

            if (admin != null)
                return RedirectToAction("Panel");

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }
    }
}