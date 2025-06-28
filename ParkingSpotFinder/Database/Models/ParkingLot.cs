namespace Database.Models
{
    public class ParkingLot
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
        public int TotalParkingSpaces { get; set; }
        public required string CameraUrl { get; set; }
    }
}