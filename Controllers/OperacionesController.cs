using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Controllers
{
    [Authorize]
    public class OperacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AlgoliaService _algolia;
        private readonly ILogger<OperacionesController> _logger;

        public OperacionesController(
            ApplicationDbContext context,
            AlgoliaService algolia,
            ILogger<OperacionesController> logger)
        {
            _context = context;
            _algolia = algolia;
            _logger = logger;
        }

        public async Task<IActionResult> Incidencias(string? q)
        {
            List<Incidencia> incidencias;

            var resultadosAlgolia = await _algolia.BuscarAsync(q);

            if (resultadosAlgolia != null)
            {
                // Algolia devolvió resultados: cruzar con SQLite para tener datos actualizados
                var ids = resultadosAlgolia.Select(r => r.Id).ToList();
                incidencias = await _context.Incidencias
                    .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
                    .ToListAsync();

                _logger.LogInformation("[ALGOLIA] Búsqueda '{Query}' → {Count} resultados.", q, incidencias.Count);
            }
            else
            {
                // Sin búsqueda: consulta normal a SQLite
                incidencias = await _context.Incidencias
                    .Where(i => i.Estado == "Abierta")
                    .ToListAsync();

                _logger.LogInformation("[SQLITE] Carga general → {Count} incidencias.", incidencias.Count);
            }

            // Pasar la query a la vista para mantener el texto en el buscador
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

                // Eliminar del índice de Algolia para que no aparezca en búsquedas futuras
                await _algolia.EliminarDelIndiceAsync(id);

                _logger.LogInformation("[CERRAR] Incidencia {Id} cerrada y eliminada de Algolia.", id);
            }
            return Ok();
        }
    }
}
