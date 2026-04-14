using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanoraApp.API.Services.VaultService
{
    [Table("vault_items")]
    public class VaultItem
    {
        [Key]
        [Column("item_id")]
        public Guid ItemId { get; set; } = Guid.NewGuid();

        [Column("trip_id")]
        public Guid TripId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("blob_url")]
        public string BlobUrl { get; set; } = string.Empty;

        [Column("parsed_data", TypeName = "jsonb")]
        public string ParsedData { get; set; } = "{}";

        [Column("source")]
        public string Source { get; set; } = "upload";

        [Column("is_offline_cached")]
        public bool IsOfflineCached { get; set; } = false;
    }
}
