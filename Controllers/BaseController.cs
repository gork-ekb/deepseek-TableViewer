using Microsoft.AspNetCore.Mvc;
using TableViewer.Services;

namespace TableViewer.Controllers;

public class BaseController : Controller
{
    protected readonly SettingsService _settingsService;

    public BaseController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    protected async Task SetAppTitle()
    {
        var title = await _settingsService.GetAppTitleAsync();
        ViewBag.AppTitle = title;
    }
}