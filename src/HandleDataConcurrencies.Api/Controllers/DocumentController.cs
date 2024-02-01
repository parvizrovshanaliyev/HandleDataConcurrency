using HandleDataConcurrency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandleDataConcurrency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _service;

    public DocumentController(IDocumentService service)
    {
        _service = service;
    }
        
    [AllowAnonymous]
    [HttpPost("create-document")]
    public async Task<ActionResult> Create(CancellationToken cancellationToken)
    {
        return Ok(await _service.CreateDocumentAsync(cancellationToken));
    }
}