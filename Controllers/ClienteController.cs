using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spa.Data;
using Spa.Models;
using System.Threading.Tasks;

namespace Spa.Controllers
{
    public class ClienteController : Controller
    {
        private readonly SpaDbContext _context;

        public ClienteController(SpaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("ClienteNombre") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correo, string password)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Correo == correo && c.Password == password);

            if (cliente == null)
            {
                ViewBag.Error = "Correo o contraseña incorrectos.";
                return View();
            }

            HttpContext.Session.SetInt32("ClienteId", cliente.Id);
            HttpContext.Session.SetString("ClienteNombre", cliente.Nombre);
            HttpContext.Session.SetString("ClienteTipo", cliente.TipoCliente);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(
            string dni,
            string nombre,
            string apellido,
            string correo,
            string password)
        {
            dni = (dni ?? string.Empty).Trim();
            nombre = (nombre ?? string.Empty).Trim();
            apellido = (apellido ?? string.Empty).Trim();
            correo = (correo ?? string.Empty).Trim().ToLowerInvariant();
            password ??= string.Empty;

            ViewBag.Dni = dni;
            ViewBag.Nombre = nombre;
            ViewBag.Apellido = apellido;
            ViewBag.Correo = correo;

            if (dni.Length != 8 || !dni.All(char.IsDigit))
            {
                ViewBag.ErrorDni = "El DNI debe contener exactamente 8 números.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.ErrorPassword = "La contraseña debe tener como mínimo 6 caracteres.";
                return View();
            }

            if (await _context.Clientes.AnyAsync(c => c.Dni == dni))
            {
                ViewBag.ErrorDni = "Ya existe una cuenta registrada con este DNI.";
                return View();
            }

            if (await _context.Clientes.AnyAsync(c => c.Correo == correo))
            {
                ViewBag.ErrorCorreo = "Ya existe una cuenta registrada con este correo.";
                return View();
            }

            var nuevoCliente = new Cliente
            {
                Dni = dni,
                Nombre = nombre,
                Apellido = apellido,
                TipoCliente = "VIP",
                Correo = correo,
                Password = password
            };

            _context.Clientes.Add(nuevoCliente);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("ClienteId", nuevoCliente.Id);
            HttpContext.Session.SetString("ClienteNombre", nuevoCliente.Nombre);
            HttpContext.Session.SetString("ClienteTipo", nuevoCliente.TipoCliente);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Bienvenida", "Home");
        }
    }
}