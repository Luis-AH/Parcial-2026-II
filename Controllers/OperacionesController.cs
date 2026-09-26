using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Controllers
{
    // Solo usuarios autenticados (supervisor)
    [Authorize]
    public class OperacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OperacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Incidencias()
        {
            // Base inicial: solo mostrar todas (después en las ramas A y B se agrega Algolia y Redis)
            var incidencias = await _context.Incidencias.ToListAsync();
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
                // Aquí más adelante se publicará a RabbitMQ y/o PieHost
            }
            return Ok();
        }
    }
}
