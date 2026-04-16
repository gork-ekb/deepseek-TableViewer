using Microsoft.AspNetCore.Mvc;
using TableViewer.Services;

namespace TableViewer.Controllers;

public class HomeController : BaseController
{
    private readonly ViewService _viewService;
    private readonly AuthService _authService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ViewService viewService,
        AuthService authService,
        ILogger<HomeController> logger,
        SettingsService settingsService) : base(settingsService)  // Добавлен settingsService
    {
        _viewService = viewService;
        _authService = authService;
        _logger = logger;
    }

    // Главная страница - список групп таблицей
    public async Task<IActionResult> Index()
    {
        await SetAppTitle();  // Устанавливаем заголовок
        var groups = await _viewService.GetGroupsAsync();
        var allViews = await _viewService.GetAllViewsAsync();

        ViewBag.AllViews = allViews;
        return View(groups);
    }

    // Страница группы - список запросов в группе
    [Route("group/{groupName}")]
    public async Task<IActionResult> Group(string groupName)
    {
        await SetAppTitle();  // Устанавливаем заголовок
        var allViews = await _viewService.GetAllViewsAsync();
        var viewsInGroup = allViews.Where(v => v.Group == groupName).ToList();

        if (!viewsInGroup.Any())
        {
            return NotFound();
        }

        ViewBag.GroupName = groupName;
        return View(viewsInGroup);
    }

    // Страница с результатами запроса
    [Route("{link}")]
    public async Task<IActionResult> ViewData(
        string link,
        [FromQuery] Dictionary<string, string> filters,
        [FromQuery] string? sortColumn,
        [FromQuery] string? sortDirection)
    {
        await SetAppTitle();  // Устанавливаем заголовок
        var viewConfig = await _viewService.GetViewByLinkAsync(link);

        if (viewConfig == null)
        {
            return NotFound();
        }

        if (!_authService.CanAccessProtectedView(viewConfig))
        {
            return Challenge();
        }

        var (data, columns) = await _viewService.ExecuteViewQueryAsync(
            viewConfig,
            viewConfig.AllowFiltering ? filters : null,
            viewConfig.AllowSorting ? sortColumn : null,
            viewConfig.AllowSorting ? sortDirection : null);

        ViewBag.ViewConfig = viewConfig;
        ViewBag.Columns = columns;
        ViewBag.Filters = filters ?? new Dictionary<string, string>();
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;

        return View(data);
    }
}