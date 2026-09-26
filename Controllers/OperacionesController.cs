using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Controllers
{
    [Authorize]
    public class OperacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OperacionesController> _logger;

        // Clave única con namespace para no chocar con otras apps en la misma DB Redis
        private const string CacheKey = "incidencias_abiertas";

        public OperacionesController(
            ApplicationDbContext context,
            IDistributedCache cache,
            ILogger<OperacionesController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IActionResult> Incidencias()
        {
            List<Incidencia> incidencias;

            // Intentar leer desde Redis
            var cached = await _cache.GetStringAsync(CacheKey);

            if (cached != null)
            {
                // HIT: datos desde Redis, sin tocar SQLite
                incidencias = JsonSerializer.Deserialize<List<Incidencia>>(cached)
                              ?? new List<Incidencia>();
                _logger.LogInformation("[REDIS] HIT — {Count} incidencias servidas desde caché.", incidencias.Count);
            }
            else
            {
                // MISS: consultar SQLite y guardar en Redis por 60 segundos
                incidencias = await _context.Incidencias
                    .Where(i => i.Estado == "Abierta")
                    .ToListAsync();

                var serializado = JsonSerializer.Serialize(incidencias);
                await _cache.SetStringAsync(CacheKey, serializado, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = System.TimeSpan.FromSeconds(60)
                });

                _logger.LogInformation("[SQLITE] MISS — {Count} incidencias cargadas de BD y guardadas en Redis.", incidencias.Count);
            }

            return View(incidencias);
        }

        [HttpPost]
        public async Task<IActionResult> Cerrar(int id)
        {
            var incidencia = await _context.Incidencias.FindAsync(id);
            if (incidencia != null)
            {
                incidencia.Estado = "Cerrada";
                await _context.SaveChangesAsync();

                // Invalidar la caché inmediatamente para que el listado esté fresco
                await _cache.RemoveAsync(CacheKey);
                _logger.LogInformation("[REDIS] Caché invalidada al cerrar incidencia {Id}.", id);
            }
            return Ok();
        }
    }
}
