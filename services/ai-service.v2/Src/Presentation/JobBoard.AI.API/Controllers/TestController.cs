using JobBoard.AI.API.Actions.Test;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

public class TestController : BaseApiController
{
    [HttpPost]
    public Task<IActionResult> Test()
        => ExecuteCommandAsync(new TestCommand(), Ok);
}
