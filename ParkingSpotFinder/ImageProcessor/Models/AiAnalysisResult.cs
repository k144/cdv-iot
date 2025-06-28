namespace ImageProcessor.Models
{
    public class AiAnalysisResult
    {
        public int OccupiedSpots { get; set; }
        public int FreeSpots { get; set; }
        public int TotalSpots { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
    }
}