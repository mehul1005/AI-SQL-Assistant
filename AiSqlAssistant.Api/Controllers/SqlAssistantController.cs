using AiSqlAssistant.Api.Models;
using AiSqlAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiSqlAssistant.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SqlAssistantController : ControllerBase
    {
        private readonly ISqlGeneratorService _sqlGenerator;

        public SqlAssistantController(ISqlGeneratorService sqlGenerator)
        {
            _sqlGenerator = sqlGenerator;
        }

        [HttpPost("generate-sql")]
        public async Task<IActionResult> GenerateSql([FromBody] SqlGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                return BadRequest("User prompt cannot be empty.");
            }

            try
            {
                var result = await _sqlGenerator.GenerateSqlAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}