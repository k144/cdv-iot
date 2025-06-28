using Microsoft.AspNetCore.Mvc;

namespace AiVisionModel.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AiController : ControllerBase
    {
        [HttpPost("analyze")]
        public IActionResult AnalyzeImage([FromBody] byte[] imageData)
        {
            // Makieta: zwraca losową liczbę wolnych miejsc
            Random rand = new Random();
            int freeSpaces = rand.Next(0, 100); // Przykładowo od 0 do 100 wolnych miejsc
            return Ok(new { FreeSpaces = freeSpaces });
        }
    }
}