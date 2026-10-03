using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NaiWa.relics;

/// <summary>
/// 始祖肚皮 - 在休息处消化激活，3场战斗后获得随机卡牌奖励。
/// </summary>
public sealed class ShiZuWeiDai : RelicModel
{
    private int _digestCount;
    private bool _rewardPending;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool ShowCounter => true;
    public override string PackedIconPath => "res://images/relics/shi_zu_wei_dai.png";
    public override int DisplayAmount => _digestCount;

    [SavedProperty]
    public int DigestCount
    {
        get => _digestCount;
        set
        {
            AssertMutable();
            _digestCount = value;
            base.Status = (_digestCount <= 0) ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public bool RewardPending
    {
        get => _rewardPending;
        set
        {
            AssertMutable();
            _rewardPending = value;
        }
    }

    /// <summary>刚捡起时是失活（灰色）状态，休息处消化后才激活。</summary>
    public override Task AfterObtained()
    {
        base.Status = RelicStatus.Disabled;
        return Task.CompletedTask;
    }

    public void Activate()
    {
        DigestCount = 3;
        Flash();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        // 只算普通战斗：精英/Boss 不消耗次数
        if (room?.RoomType != RoomType.Monster) return Task.CompletedTask;
        if (DigestCount > 0)
        {
            DigestCount--;
            // 归零的这场战斗，在随后的奖励界面追加卡牌奖励
            // （AfterCombatEnd 先于 RewardsSet.Offer/ModifyRewards 执行，不能用 DigestCount 判断）
            if (DigestCount == 0)
                RewardPending = true;
        }
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner || room == null) return false;
        if (room.RoomType is not (RoomType.Monster or RoomType.Elite or RoomType.Boss)) return false;
        if (!RewardPending) return false;
        RewardPending = false;
        Flash();
        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        return true;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner || DigestCount > 0) return false;
        options.Add(new ShiZuDigestOption(player));
        return true;
    }
}
