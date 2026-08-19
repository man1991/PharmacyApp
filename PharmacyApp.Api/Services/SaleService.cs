using PharmacyApp.Api.Models;

namespace PharmacyApp.Api.Services
{
    /// <summary>
    /// Implements the "maintain the sale records of medicine" requirement.
    /// Each sale both reduces the medicine's stock and appends an immutable
    /// entry to the sales history file.
    /// </summary>
    public class SaleService : ISaleService
    {
        private readonly JsonFileStore<SaleRecord> _store;
        private readonly IMedicineService _medicineService;
        private readonly ILogger<SaleService> _logger;

        public SaleService(IWebHostEnvironment environment, IMedicineService medicineService, ILogger<SaleService> logger)
        {
            var filePath = Path.Combine(environment.ContentRootPath, "Data", "sales.json");
            _store = new JsonFileStore<SaleRecord>(filePath);
            _medicineService = medicineService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SaleRecord>> GetAllAsync()
        {
            var sales = await _store.ReadAllAsync();
            return sales
                .OrderByDescending(s => s.SaleDate)
                .ToList();
        }

        public async Task<SaleRecord> RecordSaleAsync(SaleCreateDto dto)
        {
            // Step 1: validate stock and decrement it. This throws a BusinessRuleException
            // (mapped to a friendly 400 response) if there isn't enough stock.
            var medicine = await _medicineService.ReduceStockAsync(dto.MedicineId, dto.QuantitySold);

            // Step 2: append the sale to the history log.
            var sale = new SaleRecord
            {
                Id = Guid.NewGuid(),
                MedicineId = medicine.Id,
                MedicineName = medicine.FullName,
                QuantitySold = dto.QuantitySold,
                UnitPrice = medicine.Price,
                TotalAmount = Math.Round(medicine.Price * dto.QuantitySold, 2),
                SaleDate = DateTime.UtcNow
            };

            await _store.ReadModifyWriteAsync(list => list.Add(sale));
            _logger.LogInformation(
                "Recorded sale {SaleId}: {Quantity} x {MedicineName}", sale.Id, sale.QuantitySold, sale.MedicineName);

            return sale;
        }
    }
}
