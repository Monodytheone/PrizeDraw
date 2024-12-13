using PrizeDraw.Entities;

namespace PrizeDraw.Dto.Response;

public class SysStatusDto
{
    /// <summary>
    /// 系统状态   1-未开始 2-进行中 3-滚动中 4-停止滚动 5-抽奖结束
    /// </summary>
    public SysStatus SysStatus { get; set; }

    /// <summary>
    /// 数据设置页面的数据
    /// </summary>
    public SysStatusDto_NotStart_1? NotStart_1 { get; set; }

    /// <summary>
    /// 奖项列表页面的数据
    /// </summary>
    public PrizeListPageDataDto? Started_2_5 { get; set; }

    /// <summary>
    /// 滚动页面数据
    /// </summary>
    public RollingPageDataDto? Rolling_3 { get; set; }

    /// <summary>
    /// 停止滚动页面数据
    /// </summary>
    public RollStopPageDataDto StopRoll_4 { get; set; }
}

public class SysStatusDto_NotStart_1
{
    /// <summary>
    /// 奖项
    /// </summary>
    public List<RafflePrizeEntity>? RafflePrizes { get; set; }

    /// <summary>
    /// 员工列表
    /// </summary>
    public List<EmployeeEntity>? Employees { get; set; }
}
