namespace Database.Models
{
    public class ParkingStateHistory
    {
        public required string Id { get; set; }
        public required string ParkingLotId { get; set; }
        public DateTime Timestamp { get; set; }
        public int FreeSpaces { get; set; }
    }
}