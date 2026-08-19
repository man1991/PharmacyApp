using System.ComponentModel.DataAnnotations;

namespace PharmacyApp.Api.Models
{
    /// <summary>
    /// Represents a medicine record stored and tracked by the pharmacy system.
    /// This is the persisted representation (also used to shape the JSON "database").
    /// </summary>
    public class Medicine
    {
        /// <summary>
        /// Unique identifier, generated server-side so the client never has to invent one.
        /// </summary>
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Full name of the medicine is required.")]
        [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Free-text notes. Optional and intentionally excluded from the grid view
        /// per the functional requirement ("except Notes").
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Expiry date is required.")]
        public DateTime ExpiryDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        /// <summary>
        /// Price with 2 decimal places. Stored as decimal to avoid floating point rounding issues.
        /// </summary>
        [Range(0.00, 1000000.00, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        [StringLength(150, ErrorMessage = "Brand cannot exceed 150 characters.")]
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// Business rule threshold: medicines expiring within this many days are
        /// flagged so the UI can render a red background.
        /// </summary>
        public const int ExpiryWarningDays = 30;

        /// <summary>
        /// Business rule threshold: medicines at or below this stock level are
        /// flagged so the UI can render a yellow background.
        /// </summary>
        public const int LowStockThreshold = 10;

        /// <summary>True when the medicine expires within <see cref="ExpiryWarningDays"/> days.</summary>
        public bool IsNearExpiry => (ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays < ExpiryWarningDays;

        /// <summary>True when the remaining quantity is at/below <see cref="LowStockThreshold"/>.</summary>
        public bool IsLowStock => Quantity < LowStockThreshold;
    }
}
