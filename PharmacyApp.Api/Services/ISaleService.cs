using PharmacyApp.Api.Models;

namespace PharmacyApp.Api.Services
{
    /// <summary>Abstraction over sale-record storage/business logic.</summary>
    public interface ISaleService
    {
        Task<IReadOnlyList<SaleRecord>> GetAllAsync();

        /// <summary>
        /// Records a sale: validates and reduces the medicine's stock, then appends
        /// an entry to the sales history. Both steps succeed or fail together.
        /// </summary>
        Task<SaleRecord> RecordSaleAsync(SaleCreateDto dto);
    }
}
