using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace API.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult NotImplementedStub()
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
            {
                Title = "Not implemented",
                Status = StatusCodes.Status501NotImplemented,
                Detail = "Stub endpoint —> functionality will be implemented later.",
                Instance = HttpContext?.Request.Path.Value
            });
        }
    }
}

