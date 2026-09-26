using Algolia.Search.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlataformaIncidencias.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Services
{
    public class AlgoliaService
    {
        private readonly SearchClient _client;
        private readonly string _indexName;
        private readonly ILogger<AlgoliaService> _logger;

        public AlgoliaService(IConfiguration configuration, ILogger<AlgoliaService> logger)
        {
            _logger = logger;
            var appId = configuration["Algolia:AppId"]!;
            // WriteApiKey → solo servidor, nunca expuesta al frontend
            var writeKey = configuration["Algolia:WriteApiKey"]!;
            _indexName = configuration["Algolia:IndexName"] ?? "incidencias";

            _client = new SearchClient(appId, writeKey);
        }

        /// <summary>
        /// Indexa o actualiza una incidencia en Algolia.
        /// </summary>
        public async Task IndexarIncidenciaAsync(Incidencia incidencia)
        {
            var objeto = MapearAAlgolia(incidencia);
            await _client.SaveObjectAsync(_indexName, objeto);
            _logger.LogInformation("[ALGOLIA] Indexada incidencia Id={Id}", incidencia.Id);
        }

        /// <summary>
        /// Indexa múltiples incidencias en lote (usado por el Seeder).
        /// </summary>
        public async Task IndexarTodasAsync(IEnumerable<Incidencia> incidencias)
        {
            var objetos = incidencias.Select(MapearAAlgolia).ToList();
            await _client.SaveObjectsAsync(_indexName, objetos);
            _logger.LogInformation("[ALGOLIA] Indexadas {Count} incidencias en lote.", objetos.Count);
        }

        /// <summary>
        /// Busca incidencias abiertas en Algolia usando el texto de búsqueda.
        /// Retorna null si la búsqueda está vacía (para que el controlador use SQLite directo).
        /// </summary>
        public async Task<List<IncidenciaAlgolia>?> BuscarAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var result = await _client.SearchSingleIndexAsync<IncidenciaAlgolia>(
                _indexName,
                new Algolia.Search.Models.Search.SearchParams(
                    new Algolia.Search.Models.Search.SearchParamsObject
                    {
                        Query = query,
                        // Filtrar solo incidencias abiertas desde la consulta
                        Filters = "Estado:Abierta"
                    }
                )
            );

            _logger.LogInformation("[ALGOLIA] Búsqueda '{Query}' devolvió {Count} resultados.", query, result.Hits.Count);
            return result.Hits;
        }

        /// <summary>
        /// Elimina una incidencia del índice (al cerrarla).
        /// </summary>
        public async Task EliminarDelIndiceAsync(int id)
        {
            await _client.DeleteObjectAsync(_indexName, id.ToString());
            _logger.LogInformation("[ALGOLIA] Eliminada del índice incidencia Id={Id}", id);
        }

        private static IncidenciaAlgolia MapearAAlgolia(Incidencia i) => new()
        {
            ObjectID = i.Id.ToString(),
            Id = i.Id,
            Estacion = i.Estacion,
            Descripcion = i.Descripcion,
            Prioridad = i.Prioridad,
            Estado = i.Estado
        };
    }
}
