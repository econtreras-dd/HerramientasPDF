using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace HerramientasPDF.Controllers
{
    public class ProtegerController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Procesar(IFormFile archivo, string clave)
        {
            if (archivo == null || string.IsNullOrEmpty(clave))
                return BadRequest("Archivo o contraseña no válidos.");

            try
            {
                using (var inStream = archivo.OpenReadStream())
                using (var pdf = PdfReader.Open(inStream, PdfDocumentOpenMode.Modify))
                {
                    // 1. Asignamos la contraseña de apertura
                    pdf.SecuritySettings.UserPassword = clave;

                    // 2. Si las propiedades Allow... te dan error, intenta con esta sintaxis:
                    // pdf.SecuritySettings.PermitPrint = false; 
                    // pdf.SecuritySettings.PermitExtractContent = false;

                    // Si lo anterior sigue fallando, simplemente comenta las restricciones de permisos.
                    // Con UserPassword el archivo ya queda cifrado y pide clave al abrir.

                    using (var ms = new MemoryStream())
                    {
                        pdf.Save(ms);
                        return File(ms.ToArray(), "application/pdf", "protegido_test.pdf");
                    }
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest("Error al proteger el archivo: " + ex.Message);
            }
        }
    }
}