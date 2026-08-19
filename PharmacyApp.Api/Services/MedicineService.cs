using PharmacyApp.Api.Exceptions;
using PharmacyApp.Api.Models;

namespace PharmacyApp.Api.Services
{
    /// <summary>
    /// Implements medicine CRUD and stock-related operations on top of a JSON file,
    /// as required ("Data to be stored in Json on server side").
    /// </summary>
    public class MedicineService : IMedicineService
    {
        private readonly JsonFileStore<Medicine> _store;
        private readonly ILogger<MedicineService> _logger;

        public MedicineService(IWebHostEnvironment environment, ILogger<MedicineService> logger)
        {
            var filePath = Path.Combine(environment.ContentRootPath, "Data", "medicines.json");
            _store = new JsonFileStore<Medicine>(filePath);
            _logger = logger;
        }

        public async Task<IReadOnlyList<Medicine>> GetAllAsync(string? searchTerm = null)
        {
            var medicines = await _store.ReadAllAsync();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return medicines
                    .OrderBy(m => m.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // "search capability which can query on name of medicine attribute"
            var term = searchTerm.Trim();
            return medicines
                .Where(m => m.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<Medicine> GetByIdAsync(Guid id)
        {
            var medicines = await _store.ReadAllAsync();
            return FindOrThrow(medicines, id);
        }

        public async Task<Medicine> CreateAsync(MedicineCreateDto dto)
        {
            ValidateExpiryDate(dto.ExpiryDate);

            var medicine = new Medicine
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Notes = dto.Notes?.Trim(),
                ExpiryDate = dto.ExpiryDate,
                Quantity = dto.Quantity,
                Price = Math.Round(dto.Price, 2),
                Brand = dto.Brand.Trim()
            };

            await _store.ReadModifyWriteAsync(list => list.Add(medicine));
            _logger.LogInformation("Created medicine {MedicineId} ({Name})", medicine.Id, medicine.FullName);
            return medicine;
        }

        public async Task<Medicine> UpdateAsync(Guid id, MedicineUpdateDto dto)
        {
            ValidateExpiryDate(dto.ExpiryDate);

            Medicine? updated = null;
            await _store.ReadModifyWriteAsync(list =>
            {
                var existing = FindOrThrow(list, id);
                existing.FullName = dto.FullName.Trim();
                existing.Notes = dto.Notes?.Trim();
                existing.ExpiryDate = dto.ExpiryDate;
                existing.Quantity = dto.Quantity;
                existing.Price = Math.Round(dto.Price, 2);
                existing.Brand = dto.Brand.Trim();
                updated = existing;
            });

            _logger.LogInformation("Updated medicine {MedicineId}", id);
            return updated!;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _store.ReadModifyWriteAsync(list =>
            {
                var existing = FindOrThrow(list, id);
                list.Remove(existing);
            });
            _logger.LogInformation("Deleted medicine {MedicineId}", id);
        }

        public async Task<Medicine> ReduceStockAsync(Guid id, int quantitySold)
        {
            Medicine? updated = null;
            await _store.ReadModifyWriteAsync(list =>
            {
                var existing = FindOrThrow(list, id);

                if (quantitySold > existing.Quantity)
                {
                    throw new BusinessRuleException(
                        $"Cannot sell {quantitySold} unit(s) of '{existing.FullName}' - only {existing.Quantity} in stock.");
                }

                existing.Quantity -= quantitySold;
                updated = existing;
            });

            return updated!;
        }

        /// <summary>Finds a medicine by id or throws a user-friendly 404.</summary>
        private static Medicine FindOrThrow(List<Medicine> medicines, Guid id)
        {
            var medicine = medicines.FirstOrDefault(m => m.Id == id);
            if (medicine is null)
            {
                throw new NotFoundException($"No medicine was found with id '{id}'. It may have already been removed.");
            }
            return medicine;
        }

        private static void ValidateExpiryDate(DateTime expiryDate)
        {
            // A generous lower bound - this catches obvious data-entry mistakes
            // (e.g. year 1900) without being overly strict about historical stock.
            if (expiryDate.Year < 2000)
            {
                throw new BusinessRuleException("Expiry date looks incorrect. Please double-check the date entered.");
            }
        }
    }
}
