using NPOI.SS.Formula.Functions;
using PrizeDraw.Dto.Response;
using PrizeDraw.Entities;
using SqlSugar;

namespace PrizeDraw.Services;

public class PrizeListPageService
{
    private readonly ISqlSugarClient _db;

    public PrizeListPageService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PrizeListPageDataDto> GetPrizeListPageDataAsync()
    {
        List<RafflePrizeEntity> rafflePrizes = await _db.Queryable<RafflePrizeEntity>().ToListAsync();
        List<PrizeRecordEntity> prizeRecords = await _db.Queryable<PrizeRecordEntity>()
            .OrderBy(r => r.WinTime).ToListAsync();
        List<EmployeeEntity> employees = await _db.Queryable<EmployeeEntity>().ToListAsync();

        List<string> departments = employees.Select(employee => employee.Department)
            .ToHashSet().ToList();
        List<WinningAmountByDepartment> winningAmountByDepartments = departments.Select(d => new WinningAmountByDepartment
        {
            Department = d,
            Amount = prizeRecords.Where(r => r.WinnerDepartment == d).Sum(r => r.PrizeAmount)
        }).ToList();

        return new PrizeListPageDataDto
        {
            RafflePrizes = rafflePrizes,
            PrizeRecords = prizeRecords,
            WinningAmountByDepartment = winningAmountByDepartments,
            Employees = employees
        };
    }
}
