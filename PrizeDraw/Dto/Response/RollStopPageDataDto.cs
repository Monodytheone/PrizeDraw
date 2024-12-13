using PrizeDraw.Entities;

namespace PrizeDraw.Dto.Response;

public class RollStopPageDataDto
{
    /// <summary>
    /// 奖项
    /// </summary>
    public RollingRafflePrize RafflePrize { get; set; }

    /// <summary>
    /// 中奖人
    /// </summary>
    public CurrentWinner CurrentWinner { get; set; }

    public List<PrizeRecordEntity> PrizeRecordOfCurrentRafflePrize { get; set; }
}
