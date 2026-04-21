namespace PlanoraApp.AI.Models
{
    public class TripGenerationRequest
    {
        public string TripId { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BudgetTier { get; set; } = string.Empty;
        public string GroupType { get; set; } = string.Empty;
        public List<string> Themes { get; set; } = new();
        public string TravelMode { get; set; } = null; // "flightIncluded" or "travelOnly"
        public string Origin { get; set; } = null; // Origin city if provided
        public int PaxCount { get; set; } = 1;
    }
    
    public class AgentJobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string TripId { get; set; } = string.Empty;
        public string Status { get; set; } = "in_queue"; // in_queue, processing, generating_places, complete, failed
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
