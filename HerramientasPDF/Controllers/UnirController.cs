using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace HerramientasPDF.Controllers
{
    public class UnirController : Controller
    {
        // Esta acción muestra la página con el formulario
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000, ValueCountLimit = 5000)]
        public async Task<IActionResult> Procesar(List<IFormFile> archivos, List<string> passwords)
        {
            if (archivos == null || archivos.Count < 2)
                return BadRequest("Por favor, selecciona al menos dos archivos PDF.");

            // Variable para rastrear el índice actual y emparejar con su contraseña
            int indiceActual = 0;

            using (var documentoFinal = new PdfDocument())
            {
                foreach (var archivo in archivos)
                {
                    // Extraemos la contraseña correspondiente a este archivo específico
                    // Si la lista es nula o el índice no existe, usamos cadena vacía
                    string clave = (passwords != null && indiceActual < passwords.Count)
                                   ? passwords[indiceActual]
                                   : string.Empty;

                    using (var stream = new MemoryStream())
                    {
                        await archivo.CopyToAsync(stream);
                        stream.Position = 0; // Resetear posición para lectura segura

                        try
                        {
                            // ¡CLAVE CORREGIDA!: Aquí pasamos la contraseña que viene de la vista
                            using (var pdfImportado = PdfReader.Open(stream, clave, PdfDocumentOpenMode.Import))
                            {
                                foreach (var pagina in pdfImportado.Pages)
                                {
                                    documentoFinal.AddPage(pagina);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Si la clave falló en el servidor, devolvemos error específico del archivo
                            return BadRequest($"No se pudo abrir '{archivo.FileName}'. Verifique la contraseña.");
                        }
                    }
                    indiceActual++; // Pasamos al siguiente archivo y su clave
                }

                using (var resultado = new MemoryStream())
                {
                    documentoFinal.Save(resultado, false); // false para no cerrar el stream prematuramente
                    return File(resultado.ToArray(), "application/pdf", "PDF_Unido_Empresarial.pdf");
                }
            }
        }
    }
}