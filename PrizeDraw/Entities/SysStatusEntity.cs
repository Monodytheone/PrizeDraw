using SqlSugar;

namespace PrizeDraw.Entities;

[SugarTable]
public class SysStatusEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; init; }

    public SysStatus SysStatus { get; set; }

    public bool HideEmployeeInfo { get; set; }

    /// <summary>
    /// 公证人
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? Notary { get; set; }
}

public enum SysStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStart = 1,

    /// <summary>
    /// 进行中
    /// </summary>
    Started = 2,

    /// <summary>
    /// 滚动中
    /// </summary>
    Rolling = 3,

    /// <summary>
    /// 停止滚动
    /// </summary>
    RollStop = 4,

    /// <summary>
    /// 抽奖结束
    /// </summary>
    Finished = 5
}