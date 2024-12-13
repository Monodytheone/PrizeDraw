using Microsoft.AspNetCore.Mvc.Filters;
using SqlSugar;

namespace PrizeDraw.Filters;

public class TransactionFilter : IAsyncActionFilter
{
    private readonly ISqlSugarClient _db;

    public TransactionFilter(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            await _db.Ado.BeginTranAsync();
            await next();
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }
}
