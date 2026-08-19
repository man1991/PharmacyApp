using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Api.Models;
using PharmacyApp.Api.Services;

namespace PharmacyApp.Api.Controllers
{
    /// <summary>
    /// Exposes CRUD + search endpoints for medicines.
    /// All exceptions are intentionally left to bubble up to the global
    /// <see cref="Middleware.ExceptionHandlingMiddleware"/> rather than being caught
    /// here - this keeps controllers thin and error formatting consistent everywhere.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _medicineService;
        private readonly ILogger<MedicinesController> _logger;

        public MedicinesController(IMedicineService medicineService, ILogger<MedicinesController> logger)
        {
            _medicineService = medicineService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the list of medicines, optionally filtered by name.
        /// GET /api/medicines?search=para
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Medicine>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Medicine>>> GetAll([FromQuery] string? search)
        {
            var medicines = await _medicineService.GetAllAsync(search);
            return Ok(medicines);
        }

        /// <summary>Gets a single medicine by id. GET /api/medicines/{id}</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Medicine), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Medicine>> GetById(Guid id)
        {
            var medicine = await _medicineService.GetByIdAsync(id);
            return Ok(medicine);
        }

        /// <summary>Adds a new medicine. POST /api/medicines</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Medicine), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Medicine>> Create([FromBody] MedicineCreateDto dto)
        {
            // [ApiController] automatically returns a 400 with validation details
            // if the DTO's data-annotation rules (Required, StringLength, Range...) fail,
            // so we don't need to re-check ModelState.IsValid manually here.
            var created = await _medicineService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing medicine. PUT /api/medicines/{id}</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Medicine), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Medicine>> Update(Guid id, [FromBody] MedicineUpdateDto dto)
        {
            var updated = await _medicineService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        /// <summary>Deletes a medicine. DELETE /api/medicines/{id}</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _medicineService.DeleteAsync(id);
            return NoContent();
        }
    }
}
