using System;
using System.IO;
using System.Linq;

namespace HerramientasPDF.Helpers
{
    public static class Auditoria
    {
        private static readonly object _bloqueoArchivo = new object();

        public static void Registrar(string accion, string ip)
        {
            try
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, "auditoria.csv");
                string linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{ip};{accion}{Environment.NewLine}";

                lock (_bloqueoArchivo)
                {
                    File.AppendAllText(filePath, linea);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG ERROR] {ex.Message}");
            }
        }
    }
}