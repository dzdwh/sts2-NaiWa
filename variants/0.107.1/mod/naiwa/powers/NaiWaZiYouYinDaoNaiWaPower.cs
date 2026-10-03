using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using NaiWa;

namespace NaiWa.powers;

/// <summary>
/// 自由引导奶蛙 - 回合开始时选择一张手牌：不是大笑牌则令其获得大笑牌，否则打出这张牌。
/// </summary>
public sealed class NaiWaZiYouYinDaoNaiWaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player.Creature != base.Owner || !base.Owner.IsAlive) return;

        try
        {
            // 与原版 Entropy/Tyranny 一样，在同一个回合开始选择中一次选完。
            // HookPlayerChoiceContext 会负责多人暂停、同步和恢复；避免每层连续创建
            // 独立 choice id，也不再需要把多人模式整个禁用。
            var cards = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, base.Amount),
                context: choiceContext,
                player: player,
                filter: null,
                source: this)).ToList();

            foreach (var card in cards)
            {
                if (!NaiWaKeywords.IsLaughCard(card))
                {
                    CardCmd.ApplyKeyword(card, NaiWaKeywords.LaughCard); // 添加"大笑牌"
                }
                else
                {
                    await CardCmd.AutoPlay(choiceContext, card, null); // 打出这张牌（免费自动打出）
                }
            }
        }
        catch (Exception ex)
        {
            // 防御：选牌失败不能阻断回合开始（遥测 8/20 多人 PlayCardAction 同类崩点）
            Log.Error($"[ZiYouYinDao] 回合开始选牌失败: {ex}");
        }
    }

    /// <summary>
    /// 打出"被添加大笑牌关键词"的普通卡时，补触发大笑逻辑（动画/记录/奶蛙的世界等）。
    /// 真正的笑牌（LaughCardTypes）自身 OnPlay 已触发大笑，这里不再重复。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (cardPlay.Card.Owner?.Creature != base.Owner) return;
        if (!NaiWaKeywords.LaughCardTypes.Contains(cardPlay.Card.GetType())
            && NaiWaKeywords.IsLaughCard(cardPlay.Card))
        {
            if (await cards.NaiWaXiao.TriggerLaughTracking(choiceContext, cardPlay.Card.Owner))
                cards.NaiWaXiao.TriggerLaughVisual(cardPlay.Card.Owner);
        }
    }
}

#if !TOOLS
/// <summary>
/// 让被添加"大笑牌"关键词的普通卡在卡面描述里显示"大笑牌"标签
/// （游戏只自动渲染标准关键词，自定义关键词需手动附加文字，仿雕琢打击的"虚无"）。
/// </summary>
[HarmonyLib.HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.CardModel), nameof(MegaCrit.Sts2.Core.Models.CardModel.GetDescriptionForPile),
    new System.Type[]
    {
        typeof(MegaCrit.Sts2.Core.Entities.Cards.PileType),
        typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),
    })]
public class NaiWaLaughCardDescriptionPatch
{
    static void Postfix(MegaCrit.Sts2.Core.Models.CardModel __instance, ref string __result)
    {
        if (__instance == null || __result == null) return;
        if (!__instance.Keywords.Contains(NaiWaKeywords.LaughCard)) return;
        string title = new MegaCrit.Sts2.Core.Localization.LocString(
            "powers", "NAI_WA_LAUGH_CARD_POWER.title").GetFormattedText();
        if (__result.Contains(title)) return; // 使用当前语言的标签，防重复。
        __result += $"\n[gold]{title}[/gold]";
    }
}
#endif
