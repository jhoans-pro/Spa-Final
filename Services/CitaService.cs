using Spa.Interfaces;
using Spa.Models;
using Spa.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace Spa.Services
{
    // SRP: Su única función es coordinar el flujo del agendamiento.
    public class CitaService : ICitaService
    {
        private readonly SpaDbContext _context;
        private readonly INotificador _notificador;

        public CitaService(SpaDbContext context, INotificador notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        public async Task<Cita> AgendarCitaAsync(
            Cliente cliente,
            Servicio servicio,
            DateTime fecha,
            int cantidadPersonas,
            ICalculadorDescuento estrategiaDescuento)
        {
            // OCP: El cálculo del total se delega a la estrategia
            decimal totalCalculado = estrategiaDescuento.CalcularTotal(servicio, cantidadPersonas);

            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Dni == cliente.Dni);

            if (clienteExistente == null)
            {
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                clienteExistente = cliente;
            }

            var nuevaCita = new Cita
            {
                ClienteId = clienteExistente.Id,
                ServicioId = servicio.Id,
                FechaHora = fecha,
                CantidadPersonas = cantidadPersonas,
                TotalFinal = totalCalculado,
                Confirmada = true
            };

            _context.Citas.Add(nuevaCita);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EsErrorDeHorarioDuplicado(ex))
            {
                throw new CitaDuplicadaException(
                    "Este horario ya fue reservado por otro cliente. Por favor elige otro horario.");
            }

            nuevaCita.Cliente = clienteExistente;
            nuevaCita.Servicio = servicio;

            // SRP: El servicio no arma textos ni canales de envío, delega la notificación
            _notificador.EnviarConfirmacion(nuevaCita);

            return nuevaCita;
        }

        // Código 1062 de MySQL = "Duplicate entry" (choca con el índice único)
        private static bool EsErrorDeHorarioDuplicado(DbUpdateException ex)
            => ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062;
    }

    public class CitaDuplicadaException : Exception
    {
        public CitaDuplicadaException(string message) : base(message) { }
    }
}