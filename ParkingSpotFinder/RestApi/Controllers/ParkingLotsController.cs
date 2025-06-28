using Microsoft.AspNetCore.Mvc;
using Database.Models;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParkingLotsController : ControllerBase
    {
        private static List<ParkingLot> _parkingLots = new List<ParkingLot>
        {
            new ParkingLot { Id = "1", Name = "Parking A", Location = "City Center", TotalParkingSpaces = 100 },
            new ParkingLot { Id = "2", Name = "Parking B", Location = "Shopping Mall", TotalParkingSpaces = 200 }
        };

        [HttpGet]
        public ActionResult<IEnumerable<ParkingLot>> Get()
        {
            return Ok(_parkingLots);
        }

        [HttpGet("{id}")]
        public ActionResult<ParkingLot> Get(string id)
        {
            var parkingLot = _parkingLots.FirstOrDefault(p => p.Id == id);
            if (parkingLot == null)
            {
                return NotFound();
            }
            return Ok(parkingLot);
        }

        [HttpPost]
        public ActionResult<ParkingLot> Post([FromBody] ParkingLot parkingLot)
        {
            parkingLot.Id = Guid.NewGuid().ToString();
            _parkingLots.Add(parkingLot);
            return CreatedAtAction(nameof(Get), new { id = parkingLot.Id }, parkingLot);
        }
    }
}