using System.Collections.Generic;
using System.Linq;
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
/// 大耳奶兜 - X费uncommon技能。消耗。给予所有敌人12X/17X层糖分，获得1张糖化。
/// 若结算后手牌糖化数量恰好等于X，获得X能量并回到手牌。
/// </summary>
public sealed class NaiWaDaErNaiDou : CardModel
{
    private bool _returnToHand;

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaTangHua>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("SugarPerX", 12m),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_daernaidou.png";

    public NaiWaDaErNaiDou() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int x = ResolveEnergyXValue();
        int perEnemy = DynamicVars["SugarPerX"].IntValue * x;
        if (perEnemy > 0)
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<NaiWaTangFenPower>(choiceContext, enemy, perEnemy, Owner.Creature, this);
        }

        await NaiWaTangHua.GiveToHand(choiceContext, Owner);

        // 新效果：此时手牌中的糖化数量恰好等于 X -> 获得 X 能量，这张牌回到手牌
        _returnToHand = false;
        int handTangHua = Owner.PlayerCombatState.Hand.Cards.Count(c => c is NaiWaTangHua);
        if (handTangHua == x)
        {
            _returnToHand = true;
            await PlayerCmd.GainEnergy(x, Owner);
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_returnToHand && base.Pile?.Type != PileType.Hand)
        {
            _returnToHand = false;
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SugarPerX"].UpgradeValueBy(5m); // 12→17
    }
}
