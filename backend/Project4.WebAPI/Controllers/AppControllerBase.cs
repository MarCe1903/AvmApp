using Microsoft.AspNetCore.Mvc;
using Project4.WebAPI.Extensions;

namespace Project4.WebAPI.Controllers;

public abstract class AppControllerBase : ControllerBase
{
    protected int? CurrentUserId => User.GetUserId();

    protected bool IsSellerUser => User.IsSellerUser();

    protected IActionResult FromResult(Core.Utilities.Results.IResult result)
    {
        return result.Success ? Ok(result) : BadRequest(result);
    }

    protected IActionResult FromApiDataResult<T>(Core.Utilities.Results.IDataResult<T> result)
    {
        return Ok(new Core.Utilities.Results.ApiResponse<T>(result.Data, result.Success, result.Message));
    }
}
