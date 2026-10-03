using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 卧榻之乳蛙 - 每当召唤随从时抽牌。Amount = 抽牌数。
/// </summary>
public sealed class NaiWaWoTaZhiRuWaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private static bool _subscribed;

    internal static void Initialize()
    {
        if (!_subscribed)
        {
            MinionCmd.OnSummonEffectOccurred += OnAnyMinionSummoned;
            _subscribed = true;
        }
    }

    private static async Task OnAnyMinionSummoned(Player player)
    {
        var power = player.Creature.GetPower<NaiWaWoTaZhiRuWaPower>();
        if (power == null || power.Amount <= 0) return;
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), power.Amount, player);
    }
}
