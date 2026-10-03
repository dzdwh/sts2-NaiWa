using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NaiWa.cards;

/// <summary>
/// 奶脚 - 1费common技能。获得6/9格挡。触发所有随从的效果各1次。
/// </summary>
public sealed class NaiWaNaiJiao : CardModel
{
    /// <summary>
    /// 本回合是否打出过带召唤效果的牌。
    /// 直接查官方卡牌历史（CardPlaysFinished），双端确定性一致，无需静态状态与回合清理；
    /// 只看"是否打出过召唤牌"，与召唤是否真的发生（已在场合 HP 等）无关。
    /// </summary>
    public static bool HasPlayedSummonCardThisTurn(Player player)
    {
        if (player?.Creature?.CombatState == null) return false;
        var combatState = player.Creature.CombatState;
        var history = CombatManager.Instance?.History;
        if (history == null) return false;
        return history.CardPlaysFinished.Any(e =>
            e.HappenedThisTurn(combatState)
            && e.CardPlay.Card.Owner == player
            && NaiWaKeywords.SummonCardTypes.Contains(e.CardPlay.Card.GetType()));
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_naijiao.png";

    public NaiWaNaiJiao() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 触发所有存活随从的效果各1次（奶鲸未重写 TriggerTurnEffect，自动跳过）
        var pets = Owner.Creature.Pets.Where(p => p.IsAlive).ToList();
        foreach (var pet in pets)
        {
            foreach (var power in pet.GetPowerInstances<powers.NaiWaMinionPowerBase>())
                await power.TriggerTurnEffect(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 6→9
    }
}
