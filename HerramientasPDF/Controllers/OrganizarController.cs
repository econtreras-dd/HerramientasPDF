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

            try
            {
                var stream = archivo.OpenReadStream();
                using (var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
                {
                    var totalPaginas = pdf.PageCount;
                    return Ok(new { total = totalPaginas });
                }
            }
            catch (PdfReaderException)
            {
                // Si truena aquí es porque el archivo tiene clave, 
                // pero como ahora lo manejamos en el JS antes de llegar aquí, 
                // este método es casi un respaldo.
                return Unauthorized("El archivo está protegido.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Procesar(IFormFile archivo, List<int> orden, string password) // <-- Agregamos password
        {
            if (archivo == null || orden == null || orden.Count == 0) return BadRequest();

            try
            {
                using (var outPdf = new PdfDocument())
                using (var inStream = archivo.OpenReadStream())
                // PDFSharp permite pasar la contraseña como segundo o tercer parámetro dependiendo de la sobrecarga
                using (var inPdf = PdfReader.Open(inStream, password ?? "", PdfDocumentOpenMode.Import))
                {
                    // Reconstruimos el PDF en el orden solicitado
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
            catch (Exception ex)
            {
                // Si la contraseña enviada es incorrecta o el archivo está dañado
                return BadRequest("Error al procesar el PDF: " + ex.Message);
            }
        }
    }
}