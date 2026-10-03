using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 死生契阔 - 0费 uncommon 技能。给当前抗伤害的随从施加 buff：
/// 若本回合死亡，下回合以 3 血复活。升级获得保留。
/// </summary>
public sealed class NaiWaSiShengQiKuo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("ReviveHp", 3m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaSiShengQiKuoPower>(),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_si_sheng_qi_kuo.png";

    public NaiWaSiShengQiKuo() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 当前承伤的随从：奶蛋优先，否则最后召唤的存活随从
        var tank = NaiWaMinionPowerBase.GetCurrentTank(Owner);
        if (tank == null) return;

        var power = await PowerCmd.Apply<NaiWaSiShengQiKuoPower>(choiceContext, tank, 1, Owner.Creature, this);
        if (power != null)
            power.ReviveHp = DynamicVars["ReviveHp"].IntValue;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain); // 升级获得保留
    }
}
