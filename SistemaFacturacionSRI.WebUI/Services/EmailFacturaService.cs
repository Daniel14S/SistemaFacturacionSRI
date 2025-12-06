using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Implementación que usa la API HTTP de Resend para enviar PDF/XML de la factura.
    /// </summary>
    public class EmailFacturaService : IEmailFacturaService
    {
        private readonly HttpClient _httpClient;
        private readonly IFacturaService _facturaService;
        private readonly IPdfGeneratorService _pdfGeneratorService;
        private readonly IXmlGeneratorService _xmlGeneratorService;
        private readonly ResendOptions _options;
        private readonly ILogger<EmailFacturaService> _logger;

        public EmailFacturaService(
            HttpClient httpClient,
            IFacturaService facturaService,
            IPdfGeneratorService pdfGeneratorService,
            IXmlGeneratorService xmlGeneratorService,
            IOptions<ResendOptions> options,
            ILogger<EmailFacturaService> logger)
        {
            _httpClient = httpClient;
            _facturaService = facturaService;
            _pdfGeneratorService = pdfGeneratorService;
            _xmlGeneratorService = xmlGeneratorService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task EnviarFacturaAsync(int facturaId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.From))
            {
                _logger.LogWarning("Resend no configurado: falta ApiKey o From. Se omite envío de correo.");
                return;
            }

            var factura = await _facturaService.ObtenerPorIdAsync(facturaId, cancellationToken)
                ?? throw new KeyNotFoundException($"Factura {facturaId} no encontrada para envío de correo");

            var destinatario = factura.Cliente?.Email;
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                _logger.LogWarning("Factura {FacturaId} no tiene email de cliente, no se envía correo", facturaId);
                return;
            }

            // Generar PDF
            var pdfBytes = await _pdfGeneratorService.GenerarRideBytesAsync(facturaId);

            // Obtener XML existente o generarlo en memoria
            byte[] xmlBytes;
            if (!string.IsNullOrWhiteSpace(factura.XmlPath))
            {
                var xmlAbsolutePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "wwwroot",
                    factura.XmlPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (File.Exists(xmlAbsolutePath))
                {
                    xmlBytes = await File.ReadAllBytesAsync(xmlAbsolutePath, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("XML no encontrado en {Path}, se generará en memoria", factura.XmlPath);
                    var xmlContent = _xmlGeneratorService.GenerarXmlFactura(factura);
                    xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
                }
            }
            else
            {
                var xmlContent = _xmlGeneratorService.GenerarXmlFactura(factura);
                xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
            }

            var payload = new
            {
                from = _options.From,
                to = new[] { destinatario },
                subject = $"Factura electrónica {factura.NumeroFactura}",
                text = "Adjuntamos su factura electrónica en PDF y XML.",
                attachments = new[]
                {
                    new
                    {
                        filename = $"RIDE_{factura.NumeroFactura}.pdf",
                        content = Convert.ToBase64String(pdfBytes)
                    },
                    new
                    {
                        filename = $"XML_{factura.NumeroFactura}.xml",
                        content = Convert.ToBase64String(xmlBytes)
                    }
                }
            };

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("emails", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend devolvió error {Status}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"No se pudo enviar el correo de factura {factura.NumeroFactura}. Estado: {response.StatusCode}");
            }

            _logger.LogInformation("Correo de factura {FacturaId} enviado a {Destinatario}", facturaId, destinatario);
        }
    }
}
