using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

public class PdfOperacionesController : Controller
{
    [HttpPost]
    public IActionResult RotarPdf(IFormFile archivo, int grados)
    {
        if (archivo == null) return BadRequest("No se seleccionó archivo.");

        using (var streamOriginal = archivo.OpenReadStream())
        {
            // Abrimos el PDF existente
            using (var documento = PdfReader.Open(streamOriginal, PdfDocumentOpenMode.Modify))
            {
                foreach (PdfPage pagina in documento.Pages)
                {
                    // La rotación en PDF debe ser en incrementos de 90
                    // Sumamos la rotación actual a la nueva para que sea acumulativa
                    int rotacionActual = pagina.Rotate;
                    pagina.Rotate = (rotacionActual + grados) % 360;
                }

                using (var ms = new MemoryStream())
                {
                    documento.Save(ms);
                    string nombreFinal = Path.GetFileNameWithoutExtension(archivo.FileName) + "_rotado.pdf";
                    return File(ms.ToArray(), "application/pdf", nombreFinal);
                }
            }
        }
    }
}