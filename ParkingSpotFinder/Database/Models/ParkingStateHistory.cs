namespace Database.Models
{
    public class ParkingStateHistory
    {
        public int Id { get; set; }
        public required string ParkingLotId { get; set; }
        public DateTime Timestamp { get; set; }
        public int OccupiedSpots { get; set; }
        public int FreeSpots { get; set; }
        public int TotalSpots { get; set; }
    }
}