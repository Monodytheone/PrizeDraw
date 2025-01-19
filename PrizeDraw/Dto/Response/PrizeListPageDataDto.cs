using PrizeDraw.Entities;

namespace PrizeDraw.Dto.Response;

/// <summary>
/// 奖项列表页面所需的所有数据
/// </summary>
public class PrizeListPageDataDto
{
    public SysStatus SysStatus { get; set; }

    /// <summary>
    /// 公证人
    /// </summary>
    public string Notary { get; set; }

    /// <summary>
    /// 奖项列表
    /// </summary>
    public List<RafflePrizeEntity> RafflePrizes { get; set; }

    /// <summary>
    /// 中奖记录
    /// </summary>
    public List<PrizeRecordEntity> PrizeRecords { get; set; }

    /// <summary>
    /// By部级的中奖金额汇总表
    /// </summary>
    public List<WinningAmountByDepartment> WinningAmountByDepartment { get; set; }

    /// <summary>
    /// 员工名单
    /// </summary>
    public List<EmployeeEntity> Employees { get; set; }
}

public class WinningAmountByDepartment
{
    /// <summary>
    /// 部门
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// 金额
    /// </summary>
    public int Amount { get; set; }
}