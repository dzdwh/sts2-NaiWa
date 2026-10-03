using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.cards;

/// <summary>
/// 戈尔奶达 - X费rare技能牌。抽X张牌（升级X+1），选择手牌中的一张牌打出X-1次。
/// 模仿原版抉择抉择（DecisionsDecisions）+ X费用卡。
/// </summary>
public sealed class NaiWaGeErNaiDa : CardModel
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_ge_er_nai_da.png";

    public NaiWaGeErNaiDa() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // A stale/autoplayed card can finish after its owner has left the run.
        // PlayerChoiceSynchronizer cannot reserve a choice for slot -1.
        if (Owner.RunState.GetPlayerSlotIndex(Owner) < 0) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int x = ResolveEnergyXValue();
        int drawCount = base.IsUpgraded ? x + 1 : x;
        int playCount = Math.Max(0, x - 1); // 打出X-1次

        await CardPileCmd.Draw(choiceContext, drawCount, Owner);

        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1)
        {
            PretendCardsCanBePlayed = true
        };
        var card = (await CardSelectCmd.FromHand(
            choiceContext, Owner, prefs,
            (CardModel c) => !c.Keywords.Contains(CardKeyword.Unplayable), this)).FirstOrDefault();
        if (card != null && playCount > 0)
        {
            // 模仿官方回响形态/隐藏宝石的 playCount 机制：临时给选中牌加 ReplayCount，
            // 单次 AutoPlay 让 OnPlayWrapper 在一次打出里循环执行 OnPlay（共 X-1 次），打完恢复。
            // 不能用循环 AutoPlay 同一张牌：能力牌第一次打出后会被 RemoveFromCombat（Pile=null），
            // 第二次 AutoPlay 时 CardPileCmd.Add 静默失败、OnPlayWrapper 因 CombatState==null 直接 return
            // → 能力牌只打出1次（格尔奶达三费选奶日葵等能力牌的 bug）。
            int extra = playCount - 1; // 基础1次之外的额外次数 = X-2
            card.BaseReplayCount += extra;
            try
            {
                await CardCmd.AutoPlay(choiceContext, card, null);
            }
            finally
            {
                card.BaseReplayCount -= extra;
            }
        }
    }

    protected override void OnUpgrade() { }
}
