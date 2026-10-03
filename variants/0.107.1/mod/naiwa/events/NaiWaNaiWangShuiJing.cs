using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NaiWa.relics;

namespace NaiWa.Events;

/// <summary>
/// 奶王水晶 - 第一幕事件。
/// A：失去50%生命值，获得奶王之卵。
/// B：失去100金币，获得随机遗物。
/// C：离开。
/// </summary>
public sealed class NaiWaNaiWangShuiJing : EventModel
{
	public override bool IsDeterministic => false;

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.Any(p => p.Character is global::NaiWa.Characters.NaiWa);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		bool isNaiWa = Owner?.Character is global::NaiWa.Characters.NaiWa;
		var optionA = isNaiWa
			? new EventOption(this, OptionSacrificeHp, "NAI_WA_NAI_WANG_SHUI_JING.pages.INITIAL.options.A")
				.WithRelic<NaiWaNaiWangZhiLuan>(Owner)
			: new EventOption(this, OptionSacrificeHp, "NAI_WA_NAI_WANG_SHUI_JING.pages.INITIAL.options.A_RELIC");
		var optionB = Owner.Gold >= 100
			? new EventOption(this, OptionPayGold, "NAI_WA_NAI_WANG_SHUI_JING.pages.INITIAL.options.B")
			: new EventOption(this, null, "NAI_WA_NAI_WANG_SHUI_JING.pages.INITIAL.options.B");
		return new EventOption[]
		{
			optionA,
			optionB,
			new EventOption(this, OptionLeave, "NAI_WA_NAI_WANG_SHUI_JING.pages.INITIAL.options.C", isProceed: true),
		};
	}

	private async Task OptionSacrificeHp()
	{
		// 失去 50% 生命值
		await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature,
			Owner.Creature.CurrentHp / 2m,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			Owner.Creature, null);
		if (Owner?.Character is global::NaiWa.Characters.NaiWa)
		{
			await RelicCmd.Obtain<NaiWaNaiWangZhiLuan>(Owner);
			SetEventFinished(L10NLookup("NAI_WA_NAI_WANG_SHUI_JING.pages.SACRIFICE.description"));
		}
		else
		{
			// 非奶蛙玩家获得随机稀有遗物
			var relic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
			await RelicCmd.Obtain(relic, Owner);
			SetEventFinished(L10NLookup("NAI_WA_NAI_WANG_SHUI_JING.pages.SACRIFICE_RELIC.description"));
		}
	}

	private async Task OptionPayGold()
	{
		if (Owner.Gold < 100)
		{
			return;
		}
		// 失去 100 金币，获得随机遗物
		await PlayerCmd.LoseGold(100, Owner, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);
		var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
		await RelicCmd.Obtain(relic, Owner);
		SetEventFinished(L10NLookup("NAI_WA_NAI_WANG_SHUI_JING.pages.PAY.description"));
	}

	private Task OptionLeave()
	{
		SetEventFinished(L10NLookup("NAI_WA_NAI_WANG_SHUI_JING.pages.LEAVE.description"));
		return Task.CompletedTask;
	}
}
