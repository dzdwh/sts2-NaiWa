using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa.minions;

namespace NaiWa.powers;

/// <summary>
/// 死生契阔 - 施加在当前承伤随从身上的 buff：
/// 若该随从本回合死亡，下回合开始时以 ReviveHp 生命复活，随后 buff 消耗。
/// </summary>
public sealed class NaiWaSiShengQiKuoPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private int _reviveHp = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("ReviveHp", 3m),
    };

    /// <summary>复活后的生命值（基础 3，升级 5）。双端由卡牌打出时以相同值设置。</summary>
    [SavedProperty]
    public int ReviveHp
    {
        get => _reviveHp;
        set
        {
            AssertMutable();
            _reviveHp = value;
            DynamicVars["ReviveHp"].BaseValue = value;
        }
    }

    /// <summary>随从死亡后 buff 必须留在尸体上，否则下回合开始前就被移除。</summary>
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    /// <summary>
    /// 下一个回合开始结算：死了就复活（尸体留在 Allies，走 MinionCmd 的官方复活流程），
    /// 没死则过期；无论哪种情况都消耗掉 buff。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || base.Owner.PetOwner == null) return;
        if (player != base.Owner.PetOwner) return;

        var minion = base.Owner;
        if (!minion.IsAlive)
        {
            await MinionCmd.ReviveCorpse(choiceContext, player, minion, _reviveHp, null);
        }
        await PowerCmd.Remove(this);
    }
}
