using JobBoard.Application.Actions.Dashboard;
using JobBoard.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

[Authorize(Policy = AuthorizationPolicies.Dashboard)]
public class DashboardController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
        => await ExecuteQueryAsync(new GetDashboardQuery(), Ok);
}
