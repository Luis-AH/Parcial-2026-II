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
        private readonly PieSocketService _pieSocket;
        private readonly ILogger<OperacionesController> _logger;

        public OperacionesController(
            ApplicationDbContext context,
            PieSocketService pieSocket,
            ILogger<OperacionesController> logger)
        {
            _context   = context;
            _pieSocket = pieSocket;
            _logger    = logger;
        }

        public async Task<IActionResult> Incidencias()
        {
            var incidencias = await _context.Incidencias
                .Where(i => i.Estado == "Abierta")
                .ToListAsync();

            _logger.LogInformation("[SQLITE] {Count} incidencias abiertas cargadas.", incidencias.Count);
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

                // Publicar evento WebSocket via PieSocket para que todos los clientes
                // conectados eliminen la fila en tiempo real sin recargar la página
                await _pieSocket.PublicarIncidenciaCerradaAsync(id);

                _logger.LogInformation("[PIESOCKET] Evento publicado para incidencia {Id}.", id);
            }
            return Ok();
        }
    }
}
