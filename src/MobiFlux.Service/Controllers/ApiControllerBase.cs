using Microsoft.AspNetCore.Mvc;
using MobiFlux.Shared.Common;

namespace MobiFlux.Service.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T value) => Ok(ApiResponse<T>.Ok(value, HttpContext.TraceIdentifier));
    protected ActionResult<ApiResponse<object>> AcceptedOperation(string message) => Accepted(value: ApiResponse<object>.Ok(new { message }, HttpContext.TraceIdentifier));
}
