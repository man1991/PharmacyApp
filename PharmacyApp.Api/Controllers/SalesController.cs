using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Api.Models;
using PharmacyApp.Api.Services;

namespace PharmacyApp.Api.Controllers
{
    /// <summary>
    /// Exposes endpoints to record and view medicine sale transactions,
    /// fulfilling the "maintain the sale records of medicine" requirement.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        /// <summary>Gets the full sales history, most recent first. GET /api/sales</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SaleRecord>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SaleRecord>>> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            return Ok(sales);
        }

        /// <summary>
        /// Records a sale of a given quantity of a medicine and reduces its stock.
        /// POST /api/sales
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SaleRecord), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleRecord>> RecordSale([FromBody] SaleCreateDto dto)
        {
            var sale = await _saleService.RecordSaleAsync(dto);
            return CreatedAtAction(nameof(GetAll), sale);
        }
    }
}
