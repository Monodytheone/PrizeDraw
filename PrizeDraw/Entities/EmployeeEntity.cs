using SqlSugar;

namespace PrizeDraw.Entities;

/// <summary>
/// 员工表（所有参与抽奖的员工）
/// </summary>
[SugarTable]
public class EmployeeEntity
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

    /// <summary>
    /// 是否已中奖
    /// </summary>
    public bool HasWon { get; set; }
}
