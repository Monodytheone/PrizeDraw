using FluentValidation;

namespace PrizeDraw.Dto.Request;

public class SetRafflePrizeDto
{
    public List<SetRafflePrizeDtoItem> Items { get; set; }
}

public class SetRafflePrizeDtoItem
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


public class SetRafflePrizeDtoValidator : AbstractValidator<SetRafflePrizeDto>
{
    public SetRafflePrizeDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().ForEach(itemRule =>
        {
            itemRule.ChildRules(childRule =>
            {
                childRule.RuleFor(i => i.PrizeName).NotEmpty();
                childRule.RuleFor(i => i.Quantity).GreaterThan(0);
                childRule.RuleFor(i => i.PrizeAmount).GreaterThan(0);
            });
        });
    }
}
