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
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000, ValueCountLimit = 2000)] // Subí a 2000 para estar sobrados
        public async Task<IActionResult> Procesar(List<IFormFile> archivos)
        {
            if (archivos == null || archivos.Count < 2)
                return BadRequest("Por favor, selecciona al menos dos archivos PDF.");

            using (var documentoFinal = new PdfDocument())
            {
                foreach (var archivo in archivos)
                {
                    using (var stream = new MemoryStream())
                    {
                        await archivo.CopyToAsync(stream);
                        // Abrimos el PDF para importar sus páginas
                        var pdfImportado = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                        foreach (var pagina in pdfImportado.Pages)
                        {
                            documentoFinal.AddPage(pagina);
                        }
                    }
                }

                using (var resultado = new MemoryStream())
                {
                    documentoFinal.Save(resultado);
                    return File(resultado.ToArray(), "application/pdf", "PDF_Unido_Empresarial.pdf");
                }
            }
        }
    }
}