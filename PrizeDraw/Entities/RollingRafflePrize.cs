using SqlSugar;

namespace PrizeDraw.Entities;

/// <summary>
/// 滾動中的獎項信息
/// </summary>
[SugarTable]
public class RollingRafflePrize
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; init; }

    /// <summary>
    /// 奖项名称
    /// </summary>
    public string PrizeName { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 奖项金额
    /// </summary>
    public int PrizeAmount { get; set; }

    /// <summary>
    /// 已抽数量
    /// </summary>
    public int DrawnCount { get; set; }

    /// <summary>
    /// 可抽数量
    /// </summary>
    public int DrawsLeft { get; set; }
}
