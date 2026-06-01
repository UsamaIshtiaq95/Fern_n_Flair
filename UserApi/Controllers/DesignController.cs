using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace UserApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DesignController : ControllerBase
{
    // POST api/design/generate
    [HttpPost("generate")]
    [RequestSizeLimit(10_000_000)] // limit ~10MB for demo
    public async Task<IActionResult> GenerateDesign()
    {
        try
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            var style = form["style"].FirstOrDefault() ?? "modern";

            // For demo purposes, don't persist file. In production save to blob or DB.
            if (file == null)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Mocked design suggestions
            var suggestions = new[]
            {
                new { title = "Add light rug", description = $"A {style} light-colored rug would complement the floor." },
                new { title = "Accent wall", description = $"Paint one wall in a muted {style} tone to add depth." },
                new { title = "Greenery", description = "Add one or two potted plants to bring life to the room." }
            };

            return Ok(new { message = "Design generated", suggestions });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
        }
    }
}
