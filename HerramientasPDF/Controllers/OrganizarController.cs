using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace HerramientasPDF.Controllers
{
    public class OrganizarController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult ObtenerPaginas(IFormFile archivo)
        {
            if (archivo == null) return BadRequest("Archivo no válido.");

            // Guardamos temporalmente para contar páginas
            var stream = archivo.OpenReadStream();
            using (var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
            {
                var totalPaginas = pdf.PageCount;
                // Devolvemos la cantidad de páginas para que el JS sepa cuántas miniaturas pedir
                return Ok(new { total = totalPaginas });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Procesar(IFormFile archivo, List<int> orden)
        {
            if (archivo == null || orden == null || orden.Count == 0) return BadRequest();

            using (var outPdf = new PdfDocument())
            using (var inStream = archivo.OpenReadStream())
            using (var inPdf = PdfReader.Open(inStream, PdfDocumentOpenMode.Import))
            {
                // Reconstruimos el PDF en el orden solicitado por el usuario
                foreach (int indice in orden)
                {
                    outPdf.AddPage(inPdf.Pages[indice]);
                }

                using (var ms = new MemoryStream())
                {
                    outPdf.Save(ms);
                    return File(ms.ToArray(), "application/pdf", "reorganizado.pdf");
                }
            }
        }
    }
}