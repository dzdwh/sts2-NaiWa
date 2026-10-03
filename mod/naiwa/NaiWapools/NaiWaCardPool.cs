using Godot;
using MegaCrit.Sts2.Core.Models;

namespace NaiWa.NaiWapools;

public sealed class NaiWaCardPool : CardPoolModel
{
    public override string Title => "nai_wa";
    public override string EnergyColorName => "nai_wa";
    public override string CardFrameMaterialPath => "card_frame_yellow";
    public override Color DeckEntryCardColor => new("7BC67E");
    public override Color EnergyOutlineColor => new("2E7D32");
    public override bool IsColorless => false;

    private static CardModel[]? _cachedAllCards;

    protected override CardModel[] GenerateAllCards()
    {
        if (_cachedAllCards != null) return _cachedAllCards;
        _cachedAllCards = new CardModel[]
        {
            ModelDb.Card<cards.NaiWaStrike>(),
            ModelDb.Card<cards.NaiWaDefend>(),
            ModelDb.Card<cards.NaiWaXiao>(),
            ModelDb.Card<cards.NaiWaFengTi>(),
            ModelDb.Card<cards.NaiWaYao>(),
            ModelDb.Card<cards.NaiWaShiTou>(),
            ModelDb.Card<cards.NaiWaBeng>(),
        ModelDb.Card<cards.NaiWaDao>(),
        ModelDb.Card<cards.NaiWaPengFu>(),
        ModelDb.Card<cards.NaiWaZhongBin>(),
        ModelDb.Card<cards.NaiWaYouYu>(),
            ModelDb.Card<cards.NaiWaShiJie>(),
            ModelDb.Card<cards.NaiWaXin>(),
            ModelDb.Card<cards.NaiWaNahan>(),
            ModelDb.Card<cards.NaiWaJiushu>(),
            ModelDb.Card<cards.NaiWaNainasi>(),
            ModelDb.Card<cards.NaiWaShengTang>(),
            ModelDb.Card<cards.NaiWaNingShi>(),
            ModelDb.Card<cards.NaiWaJiangTang>(),
            ModelDb.Card<cards.NaiWaBei>(),
            ModelDb.Card<cards.NaiWaChouXiang>(),
            ModelDb.Card<cards.NaiWaDuoBiShiJian>(),
			ModelDb.Card<cards.NaiGouTi>(),
            ModelDb.Card<cards.NaiWaChongCi>(),
		ModelDb.Card<cards.NaiWaWanMeiNaiJi>(),
            ModelDb.Card<cards.NaiWaWeiYa>(),
            ModelDb.Card<cards.NaiWaSiZhiTangXinJian>(),
            ModelDb.Card<cards.NaiWaNaiLong>(),
		ModelDb.Card<cards.NaiWaTangZhu>(),
		ModelDb.Card<cards.NaiWaBaiRiMeng>(),
		ModelDb.Card<cards.NaiWaQingXing>(),
			ModelDb.Card<cards.NaiWaRouRen>(),
			ModelDb.Card<cards.NaiWaNaiDanJiangLin>(),
				ModelDb.Card<cards.NaiWaNaiGouWo>(),
			ModelDb.Card<cards.NaiWaHeHuNaiLi>(),
			ModelDb.Card<cards.NaiWaSiWeiNaiHua>(),
			ModelDb.Card<cards.NaiWaWanWuGuiNai>(),
			ModelDb.Card<cards.NaiWaPengYou>(),
			ModelDb.Card<cards.NaiWaChaoTangXingTai>(),
			ModelDb.Card<cards.NaiRiKui>(),
			ModelDb.Card<cards.NaiWaYuanGuDaXiao>(),
			ModelDb.Card<cards.NaiWaTianXiaWuShuang>(),
			ModelDb.Card<cards.NaiWaNaiJiHeDan>(),
			ModelDb.Card<cards.NaiWaSiShengQiKuo>(),
			ModelDb.Card<cards.NaiWaYongYuanDeNaiShu>(),
			ModelDb.Card<cards.NaiWaNaiLiangKongChu>(),
			ModelDb.Card<cards.NaiWaGeErNaiDa>(),
			ModelDb.Card<cards.NaiWaZuiHouDeNaiCan>(),
			ModelDb.Card<cards.NaiWaKuaYueNaiErBeiSiShan>(),
			ModelDb.Card<cards.NaiWaNaiGuoRen>(),
			ModelDb.Card<cards.NaiWaNaiXiTan>(),
			ModelDb.Card<cards.NaiWaNaiNaiZaiShangDesuwa>(),
			ModelDb.Card<cards.NaiWaNaiShiHou>(),
			ModelDb.Card<cards.NaiWaNaiWaZhiZi>(),
			ModelDb.Card<cards.NaiWaSiXiaoNaiE>(),
			ModelDb.Card<cards.NaiWaMengLong>(),
			ModelDb.Card<cards.NaiWaNaiNu>(),
			ModelDb.Card<cards.NaiWaNaiJing>(),
			ModelDb.Card<cards.NaiWaZiHuaXiang>(),
			ModelDb.Card<cards.NaiWaDaKouNaiJing>(),
			ModelDb.Card<cards.NaiWaWoTaZhiRuWa>(),
			ModelDb.Card<cards.NaiWaNaiGeLaDiZhiSi>(),
			ModelDb.Card<cards.NaiWaZiYouYinDaoNaiWa>(),
			ModelDb.Card<cards.NaiWaDuoLuoNaiWa>(),
			ModelDb.Card<cards.NaiWaJiangJunJiLuoLeYiZhiNaiWa>(),
			ModelDb.Card<cards.NaiWaXiaoNaiZhu>(),
			ModelDb.Card<cards.NaiWaNaiWu>(),
			ModelDb.Card<cards.NaiWaNaiWaDeYongHeng>(),
			ModelDb.Card<cards.NaiWaWangRi>(),
			ModelDb.Card<cards.NaiWaChuangZaoNaiWa>(),
			ModelDb.Card<cards.NaiWaPoBaiZhiNai>(),
			ModelDb.Card<cards.NaiWaGangBeiWa>(),
			ModelDb.Card<cards.NaiWaShuYuFen>(),
			ModelDb.Card<cards.NaiWaNaiKong>(),
				ModelDb.Card<cards.NaiWaBuZhiNaiWu>(),
				ModelDb.Card<cards.NaiWaGuoLiangYiZhi>(),
				ModelDb.Card<cards.NaiWaJiangWeiDaJi>(),
				ModelDb.Card<cards.NaiWaDaErNaiDou>(),
				ModelDb.Card<cards.NaiWaNaiJiao>(),
				ModelDb.Card<cards.NaiWaNaiSeWang>(),
				ModelDb.Card<cards.NaiWaShenFu>(),
				ModelDb.Card<cards.NaiWaTangJiKeDe>(),
				ModelDb.Card<cards.NaiWaDaiZhenZhuErHuanDeNaiWa>(),
				ModelDb.Card<cards.NaiWaMengNaiLiSha>(),
				ModelDb.Card<cards.NaiWaShenNaiChuanChongLangLi>(),
				ModelDb.Card<cards.NaiWaZhuXingNaiWang>(),
				ModelDb.Card<cards.NaiWaNaiDao>(),
				ModelDb.Card<cards.NaiWaJingHongYiPie>(),
				ModelDb.Card<cards.NaiWaNaiPu>(),
				ModelDb.Card<cards.NaiWaNaiZhiShou>(),
        };
        return _cachedAllCards;
    }
}
