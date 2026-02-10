using Microsoft.AspNetCore.Mvc;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace HerramientasPDF.Controllers
{
    public class ImagenesController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult ConvertirImagenAPdf(List<IFormFile> imagenes)
        {
            if (imagenes == null || imagenes.Count == 0) return BadRequest("No hay imágenes.");

            // Creamos el documento de PdfSharp
            PdfDocument documento = new PdfDocument();
            documento.Info.Title = "Convertido por DynaPDF v8";

            try
            {
                foreach (var archivo in imagenes)
                {
                    using (var stream = archivo.OpenReadStream())
                    {
                        // 1. Usamos ImageSharp para cargar la imagen y conocer sus dimensiones
                        using (var image = SixLabors.ImageSharp.Image.Load(stream))
                        {
                            // Creamos una página nueva en el PDF
                            PdfPage page = documento.AddPage();

                            // Ajustamos el tamaño de la página al tamaño de la imagen (en puntos)
                            // Nota: 72 puntos = 1 pulgada
                            page.Width = XUnit.FromPoint(image.Width);
                            page.Height = XUnit.FromPoint(image.Height);

                            using (XGraphics gfx = XGraphics.FromPdfPage(page))
                            {
                                // 2. Convertimos el stream de nuevo a un formato que PdfSharp entienda
                                // Re-embobinar el stream para leerlo desde el principio
                                stream.Position = 0;
                                using (XImage xImage = XImage.FromStream(stream))
                                {
                                    gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);
                                }
                            }
                        }
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    documento.Save(ms);
                    return File(ms.ToArray(), "application/pdf", "DynaPDF_V8.pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Procesar(List<IFormFile> imagenes, List<int> rotaciones)
        {
            if (imagenes == null || imagenes.Count == 0) return BadRequest("No hay imágenes.");

            // 1. Declaración fuera del try para que el finally pueda verla
            string carpetaTrabajo = Path.Combine("/tmp", Guid.NewGuid().ToString());

            try
            {
                // 2. Creamos la carpeta físicamente
                Directory.CreateDirectory(carpetaTrabajo);

                using (var documento = new PdfDocument())
                {
                    string nombreFinal = "fotos_a_pdf.pdf";

                    for (int i = 0; i < imagenes.Count; i++)
                    {
                        using (var imageStream = imagenes[i].OpenReadStream())
                        using (var image = await SixLabors.ImageSharp.Image.LoadAsync(imageStream))
                        {
                            int angulo = rotaciones.Count > i ? rotaciones[i] : 0;

                            if (imagenes.Count == 1)
                            {
                                string nombreSinExt = Path.GetFileNameWithoutExtension(imagenes[i].FileName);
                                string sufijo = angulo > 0 ? $"_rotado{angulo}" : "";
                                nombreFinal = $"{nombreSinExt}{sufijo}.pdf";
                            }

                            if (angulo != 0)
                            {
                                switch (angulo)
                                {
                                    case 90: image.Mutate(x => x.Rotate(RotateMode.Rotate90)); break;
                                    case 180: image.Mutate(x => x.Rotate(RotateMode.Rotate180)); break;
                                    case 270: image.Mutate(x => x.Rotate(RotateMode.Rotate270)); break;
                                }
                            }

                            using (var msTemp = new MemoryStream())
                            {
                                await image.SaveAsJpegAsync(msTemp, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 100 });
                                msTemp.Position = 0;
                                XImage xImg = XImage.FromStream(msTemp);

                                PdfPage pagina = documento.AddPage();
                                pagina.Width = XUnit.FromPoint(xImg.PointWidth);
                                pagina.Height = XUnit.FromPoint(xImg.PointHeight);
                                XGraphics gfx = XGraphics.FromPdfPage(pagina);
                                gfx.DrawImage(xImg, 0, 0);
                            }
                        }
                    }

                    using (var msFinal = new MemoryStream())
                    {
                        documento.Save(msFinal);
                        return File(msFinal.ToArray(), "application/pdf", nombreFinal);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error procesando imágenes: {ex.Message}");
            }
            finally
            {
                // 3. Limpieza atómica garantizada para Azure
                if (!string.IsNullOrEmpty(carpetaTrabajo) && Directory.Exists(carpetaTrabajo))
                {
                    try
                    {
                        Directory.Delete(carpetaTrabajo, true);
                    }
                    catch (IOException)
                    {
                        // Pequeño reintento si Azure tiene el archivo bloqueado un instante
                        Task.Delay(500).Wait();
                        try { Directory.Delete(carpetaTrabajo, true); } catch { }
                    }
                    catch (Exception) { /* Silencio para no interrumpir la descarga */ }
                }
            }
        }
    }
}