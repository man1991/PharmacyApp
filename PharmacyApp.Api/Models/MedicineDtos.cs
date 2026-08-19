using System.ComponentModel.DataAnnotations;

namespace PharmacyApp.Api.Models
{
    /// <summary>
    /// Payload accepted when a client adds a new medicine.
    /// Kept separate from <see cref="Medicine"/> so the client can never set
    /// server-controlled fields such as Id.
    /// </summary>
    public class MedicineCreateDto
    {
        [Required(ErrorMessage = "Full name of the medicine is required.")]
        [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Expiry date is required.")]
        public DateTime ExpiryDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        [Range(0.00, 1000000.00, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        [StringLength(150, ErrorMessage = "Brand cannot exceed 150 characters.")]
        public string Brand { get; set; } = string.Empty;
    }

    /// <summary>Payload accepted when a client updates an existing medicine.</summary>
    public class MedicineUpdateDto : MedicineCreateDto
    {
    }

    /// <summary>Payload accepted when a client records a sale against a medicine.</summary>
    public class SaleCreateDto
    {
        [Required(ErrorMessage = "MedicineId is required.")]
        public Guid MedicineId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity sold must be at least 1.")]
        public int QuantitySold { get; set; }
    }
}
