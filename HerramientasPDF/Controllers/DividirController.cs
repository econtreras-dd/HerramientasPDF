using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO;

namespace HerramientasPDF.Controllers
{
    public class DividirController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Procesar(IFormFile archivo, string rango)
        {
            if (archivo == null || string.IsNullOrEmpty(rango))
                return BadRequest("Archivo o rango no válido.");

            string guid = Guid.NewGuid().ToString();
            string carpeta = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(carpeta);

            string rutaEntrada = Path.Combine(carpeta, "original.pdf");
            string rutaSalida = Path.Combine(carpeta, "extraido.pdf");

            try
            {
                using (var fs = new FileStream(rutaEntrada, FileMode.Create))
                {
                    await archivo.CopyToAsync(fs);
                }

                // --- LÓGICA DEL BISTURÍ ---
                using (PdfDocument documentoEntrada = PdfReader.Open(rutaEntrada, PdfDocumentOpenMode.Import))
                using (PdfDocument documentoSalida = new PdfDocument())
                {
                    // Convertimos el rango (ej: "1-3, 5") en una lista de números de página
                    var paginasSeleccionadas = InterpretarRango(rango, documentoEntrada.PageCount);

                    foreach (int n in paginasSeleccionadas)
                    {
                        // Las páginas en PdfSharp son base 0, por eso restamos 1
                        documentoSalida.AddPage(documentoEntrada.Pages[n - 1]);
                    }

                    documentoSalida.Save(rutaSalida);
                }

                byte[] bytes = await System.IO.File.ReadAllBytesAsync(rutaSalida);
                Directory.Delete(carpeta, true);

                return File(bytes, "application/pdf", "extraido.pdf");
            }
            catch (Exception ex)
            {
                if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
                return BadRequest($"Error al procesar el bisturí: {ex.Message}");
            }
        }

        // Función auxiliar para entender rangos como "1-5, 8, 10-12"
        private List<int> InterpretarRango(string rango, int totalPaginas)
        {
            var resultado = new List<int>();
            var partes = rango.Split(',');

            foreach (var parte in partes)
            {
                if (parte.Contains("-"))
                {
                    var limites = parte.Split('-');
                    int inicio = int.Parse(limites[0]);
                    int fin = int.Parse(limites[1]);
                    for (int i = inicio; i <= fin && i <= totalPaginas; i++) resultado.Add(i);
                }
                else
                {
                    int p = int.Parse(parte);
                    if (p <= totalPaginas) resultado.Add(p);
                }
            }
            return resultado.Distinct().OrderBy(x => x).ToList();
        }
    }
}