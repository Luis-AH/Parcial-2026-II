using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PlataformaIncidencias.Services
{
    public class PieSocketService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PieSocketService> _logger;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _clusterId;
        private readonly string _channel;

        public PieSocketService(IConfiguration configuration, ILogger<PieSocketService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _apiKey = configuration["PieSocket:ApiKey"] ?? string.Empty;
            _apiSecret = configuration["PieSocket:ApiSecret"] ?? string.Empty;
            _clusterId = configuration["PieSocket:ClusterId"] ?? "free.blr2";
            _channel = configuration["PieSocket:Channel"] ?? "incidencias-channel";
        }

        public async Task PublicarIncidenciaCerradaAsync(int id)
        {
            var url = $"https://{_clusterId}.piesocket.com/api/publish";

            var payload = new
            {
                key = _apiKey,
                secret = _apiSecret,
                roomId = _channel,
                message = new {
                    @event = "IncidenciaActualizada", // Ojo con la arroba porque event es palabra reservada
                    data = new { Id = id, Estado = "Cerrada" }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[PIESOCKET] Evento 'IncidenciaActualizada' publicado exitosamente vía API REST.");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("[PIESOCKET] Error al publicar. Status: {Status}, Body: {Body}", response.StatusCode, body);
            }
        }
    }
}
