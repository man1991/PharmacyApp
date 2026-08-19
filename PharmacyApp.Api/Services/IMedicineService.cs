using PharmacyApp.Api.Models;

namespace PharmacyApp.Api.Services
{
    /// <summary>
    /// Abstraction over medicine storage/business logic so controllers depend on
    /// a contract rather than a concrete JSON-file implementation. This also makes
    /// the service easy to unit test with a mock or an in-memory fake.
    /// </summary>
    public interface IMedicineService
    {
        Task<IReadOnlyList<Medicine>> GetAllAsync(string? searchTerm = null);
        Task<Medicine> GetByIdAsync(Guid id);
        Task<Medicine> CreateAsync(MedicineCreateDto dto);
        Task<Medicine> UpdateAsync(Guid id, MedicineUpdateDto dto);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Reduces stock for a medicine as part of recording a sale.
        /// Throws if there isn't enough stock available.
        /// </summary>
        Task<Medicine> ReduceStockAsync(Guid id, int quantitySold);
    }
}
