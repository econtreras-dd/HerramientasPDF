using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Text.Json;
using HerramientasPDF.Helpers;

namespace HerramientasPDF.Controllers
{
    public class PdfRotarController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Procesar(IFormFile archivo, string instruccionesJson)
        {
            if (archivo == null || string.IsNullOrEmpty(instruccionesJson))
                return BadRequest("Datos incompletos.");

            // Recibimos un JSON: { "1": 90, "2": 0, "3": 270 ... }
            var rotaciones = JsonSerializer.Deserialize<Dictionary<string, int>>(instruccionesJson);

            try
            {
                using (var stream = archivo.OpenReadStream())
                using (var documento = PdfReader.Open(stream, PdfDocumentOpenMode.Modify))
                {
                    foreach (var item in rotaciones)
                    {
                        int indicePagina = int.Parse(item.Key) - 1; // Base 0
                        int gradosExtra = item.Value;

                        if (indicePagina < documento.PageCount && gradosExtra != 0)
                        {
                            PdfPage pagina = documento.Pages[indicePagina];
                            pagina.Rotate = (pagina.Rotate + gradosExtra) % 360;
                        }
                    }

                    using (var ms = new MemoryStream())
                    {
                        documento.Save(ms);
                        string ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "IP Desconocida";

                        // Registramos la acción
                        Auditoria.Registrar("Rotar PDF", ip);
                        return File(ms.ToArray(), "application/pdf", "documento_rotado.pdf");
                    }
                }

            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}