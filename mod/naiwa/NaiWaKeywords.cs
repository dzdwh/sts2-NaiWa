using MegaCrit.Sts2.Core.Entities.Cards;

namespace NaiWa;

/// <summary>
/// Custom keywords for NaiWa mod.
/// </summary>
public static class NaiWaKeywords
{
    /// <summary>笑牌 — cards that trigger laugh animation and synergize with laugh-related effects.</summary>
    public static readonly CardKeyword Laugh = (CardKeyword)2000;
    /// <summary>绷住 — 任意大笑清空层数；若由大笑牌触发则该牌额外打出层数次。</summary>
    public static readonly CardKeyword BengZhu = (CardKeyword)2001;
    /// <summary>糖分 — 回合内若层数不低于血量，触发大笑并跳过回合。</summary>
    public static readonly CardKeyword TangFen = (CardKeyword)2002;
    /// <summary>奶蛋 — 替主人承受伤害，每回合+1最大生命。</summary>
    public static readonly CardKeyword NaiDan = (CardKeyword)2003;
    /// <summary>奶狗 — 替主人承受伤害。</summary>
    public static readonly CardKeyword NaiGou = (CardKeyword)2004;
    /// <summary>奶蛇 — 替主人承受伤害。</summary>
    public static readonly CardKeyword NaiShe = (CardKeyword)2005;
    /// <summary>奶兔 — 替主人承受伤害。</summary>
    public static readonly CardKeyword NaiTu = (CardKeyword)2006;
    /// <summary>奶狮 — 替主人承受伤害。</summary>
    public static readonly CardKeyword NaiShi = (CardKeyword)2007;
    /// <summary>奶龙 — 替主人承受伤害，回合开始时对随机敌人造成等同于自身生命值的伤害。</summary>
    public static readonly CardKeyword NaiLong = (CardKeyword)2008;
    /// <summary>奶鸡 — 替主人承受伤害，回合开始时给予主人等同于自身生命值的能量。</summary>
    public static readonly CardKeyword NaiJi = (CardKeyword)2009;
    /// <summary>奶鼠 — HP永久继承，每回合随机效果。</summary>
    public static readonly CardKeyword NaiShu = (CardKeyword)2010;
    public static readonly CardKeyword YuanShu = (CardKeyword)2011;
    public static readonly CardKeyword NaiZhu = (CardKeyword)2012;
    public static readonly CardKeyword LaughCard = (CardKeyword)2013;
    /// <summary>奶鲸 — 在场时吞下其他召唤物。</summary>
    public static readonly CardKeyword NaiJing = (CardKeyword)2014;
    /// <summary>卡面作者（硬汉三角龙）— 钢背蛙等卡面的作者标注。</summary>
    public static readonly CardKeyword Artist = (CardKeyword)2015;
    /// <summary>本回合可免费打出（朋友给的召唤牌标记，随卡同步保证多人一致）。</summary>
    public static readonly CardKeyword FreeThisTurn = (CardKeyword)2016;

    /// <summary>Types of cards that trigger laugh — used for gameplay detection (BeforeCardPlayed, power hooks, etc).</summary>
    public static readonly HashSet<System.Type> LaughCardTypes = new()
    {
        typeof(cards.NaiWaXiao),
        typeof(cards.NaiWaChongCi),
        typeof(cards.NaiWaHeHuNaiLi),
        typeof(cards.NaiWaPengFu),
        typeof(cards.NaiWaWanMeiNaiJi),
        typeof(cards.NaiWaYuanGuDaXiao),
        typeof(cards.NaiWaZhongBin),
        typeof(cards.NaiWaNahan),
        typeof(cards.NaiWaDaiZhenZhuErHuanDeNaiWa),
        typeof(cards.NaiWaZhuXingNaiWang),
        typeof(cards.NaiWaNaiXiTan),
        typeof(cards.NaiWaYouYu),
    };

    /// <summary>
    /// 是否是大笑牌：类型在 LaughCardTypes 中，或带有"大笑牌"关键词
    /// （自由引导奶蛙等通过 CardCmd.ApplyKeyword 添加的标记）。
    /// </summary>
    public static bool IsLaughCard(MegaCrit.Sts2.Core.Models.CardModel card)
        => card != null && (LaughCardTypes.Contains(card.GetType()) || card.Keywords.Contains(LaughCard));

    /// <summary>带有召唤效果的牌 — 用于"下一张召唤牌0费"等机制检测。</summary>
    public static readonly HashSet<System.Type> SummonCardTypes = new()
    {
        typeof(cards.NaiWaJiangWeiDaJi),
        typeof(cards.NaiWaNaiDanJiangLin),
        typeof(cards.NaiWaNaiGouWo),
        typeof(cards.NaiWaNaiJiHeDan),
        typeof(cards.NaiWaNaiLong),
        typeof(cards.NaiWaXiaoNaiZhu),
        typeof(cards.NaiWaYao),
        typeof(cards.NaiWaYongYuanDeNaiShu),
        typeof(cards.NaiWaDaKouNaiJing),
        typeof(cards.NaiWaPoBaiZhiNai),
        typeof(cards.NaiWaNaiGeLaDiZhiSi),
    };
}
