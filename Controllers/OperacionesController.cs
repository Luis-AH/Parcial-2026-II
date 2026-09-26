using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Controllers
{
    [Authorize]
    public class OperacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AlgoliaService _algolia;
        private readonly IDistributedCache _cache;
        private readonly PieSocketService _pieSocket;
        private readonly ILogger<OperacionesController> _logger;

        private const string CacheKey = "incidencias_abiertas";

        public OperacionesController(
            ApplicationDbContext context,
            AlgoliaService algolia,
            IDistributedCache cache,
            PieSocketService pieSocket,
            ILogger<OperacionesController> logger)
        {
            _context   = context;
            _algolia   = algolia;
            _cache     = cache;
            _pieSocket = pieSocket;
            _logger    = logger;
        }

        public async Task<IActionResult> Incidencias(string? q)
        {
            List<Incidencia> incidencias;

            if (!string.IsNullOrWhiteSpace(q))
            {
                try
                {
                    // Búsqueda activa → Algolia
                    var resultadosAlgolia = await _algolia.BuscarAsync(q);
                    if (resultadosAlgolia != null)
                    {
                        var ids = resultadosAlgolia.Select(r => r.Id).ToList();
                        incidencias = await _context.Incidencias
                            .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
                            .ToListAsync();
                        _logger.LogInformation("[ALGOLIA] Búsqueda '{Q}' → {Count} resultados.", q, incidencias.Count);
                    }
                    else
                    {
                        incidencias = new List<Incidencia>();
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error conectando con Algolia");
                    // Fallback si falla Algolia: búsqueda básica local
                    incidencias = await _context.Incidencias
                        .Where(i => i.Estado == "Abierta" && (i.Estacion.Contains(q) || i.Descripcion.Contains(q)))
                        .ToListAsync();
                }
            }
            else
            {
                try
                {
                    // Sin búsqueda → Redis primero
                    var cached = await _cache.GetStringAsync(CacheKey);
                    if (cached != null)
                    {
                        incidencias = JsonSerializer.Deserialize<List<Incidencia>>(cached) ?? new List<Incidencia>();
                        _logger.LogInformation("[REDIS] HIT — {Count} incidencias desde caché.", incidencias.Count);
                    }
                    else
                    {
                        incidencias = await _context.Incidencias
                            .Where(i => i.Estado == "Abierta")
                            .ToListAsync();

                        await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(incidencias),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = System.TimeSpan.FromSeconds(60)
                            });
                        _logger.LogInformation("[SQLITE] MISS — {Count} incidencias guardadas en Redis.", incidencias.Count);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error conectando con Redis Cache");
                    // Fallback si falla Redis: ir directo a SQLite
                    incidencias = await _context.Incidencias
                        .Where(i => i.Estado == "Abierta")
                        .ToListAsync();
                }
            }

            ViewBag.Query = q;
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

                // 1. Invalidar caché Redis
                try
                {
                    await _cache.RemoveAsync(CacheKey);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "[CERRAR] Error en Redis");
                }

                // 2. Eliminar del índice Algolia
                try
                {
                    await _algolia.EliminarDelIndiceAsync(id);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "[CERRAR] Error en Algolia");
                }

                // 3. Publicar evento WebSocket via PieSocket
                try
                {
                    await _pieSocket.PublicarIncidenciaCerradaAsync(id);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "[CERRAR] Error en PieSocket");
                }

                _logger.LogInformation("[CERRAR] Procesamiento de cierre completado para Id={Id}", id);
            }
            return Ok();
        }
    }
}
