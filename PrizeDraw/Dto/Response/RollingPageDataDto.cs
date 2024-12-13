using PrizeDraw.Entities;

namespace PrizeDraw.Dto.Response;

public class RollingPageDataDto
{
    /// <summary>
    /// 正在滚动的奖项
    /// </summary>
    public RollingRafflePrize RollingRafflePrize { get; set; }

    /// <summary>
    /// 正在滚动的员工列表
    /// </summary>
    public List<EmployeeEntity> RollingEmployees { get; set; }
}
