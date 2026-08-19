namespace PharmacyApp.Api.Models
{
    /// <summary>
    /// Represents a single sale transaction of a medicine.
    /// Kept as an append-only log so the pharmacy has a full sales history.
    /// </summary>
    public class SaleRecord
    {
        public Guid Id { get; set; }

        public Guid MedicineId { get; set; }

        /// <summary>
        /// Denormalized snapshot of the medicine name at the time of sale, so the
        /// sales history still reads correctly even if the medicine is later renamed or removed.
        /// </summary>
        public string MedicineName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        /// <summary>Unit price at the time of sale (captured, not looked up later).</summary>
        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime SaleDate { get; set; }
    }
}
