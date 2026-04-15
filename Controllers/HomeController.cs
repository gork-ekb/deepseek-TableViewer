using Microsoft.AspNetCore.Mvc;
using TableViewer.Services;

namespace TableViewer.Controllers;

public class HomeController(
    ViewService viewService,
    AuthService authService,
    ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var groups = await viewService.GetGroupsAsync();
        var allViews = await viewService.GetAllViewsAsync();

        ViewBag.Groups = groups;
        ViewBag.AllViews = allViews;

        return View();
    }

    [Route("{link}")]
    public async Task<IActionResult> ViewData(
        string link,
        [FromQuery] Dictionary<string, string> filters,
        [FromQuery] string? sortColumn,
        [FromQuery] string? sortDirection)
    {
        var viewConfig = await viewService.GetViewByLinkAsync(link);

        if (viewConfig == null)
        {
            return NotFound();
        }

        if (!authService.CanAccessProtectedView(viewConfig))
        {
            return Challenge();
        }

        var (data, columns) = await viewService.ExecuteViewQueryAsync(
            viewConfig,
            viewConfig.AllowFiltering ? filters : null,
            viewConfig.AllowSorting ? sortColumn : null,
            viewConfig.AllowSorting ? sortDirection : null);

        ViewBag.ViewConfig = viewConfig;
        ViewBag.Columns = columns;
        ViewBag.Filters = filters;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;

        return View(data);
    }
}