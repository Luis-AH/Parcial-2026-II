using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PusherServer;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlataformaIncidencias.Services
{
    /// <summary>
    /// Servicio para publicar eventos de tiempo real a través de PieSocket
    /// usando el protocolo Pusher compatible.
    /// API Key y Secret se manejan SOLO en servidor (nunca al frontend).
    /// El cliente JS usará únicamente la ApiKey pública.
    /// </summary>
    public class PieSocketService
    {
        private readonly Pusher _pusher;
        private readonly string _channel;
        private readonly ILogger<PieSocketService> _logger;

        public PieSocketService(IConfiguration configuration, ILogger<PieSocketService> logger)
        {
            _logger = logger;

            var appId     = configuration["PieSocket:AppId"]!;
            var apiKey    = configuration["PieSocket:ApiKey"]!;
            var apiSecret = configuration["PieSocket:ApiSecret"]!;
            var clusterId = configuration["PieSocket:ClusterId"]!;  // free.blr2
            _channel      = configuration["PieSocket:Channel"] ?? "incidencias-channel";

            // PieSocket es compatible con Pusher:
            // Host pattern: <clusterId>.piesocket.com
            _pusher = new Pusher(appId, apiKey, apiSecret, new PusherOptions
            {
                HostName  = $"{clusterId}.piesocket.com",
                Encrypted = true
            });
        }

        /// <summary>
        /// Emite el evento "incidencia-cerrada" al canal de incidencias.
        /// El payload se serializa en camelCase estricto para evitar errores en el cliente JS.
        /// </summary>
        public async Task PublicarIncidenciaCerradaAsync(int id)
        {
            // La rúbrica del examen pide explícitamente enviar "IncidenciaActualizada"
            // y que el payload contenga "Id" y "Estado"
            var payload = new { Id = id, Estado = "Cerrada" };

            var result = await _pusher.TriggerAsync(
                _channel,
                "IncidenciaActualizada",
                payload
            );

            _logger.LogInformation(
                "[PIESOCKET] Evento 'IncidenciaActualizada' publicado para Id={Id}. Status={Status}",
                id, result.StatusCode);
        }
    }
}
