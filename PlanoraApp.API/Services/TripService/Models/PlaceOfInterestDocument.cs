using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanoraApp.API.Services.TripService.Models
{
    public class PlaceOfInterestDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AiSummary { get; set; } = string.Empty;
        
        public double Lat { get; set; }
        public double Lng { get; set; }
        
        public string Category { get; set; } = string.Empty; // hotel, restaurant, attraction
        
        public string SuggestedTime { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 60;
        
        public string MapsUrl { get; set; } = string.Empty;
        public decimal EstimatedCostUsd { get; set; }
    }
}
