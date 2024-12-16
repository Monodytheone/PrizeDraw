using FluentValidation;

namespace PrizeDraw.Dto.Request;

public class AddBonusPrizeDto
{
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
}

public class AddBonusPrizeDtoValidator : AbstractValidator<AddBonusPrizeDto>
{
    public AddBonusPrizeDtoValidator()
    {
        RuleFor(x => x.PrizeName).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PrizeAmount).GreaterThan(0);
    }
}
