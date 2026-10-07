using Microsoft.EntityFrameworkCore;
using Spa.Data;
using Spa.Interfaces;
using Spa.Models;

namespace Spa.Services
{
    public class PromocionService : IPromocionService
    {
        private readonly SpaDbContext _context;

        public PromocionService(SpaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Promocion>> ObtenerTodasAsync()
        {
            var promociones = await _context.Promociones.ToListAsync();

            var hoy = DateTime.Today;
            bool huboCambios = false;

            foreach (var promo in promociones)
            {
                if (promo.FechaFin.HasValue &&
                    promo.FechaFin.Value.Date < hoy &&
                    promo.Activa)
                {
                    promo.Activa = false;
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                await _context.SaveChangesAsync();
            }

            return promociones;
        }

        public async Task<Promocion?> ObtenerPorIdAsync(int id)
            => await _context.Promociones.FindAsync(id);

        public async Task CrearAsync(Promocion promocion)
        {
            var hoy = DateTime.Today;

            if (promocion.FechaFin.HasValue &&
                promocion.FechaFin.Value.Date < hoy)
            {
                promocion.Activa = false;
            }

            _context.Promociones.Add(promocion);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Promocion promocion)
        {
            var hoy = DateTime.Today;

            if (promocion.FechaFin.HasValue &&
                promocion.FechaFin.Value.Date < hoy)
            {
                promocion.Activa = false;
            }

            _context.Promociones.Update(promocion);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var promo = await _context.Promociones.FindAsync(id);

            if (promo != null)
            {
                _context.Promociones.Remove(promo);
                await _context.SaveChangesAsync();
            }
        }
    }
}