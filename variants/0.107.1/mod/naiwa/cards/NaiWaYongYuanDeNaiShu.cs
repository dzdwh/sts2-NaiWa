using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa.minions;

using NaiWa.powers;
namespace NaiWa.cards;

/// <summary>
/// 永远的奶鼠 - 2费rare能力牌。基础召唤8HP奶鼠，每次打出永久+3/4HP。
/// 模仿遗传算法/巨镰：DynamicVar 用实例字段，卡面显示累计值。
/// </summary>
public sealed class NaiWaYongYuanDeNaiShu : CardModel
{
    private int _baseHp = 8;
    private int _increasedHp;

    private int TotalHp => _baseHp + _increasedHp;

    [SavedProperty]
    public int BaseHp
    {
        get => _baseHp;
        set { AssertMutable(); _baseHp = value; }
    }

    [SavedProperty]
    public int IncreasedHp
    {
        get => _increasedHp;
        set
        {
            AssertMutable();
            _increasedHp = value;
            try { base.DynamicVars["MouseHp"].BaseValue = TotalHp; } catch { }
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("MouseHp", TotalHp),
        new DynamicVar("Growth", 3m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => System.Array.Empty<CardKeyword>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<NaiWaMouseBuffPower>(),
        HoverTipFactory.FromKeyword(NaiWaKeywords.YuanShu),
    };

    public override string PortraitPath =>
        "res://images/card_portraits/nai_wa/nai_wa_yong_yuan_de_nai_shu.png";

    public NaiWaYongYuanDeNaiShu() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        int hp = TotalHp;
        await MinionCmd.AddMinion<NaiWaMouseMinion>(ctx, Owner,
            new MinionSummonOptions(hp, Source: this, Position: MinionPosition.FrontUpper,
                AccumulatedHp: _increasedHp));

        int growth = DynamicVars["Growth"].IntValue;
        IncreasedHp += growth;
        base.DynamicVars["MouseHp"].BaseValue = TotalHp;

        (base.DeckVersion as NaiWaYongYuanDeNaiShu)?.BuffFromPlay(growth);
    }

    private void BuffFromPlay(int extra)
    {
        IncreasedHp += extra;
        base.DynamicVars["MouseHp"].BaseValue = TotalHp;
    }

    protected override void OnUpgrade()
    {
        // 升级值由 CurrentUpgradeLevel 推导（幂等）：多人下升级奖励若重复触发 OnUpgrade，
        // 双端仍得到一致结果，避免 BaseHp 漂移导致"数据不同步"。
        int targetBaseHp = 8; // 基础召唤固定8，不随升级提高
        if (BaseHp < targetBaseHp)
            BaseHp = targetBaseHp;
        base.DynamicVars["MouseHp"].BaseValue = TotalHp;
        int targetGrowth = 3 + CurrentUpgradeLevel;   // 3→4
        if (base.DynamicVars["Growth"].IntValue < targetGrowth)
            base.DynamicVars["Growth"].BaseValue = targetGrowth;
    }
}
