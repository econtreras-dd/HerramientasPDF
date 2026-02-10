using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HerramientasPDF.Controllers
{
    public class ConvertirController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Procesar(IFormFile archivo)
        {
            // 1. Validación de existencia
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo no válido.");

            // 2. Validación de extensiones permitidas (Filtro de Seguridad)
            var extensionesPermitidas = new[] { ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt" };
            var extensionActual = Path.GetExtension(archivo.FileName).ToLower();

            if (!extensionesPermitidas.Contains(extensionActual))
            {
                return BadRequest("Formato no soportado. Por favor, suba solo archivos de Word, Excel o PowerPoint.");
            }

            string idTransaccion = Guid.NewGuid().ToString();
            // Usamos una ruta que siempre tiene permisos en Docker
            string carpetaTrabajo = Path.Combine("/tmp", idTransaccion);
            Directory.CreateDirectory(carpetaTrabajo);

            try
            {
                string nombreSinExt = Path.GetFileNameWithoutExtension(archivo.FileName);
                string rutaEntrada = Path.Combine(carpetaTrabajo, archivo.FileName);
                string rutaSalida = Path.Combine(carpetaTrabajo, nombreSinExt + ".pdf");

                using (var stream = new FileStream(rutaEntrada, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // --- MOTOR DE CONVERSIÓN BLINDADO ---
                var startInfo = new ProcessStartInfo
                {
                    FileName = "soffice",
                    // Cambiamos el perfil a una ruta interna para evitar el error de permisos
                    Arguments = $"--headless --nologo --nodefault --norestore --nocrashreport --nofirststartwizard \"-env:UserInstallation=file://{carpetaTrabajo}/.user\" --convert-to pdf \"{rutaEntrada}\" --outdir \"{carpetaTrabajo}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proceso = Process.Start(startInfo))
                {
                    // Le damos 30 segundos. Si no termina, es que el motor está bloqueado
                    if (proceso == null || !proceso.WaitForExit(30000))
                    {
                        proceso?.Kill();
                        return BadRequest("El motor de conversión no responde. Por favor, reinicie el contenedor desde Docker Desktop.");
                    }
                }

                if (System.IO.File.Exists(rutaSalida))
                {
                    byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(rutaSalida);
                    return File(pdfBytes, "application/pdf", $"{nombreSinExt}.pdf");
                }

                return BadRequest("LibreOffice no pudo generar el PDF. Verifique que el archivo no esté protegido.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error crítico: {ex.Message}");
            }
            finally
            {
                // Limpieza absoluta de la transacción
                if (Directory.Exists(carpetaTrabajo))
                {
                    try { Directory.Delete(carpetaTrabajo, true); } catch { }
                }
            }
        }
    }
}