using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using NaiWa.cards;

namespace NaiWa.relics;

/// <summary>
/// 欧洛巴斯古老牙齿 — 将牌组中的奶笑蜕变为远古大笑。
/// 先古遗物，在先古之民事件中获取。
/// </summary>
public sealed class NaiWaGuLaoYaChi : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "res://images/relics/nai_wa_gu_lao_ya_chi.png";

    public override async Task AfterObtained()
    {
        try
        {
            var xiaoCard = Owner.Deck.Cards.FirstOrDefault(c => c is NaiWaXiao);
            if (xiaoCard == null) return;
            var ancientCard = Owner.RunState.CreateCard<NaiWaYuanGuDaXiao>(Owner);
            if (xiaoCard.IsUpgraded)
                CardCmd.Upgrade(ancientCard);
            // Remove from deck first, then add transformed card
            await CardPileCmd.RemoveFromDeck(xiaoCard);
            await CardPileCmd.Add(ancientCard, PileType.Deck);
            Flash();
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaGuLaoYaChi] Transform failed: {ex.Message}");
        }
    }
}
