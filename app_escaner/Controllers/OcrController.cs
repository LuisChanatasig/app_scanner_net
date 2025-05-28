using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using Tesseract;

namespace app_escaner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        private static readonly Dictionary<string, Rectangle> zonasAbsolutas = new()
        {
            ["placa_actual"] = new Rectangle(60, 150, 110, 40),
            ["placa_anterior"] = new Rectangle(320, 155, 50, 20),
            ["anio"] = new Rectangle(530, 160, 100, 30),
            ["vin"] = new Rectangle(60, 220, 125, 14),
            ["numero_motor"] = new Rectangle(305, 220, 110, 20),
            ["ramv_cpn"] = new Rectangle(520, 220, 110, 20),
            ["marca"] = new Rectangle(90, 260, 120, 20),
            ["modelo"] = new Rectangle(240, 260, 200, 20),
            ["cilindraje"] = new Rectangle(500, 260, 50, 20),
            ["anio_modelo"] = new Rectangle(610, 260, 50, 20),
            ["clase_vehiculo"] = new Rectangle(90, 300, 120, 20),
            ["tipo_vehiculo"] = new Rectangle(250, 300, 200, 30),
            ["pasajeros"] = new Rectangle(510, 300, 10, 20),
            ["toneladas"] = new Rectangle(615, 300, 20, 20),
            ["pais_origen"] = new Rectangle(90, 350, 120, 20),
            ["combustible"] = new Rectangle(250, 350, 200, 20),
            ["carroceria"] = new Rectangle(470, 350, 90, 20),
            ["tipo_de_peso"] = new Rectangle(570, 350, 110, 20),
            ["color_1"] = new Rectangle(90, 390, 120, 20),
            ["color_2"] = new Rectangle(320, 390, 120, 20),
            ["ortopedico"] = new Rectangle(500, 390, 50, 20),
            ["remarcado"] = new Rectangle(610, 390, 30, 20),
            ["observaciones"] = new Rectangle(90, 425, 500, 30)
        };

        // Ruta fija de la imagen base para calcular zonas relativas
        private static readonly string rutaImagenBase =
            @"C:\Users\Saferisk-Fernando\source\repos\app_escaner\app_escaner\Imagen\Anverso_Matricula.jpg";

        private static Dictionary<string, RectangleF>? zonasRelativasCache;

        // Lazy load de zonas relativas
        private static Dictionary<string, RectangleF> ObtenerZonasRelativas()
        {
            if (zonasRelativasCache != null)
                return zonasRelativasCache;

            if (!System.IO.File.Exists(rutaImagenBase))
                throw new FileNotFoundException("Imagen base para referencia no encontrada", rutaImagenBase);

            using var imagen = new Bitmap(rutaImagenBase);
            float width = imagen.Width;
            float height = imagen.Height;

            zonasRelativasCache = new Dictionary<string, RectangleF>();
            foreach (var zona in zonasAbsolutas)
            {
                var abs = zona.Value;
                zonasRelativasCache[zona.Key] = new RectangleF(
                    abs.X / width,
                    abs.Y / height,
                    abs.Width / width,
                    abs.Height / height
                );
            }

            return zonasRelativasCache;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> ScanMatriculaDesdeUrl([FromBody] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("La URL no puede estar vacía.");

            try
            {
                var zonasRelativas = ObtenerZonasRelativas();

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                using var response = await httpClient.GetAsync(path);
                if (!response.IsSuccessStatusCode)
                    return BadRequest("No se pudo descargar la imagen desde la URL proporcionada.");

                await using var imageStream = await response.Content.ReadAsStreamAsync();
                using var img = new Bitmap(imageStream);

                var result = new Dictionary<string, string>();
                var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

                using var engine = new TesseractEngine(tessdataPath, "spa", EngineMode.Default);

                int width = img.Width;
                int height = img.Height;

                foreach (var zona in zonasRelativas)
                {
                    var rect = zona.Value;
                    var zonaReal = new Rectangle(
                        (int)(rect.X * width),
                        (int)(rect.Y * height),
                        (int)(rect.Width * width),
                        (int)(rect.Height * height)
                    );

                    using var subImg = img.Clone(zonaReal, img.PixelFormat);
                    using var pix = BitmapToPix(subImg);
                    using var page = engine.Process(pix, PageSegMode.SingleLine);
                    result[zona.Key] = page.GetText().Trim();
                }

                return Ok(result);
            }
            catch (FileNotFoundException fnfEx)
            {
                return StatusCode(500, new { error = fnfEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("scan-local-test")]
        public IActionResult ScanMatriculaDesdeRutaTest([FromBody] string path2)
        {
            if (string.IsNullOrWhiteSpace(path2) || !System.IO.File.Exists(path2))
                return BadRequest("La ruta del archivo no es válida o el archivo no existe.");

            try
            {
                var zonasRelativas = ObtenerZonasRelativas();

                using var img = new Bitmap(path2);
                var result = new Dictionary<string, string>();
                var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

                using var engine = new TesseractEngine(tessdataPath, "spa", EngineMode.Default);

                int width = img.Width;
                int height = img.Height;

                foreach (var zona in zonasRelativas)
                {
                    var rect = zona.Value;
                    var zonaReal = new Rectangle(
                        (int)(rect.X * width),
                        (int)(rect.Y * height),
                        (int)(rect.Width * width),
                        (int)(rect.Height * height)
                    );

                    using var subImg = img.Clone(zonaReal, img.PixelFormat);
                    using var pix = BitmapToPix(subImg);
                    using var page = engine.Process(pix, PageSegMode.SingleLine);
                    result[zona.Key] = page.GetText().Trim();
                }

                return Ok(result);
            }
            catch (FileNotFoundException fnfEx)
            {
                return StatusCode(500, new { error = fnfEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static Pix BitmapToPix(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return Pix.LoadFromMemory(stream.ToArray());
        }
    }
}
