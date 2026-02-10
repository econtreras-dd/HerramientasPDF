using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HerramientasPDF.Controllers
{
    public class ComprimirController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Procesar(IFormFile archivo, string nivel)
        {
            if (archivo == null || archivo.Length == 0) return BadRequest("Archivo no válido.");

            string guid = Guid.NewGuid().ToString();
            string carpetaTrabajo = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(carpetaTrabajo);

            string rutaEntrada = Path.Combine(carpetaTrabajo, "entrada.pdf");
            string rutaSalida = Path.Combine(carpetaTrabajo, "comprimido.pdf");

            try
            {
                using (var stream = new FileStream(rutaEntrada, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // --- MEJORA AQUÍ: Argumentos para forzar archivos médicos y escaneados ---
                // Agregamos el remuestreo a 150 DPI para que los escaneos pesados realmente bajen
                string argumentos = $"-sDEVICE=pdfwrite -dCompatibilityLevel=1.4 -dPDFSETTINGS=/{nivel} " +
                                   "-dNOPAUSE -dQUIET -dBATCH " +
                                   "-dDownsampleColorImages=true -dColorImageResolution=150 " +
                                   "-dDownsampleGrayImages=true -dGrayImageResolution=150 " +
                                   "-dDownsampleMonoImages=true -dMonoImageResolution=150 " +
                                   $"-sOutputFile=\"{rutaSalida}\" \"{rutaEntrada}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "gs",
                    Arguments = argumentos, // Usamos la variable con los nuevos parámetros
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proceso = Process.Start(startInfo))
                {
                    if (proceso == null) throw new Exception("No se pudo iniciar Ghostscript.");

                    string error = await proceso.StandardError.ReadToEndAsync();
                    proceso.WaitForExit();

                    if (proceso.ExitCode != 0)
                    {
                        return BadRequest($"Error en motor: {error}");
                    }
                }

                if (System.IO.File.Exists(rutaSalida))
                {
                    byte[] archivoBytes = await System.IO.File.ReadAllBytesAsync(rutaSalida);

                    // IMPORTANTE: Liberamos el archivo antes de intentar borrar la carpeta
                    long tamañoOriginal = archivo.Length;
                    long tamañoNuevo = archivoBytes.Length;

                    // Limpieza antes de entregar (Correcto)
                    if (Directory.Exists(carpetaTrabajo))
                        Directory.Delete(carpetaTrabajo, true);

                    return File(archivoBytes, "application/pdf", Path.GetFileNameWithoutExtension(archivo.FileName) + "_comprimido.pdf");
                }

                return BadRequest("El proceso terminó pero no generó el resultado.");
            }
            catch (Exception ex)
            {
                if (Directory.Exists(carpetaTrabajo)) Directory.Delete(carpetaTrabajo, true);
                return BadRequest($"Error interno: {ex.Message}");
            }
        }
    }
}