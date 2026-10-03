using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.cards;

namespace NaiWa.powers;

/// <summary>
/// 奶量空处 - 回合开始时：抽牌堆加入1张糖化，并获得 Amount × Block 点格挡。
/// Counter：每次打出叠加层数（每层 9/11 格挡）。
/// </summary>
public sealed class NaiWaNaiLiangKongChuPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Block", 9m),
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (base.Owner == null || !base.Owner.IsAlive) return;
        if (player.Creature != base.Owner) return;

        // 抽牌堆加入1张糖化
        await NaiWaTangHua.GiveToDrawPile(choiceContext, player);
        // 获得 Amount × Block 点格挡
        int block = base.Amount * base.DynamicVars["Block"].IntValue;
        if (block > 0)
            await CreatureCmd.GainBlock(base.Owner, block, ValueProp.Unpowered, null);
    }
}
