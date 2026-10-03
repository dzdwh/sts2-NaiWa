using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using NaiWa.powers;

namespace NaiWa.cards;

/// <summary>
/// 不知奶舞 - 2费rare能力。多人模式牌。将4张Token牌尽量均分给玩家，Power追踪打出顺序。
/// </summary>
public sealed class NaiWaBuZhiNaiWu : CardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_buzhinaiwu.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<NaiWaGaGaDiLaXia>(),
        HoverTipFactory.FromCard<NaiWaGaJiaoSai>(),
        HoverTipFactory.FromCard<NaiWaHouLa>(),
        HoverTipFactory.FromCard<NaiWaGanBaDe>(),
    };

    public NaiWaBuZhiNaiWu() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // Apply the Power
        await PowerCmd.Apply<NaiWaBuZhiNaiWuPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);

        // Evenly distribute 4 token cards among players (round-robin after shuffle)
        var combatState = CombatState;
        if (combatState == null) return;
        var players = combatState.Players.ToList();
        if (players.Count == 0) return;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        rng.Shuffle(players);

        // Shuffle the order of which card type goes to which player index
        var order = new List<int> { 0, 1, 2, 3 };
        rng.Shuffle(order);

        for (int i = 0; i < 4; i++)
        {
            var target = players[i % players.Count];
            switch (order[i])
            {
                case 0: await CreateAndDistribute<NaiWaGaGaDiLaXia>(target); break;
                case 1: await CreateAndDistribute<NaiWaGaJiaoSai>(target); break;
                case 2: await CreateAndDistribute<NaiWaHouLa>(target); break;
                case 3: await CreateAndDistribute<NaiWaGanBaDe>(target); break;
            }
        }
    }

    private async Task CreateAndDistribute<T>(Player target) where T : CardModel
    {
        var combatState = CombatState;
        if (combatState == null) return;
        var card = combatState.CreateCard<T>(target);
        var results = await CardPileCmd.AddGeneratedCardsToCombat(
            new[] { card }, PileType.Hand, Owner);
        if (LocalContext.IsMe(target))
            CardCmd.PreviewCardPileAdd(results);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
