using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TableViewer.Data;
using TableViewer.Models;

namespace TableViewer.Services;

public class ViewService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ViewService> _logger;

    public ViewService(ApplicationDbContext context, ILogger<ViewService> logger)
    {
        _context = context;
        _logger = logger;
    }
    // }
    // {
    public async Task<List<ViewConfig>> GetAllViewsAsync()
    {
        return await _context.Views.ToListAsync();
    }

    public async Task<ViewConfig?> GetViewByIdAsync(int id)
    {
        return await _context.Views.FindAsync(id);
    }

    public async Task<ViewConfig?> GetViewByLinkAsync(string link)
    {
        return await _context.Views.FirstOrDefaultAsync(v => v.Link == link);
    }

    public async Task<List<string>> GetGroupsAsync()
    {
        return await _context.Views
            .Where(v => v.Group != null)
            .Select(v => v.Group!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    public async Task<(IEnumerable<dynamic> Data, List<string> Columns)> ExecuteViewQueryAsync(
        ViewConfig view,
        Dictionary<string, string>? filters = null,
        string? sortColumn = null,
        string? sortDirection = null)
    {
        var fullTableName = $"[{view.DatabaseName}].[{view.SchemaName}].[{view.TableName}]";
        var connectionString = $"Server={view.Host};Database={view.DatabaseName};Integrated Security=true;TrustServerCertificate=true;Encrypt=false";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Получаем колонки (сохраняем оригинальные имена)
        var schemaQuery = $"""
            SELECT COLUMN_NAME
            FROM [{view.DatabaseName}].INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = '{view.SchemaName}'
            AND TABLE_NAME = '{view.TableName}'
            ORDER BY ORDINAL_POSITION
            """;

        var columns = (await connection.QueryAsync<string>(schemaQuery)).ToList();

        // СОЗДАЕМ СЛОВАРЬ ДЛЯ ИГНОРИРОВАНИЯ РЕГИСТРА
        var columnsDictionary = columns.ToDictionary(c => c, c => c, StringComparer.OrdinalIgnoreCase);

        // Строим запрос
        var sqlBuilder = new System.Text.StringBuilder($"SELECT * FROM {fullTableName}");
        var parameters = new DynamicParameters();

        // Фильтрация (если разрешена)
        if (view.AllowFiltering && filters is { Count: > 0 })
        {
            var filterClauses = new List<string>();

            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
            {
                // Ищем колонку без учета регистра
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, filter.Key, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    filterClauses.Add($"[{matchingColumn}] LIKE @{matchingColumn}");
                    parameters.Add(matchingColumn, $"%{filter.Value}%");
                }
            }

            if (filterClauses.Count != 0)
            {
                sqlBuilder.Append(" WHERE ");
                sqlBuilder.Append(string.Join(" AND ", filterClauses));
            }
        }

        // ЛОГИКА СОРТИРОВКИ (с учетом регистра)
        string? finalSortColumn = null;
        string? finalSortDirection = null;

        if (view.AllowSorting)
        {
            // Приоритет 1: Явная сортировка от пользователя
            if (!string.IsNullOrEmpty(sortColumn))
            {
                // Ищем колонку без учета регистра
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, sortColumn, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    finalSortColumn = matchingColumn;
                    finalSortDirection = sortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";
                }
            }

            // Приоритет 2: Сортировка по умолчанию из настроек
            if (string.IsNullOrEmpty(finalSortColumn) && !string.IsNullOrEmpty(view.DefaultSortField))
            {
                // Ищем колонку без учета регистра
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, view.DefaultSortField, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    finalSortColumn = matchingColumn;
                    finalSortDirection = "ASC";
                }
            }

            // Применяем сортировку если есть
            if (!string.IsNullOrEmpty(finalSortColumn))
            {
                sqlBuilder.Append($" ORDER BY [{finalSortColumn}] {finalSortDirection}");
            }
        }

        // Выполняем запрос
        var finalData = await connection.QueryAsync(sqlBuilder.ToString(), parameters);
        return (finalData, columns);
    }

    public async Task<(IEnumerable<dynamic> Data, List<string> Columns, int TotalCount)> ExecuteViewQueryWithPaginationAsync(
        ViewConfig view,
        Dictionary<string, string>? filters = null,
        string? sortColumn = null,
        string? sortDirection = null,
        int pageNumber = 1)
    {
        var fullTableName = $"[{view.DatabaseName}].[{view.SchemaName}].[{view.TableName}]";
        var connectionString = $"Server={view.Host};Database={view.DatabaseName};Integrated Security=true;TrustServerCertificate=true;Encrypt=false";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Получаем колонки
        var schemaQuery = $"""
        SELECT COLUMN_NAME
        FROM [{view.DatabaseName}].INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = '{view.SchemaName}'
          AND TABLE_NAME = '{view.TableName}'
        ORDER BY ORDINAL_POSITION
        """;

        var columns = (await connection.QueryAsync<string>(schemaQuery)).ToList();

        // Строим WHERE и ORDER BY
        var whereClause = "";
        var orderByClause = "";
        var parameters = new DynamicParameters();

        // Фильтрация
        if (view.AllowFiltering && filters is { Count: > 0 })
        {
            var filterClauses = new List<string>();

            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
            {
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, filter.Key, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    filterClauses.Add($"[{matchingColumn}] LIKE @{matchingColumn}");
                    parameters.Add(matchingColumn, $"%{filter.Value}%");
                }
            }

            if (filterClauses.Count != 0)
            {
                whereClause = " WHERE " + string.Join(" AND ", filterClauses);
            }
        }

        // Сортировка
        string? finalSortColumn = null;
        string? finalSortDirection = null;

        if (view.AllowSorting)
        {
            if (!string.IsNullOrEmpty(sortColumn))
            {
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, sortColumn, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    finalSortColumn = matchingColumn;
                    finalSortDirection = sortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";
                }
            }

            if (string.IsNullOrEmpty(finalSortColumn) && !string.IsNullOrEmpty(view.DefaultSortField))
            {
                var matchingColumn = columns.FirstOrDefault(c =>
                    string.Equals(c, view.DefaultSortField, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn != null)
                {
                    finalSortColumn = matchingColumn;
                    finalSortDirection = "ASC";
                }
            }

            if (!string.IsNullOrEmpty(finalSortColumn))
            {
                orderByClause = $" ORDER BY [{finalSortColumn}]";
            }
        }

        // Получаем общее количество записей
        var countSql = $"SELECT COUNT(*) FROM {fullTableName}{whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Пагинация
        var pageSize = view.PageSize > 0 ? view.PageSize : totalCount;
        var offset = (pageNumber - 1) * pageSize;

        // Убеждаемся, что ORDER BY присутствует
        var finalOrderBy = !string.IsNullOrEmpty(orderByClause) ? orderByClause : " ORDER BY (SELECT NULL)";

        var dataSql = $"""
            SELECT * FROM {fullTableName}
            {whereClause}
            {finalOrderBy}
            OFFSET @offset ROWS
            FETCH NEXT @pageSize ROWS ONLY
            """;

        parameters.Add("offset", offset);
        parameters.Add("pageSize", pageSize);

        var data = await connection.QueryAsync(dataSql, parameters);

        return (data, columns, totalCount);
    }

    public async Task AddViewAsync(ViewConfig view)
    {
        _context.Views.Add(view);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateViewAsync(ViewConfig view)
    {
        _context.Views.Update(view);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteViewAsync(int id)
    {
        var view = await _context.Views.FindAsync(id);
        if (view != null)
        {
            _context.Views.Remove(view);
            await _context.SaveChangesAsync();
        }
    }
}

