using Microsoft.EntityFrameworkCore;
using TableViewer.Data;
using TableViewer.Models;

namespace TableViewer.Services;

public class SettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;
    private Dictionary<string, string?> _settingsCache;
    private readonly object _cacheLock = new object();

    public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
        _settingsCache = new Dictionary<string, string?>();
    }

    /// <summary>
    /// Получить значение настройки по имени
    /// </summary>
    public async Task<string?> GetSettingAsync(string name)
    {
        // Проверяем кэш
        lock (_cacheLock)
        {
            if (_settingsCache.TryGetValue(name, out var cachedValue))
            {
                return cachedValue;
            }
        }

        // Загружаем из БД
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Name == name);
        var value = setting?.Value;

        // Сохраняем в кэш
        lock (_cacheLock)
        {
            _settingsCache[name] = value;
        }

        return value;
    }

    /// <summary>
    /// Установить значение настройки
    /// </summary>
    public async Task SetSettingAsync(string name, string? value)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Name == name);

        if (setting == null)
        {
            setting = new Setting { Name = name, Value = value };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        await _context.SaveChangesAsync();

        // Обновляем кэш
        lock (_cacheLock)
        {
            _settingsCache[name] = value;
        }
    }

    /// <summary>
    /// Получить заголовок приложения
    /// </summary>
    public async Task<string> GetAppTitleAsync()
    {
        var header = await GetSettingAsync("header");
        return string.IsNullOrEmpty(header) ? "TableViewer" : header;
    }

    /// <summary>
    /// Очистить кэш
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _settingsCache.Clear();
        }
    }
}