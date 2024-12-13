using SqlSugar;

namespace PrizeDraw.Entities;

[SugarTable]
public class CurrentWinner
{
    /// <summary>
    /// 工号
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public string UserId { get; init; }

    /// <summary>
    /// 姓名
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    public string Department { get; set; }
}
