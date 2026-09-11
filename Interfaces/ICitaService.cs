using System;
using System.Threading.Tasks;
using Spa.Models;

namespace Spa.Interfaces
{
    public interface ICitaService
    {
        Task<Cita> AgendarCitaAsync(
            Cliente cliente,
            Servicio servicio,
            DateTime fecha,
            int cantidadPersonas,
            ICalculadorDescuento estrategiaDescuento);
    }
}