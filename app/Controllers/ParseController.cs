using Microsoft.AspNetCore.Mvc;
using TestApp.Models;
using TestApp.Services;

namespace TestApp.Controllers;

[ApiController]
[Route("api/parse")]
public sealed class ParseController(IParseService parseService) : ControllerBase
{
    /// <summary>
    /// Разбор HTML: элементы по CSS-селектору, email, AES-256 ECB, запись в PostgreSQL.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ParseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ParseResponse>> Post(
        [FromBody] ParseRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Ok(ParseResponse.Fail("INVALID_REQUEST", "Request body is required."));
        }

        var response = await parseService.ProcessAsync(request, cancellationToken);
        return Ok(response);
    }
}
