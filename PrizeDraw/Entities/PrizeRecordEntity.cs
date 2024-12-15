using SqlSugar;

namespace PrizeDraw.Entities;

/// <summary>
/// 中奖记录
/// </summary>
[SugarTable]
public class PrizeRecordEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; init; }

    public string RafflePrizeId { get; set; }

    /// <summary>
    /// 奖项名称
    /// </summary>
    public string RafflePrizeName { get; set; }

    /// <summary>
    /// 中奖金额
    /// </summary>
    public int PrizeAmount { get; set; }

    /// <summary>
    /// 中奖人工号
    /// </summary>
    public string WinnerId { get; set; }

    /// <summary>
    /// 中奖人姓名
    /// </summary>
    public string WinnerName { get; set; }

    /// <summary>
    /// 中奖人部门
    /// </summary>
    public string WinnerDepartment { get; set; }

    /// <summary>
    /// 中奖时间
    /// </summary>
    public DateTime WinTime { get; set; }
}
