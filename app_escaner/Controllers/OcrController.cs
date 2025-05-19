using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using Tesseract;

namespace TuProyectoOCR.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        private static readonly Dictionary<string, Rectangle> zonas = new()
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

        [HttpPost("scan")]
        public IActionResult ScanMatriculaDesdeRuta([FromBody] string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return BadRequest("La ruta del archivo no es válida o el archivo no existe.");

            try
            {
                using var img = new Bitmap(path);
                var result = new Dictionary<string, string>();

                var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

                using var engine = new TesseractEngine(tessdataPath, "spa", EngineMode.Default);

                foreach (var zona in zonas)
                {
                    using var subImg = img.Clone(zona.Value, img.PixelFormat);
                    using var pix = BitmapToPix(subImg);
                    using var page = engine.Process(pix, PageSegMode.SingleLine);
                    result[zona.Key] = page.GetText().Trim();
                }

                return Ok(result);
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
