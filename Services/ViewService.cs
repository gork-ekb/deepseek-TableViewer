using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TableViewer.Data;
using TableViewer.Models;

namespace TableViewer.Services;

public class ViewService(
    ApplicationDbContext context,
    ILogger<ViewService> logger)
{
    public async Task<List<ViewConfig>> GetAllViewsAsync()
    {
        return await context.Views.ToListAsync();
    }

    public async Task<ViewConfig?> GetViewByIdAsync(int id)
    {
        return await context.Views.FindAsync(id);
    }

    public async Task<ViewConfig?> GetViewByLinkAsync(string link)
    {
        return await context.Views.FirstOrDefaultAsync(v => v.Link == link);
    }

    public async Task<List<string>> GetGroupsAsync()
    {
        return await context.Views
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

        // Получаем колонки
        var schemaQuery = $"""
            SELECT COLUMN_NAME
            FROM [{view.DatabaseName}].INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = '{view.SchemaName}'
              AND TABLE_NAME = '{view.TableName}'
            ORDER BY ORDINAL_POSITION
            """;

        var columns = (await connection.QueryAsync<string>(schemaQuery)).ToList();

        // Строим запрос
        var sqlBuilder = new System.Text.StringBuilder($"SELECT * FROM {fullTableName}");

        // Фильтрация
        if (view.AllowFiltering && filters is {Count: > 0})
        {
            var filterClauses = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value) && columns.Contains(f.Key)))
            {
                filterClauses.Add($"[{filter.Key}] LIKE @{filter.Key}");
                parameters.Add(filter.Key, $"%{filter.Value}%");
            }

            if (filterClauses.Count != 0)
            {
                sqlBuilder.Append(" WHERE ");
                sqlBuilder.Append(string.Join(" AND ", filterClauses));

                var data = await connection.QueryAsync(sqlBuilder.ToString(), parameters);
                return (data, columns);
            }
        }

        // Сортировка
        if (view.AllowSorting && !string.IsNullOrEmpty(sortColumn) && columns.Contains(sortColumn))
        {
            var direction = sortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";
            sqlBuilder.Append($" ORDER BY [{sortColumn}] {direction}");
        }

        var finalData = await connection.QueryAsync(sqlBuilder.ToString());
        return (finalData, columns);
    }

    public async Task AddViewAsync(ViewConfig view)
    {
        context.Views.Add(view);
        await context.SaveChangesAsync();
    }

    public async Task UpdateViewAsync(ViewConfig view)
    {
        context.Views.Update(view);
        await context.SaveChangesAsync();
    }

    public async Task DeleteViewAsync(int id)
    {
        var view = await context.Views.FindAsync(id);
        if (view != null)
        {
            context.Views.Remove(view);
            await context.SaveChangesAsync();
        }
    }
}