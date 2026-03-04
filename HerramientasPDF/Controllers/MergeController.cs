using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

public class MergeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Procesar(List<IFormFile> archivos)
    {
        if (archivos == null || archivos.Count == 0) return BadRequest("No hay archivos.");

        using (var documentoFinal = new PdfDocument())
        {
            foreach (var archivo in archivos)
            {
                var extension = Path.GetExtension(archivo.FileName).ToLower();
                using (var ms = new MemoryStream())
                {
                    await archivo.CopyToAsync(ms);
                    ms.Position = 0;

                    if (extension == ".pdf")
                    {
                        using (var importado = PdfReader.Open(ms, PdfDocumentOpenMode.Import))
                        {
                            foreach (var pagina in importado.Pages) documentoFinal.AddPage(pagina);
                        }
                    }
                    else if (new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
                    {
                        var pagina = documentoFinal.AddPage();
                        using (var imagen = XImage.FromStream(ms))
                        {
                            var gfx = XGraphics.FromPdfPage(pagina);
                            // Ajuste proporcional básico
                            double ancho = pagina.Width.Point - 40;
                            double alto = (imagen.PixelHeight * ancho) / imagen.PixelWidth;
                            gfx.DrawImage(imagen, 20, 20, ancho, alto);
                        }
                    }
                }
            }

            var streamSalida = new MemoryStream();
            documentoFinal.Save(streamSalida);
            return File(streamSalida.ToArray(), "application/pdf", "Unificado.pdf");
        }
    }
}