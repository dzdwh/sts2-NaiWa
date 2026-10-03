using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using NaiWa.relics;

namespace NaiWa.Events;

public sealed class ShiKongLieXi : EventModel
{
	/// <summary>Set by Harmony when BGM starts.</summary>
	public static AudioStreamPlayer? CurrentBgm;

	public override bool IsDeterministic => false;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new HealVar(25m),
	};

	public override bool IsAllowed(IRunState runState)
	{
		// Cannot use Owner here — IsAllowed is called during map generation before Owner is set.
		return runState.Players.Any(p => p.Character is global::NaiWa.Characters.NaiWa);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new EventOption[]
		{
			new EventOption(this, ListenCarefully,
				Owner?.Character is global::NaiWa.Characters.NaiWa
					? "SHI_KONG_LIE_XI.pages.INITIAL.options.LISTEN_CAREFULLY"
					: "SHI_KONG_LIE_XI.pages.INITIAL.options.LISTEN_CAREFULLY_RELIC"),
			new EventOption(this, ListenCasually, "SHI_KONG_LIE_XI.pages.INITIAL.options.LISTEN_CASUALLY"),
		};
	}

	private async Task ListenCarefully()
	{
		StopBgm();
		await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, 8, false);
		if (Owner?.Character is global::NaiWa.Characters.NaiWa)
			await RelicCmd.Obtain<ShiZuWeiDai>(Owner);
		else
		{
			// 非奶蛙玩家获得随机遗物
			var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
			await RelicCmd.Obtain(relic, Owner);
		}
		SetEventFinished(L10NLookup("SHI_KONG_LIE_XI.pages.CAREFULLY.description"));
	}

	private async Task ListenCasually()
	{
		StopBgm();
		await CreatureCmd.Heal(Owner.Creature, (int)(Owner.Creature.MaxHp * 0.20m));
		SetEventFinished(L10NLookup("SHI_KONG_LIE_XI.pages.CASUALLY.description"));
	}

	internal static void StopBgm()
	{
		if (CurrentBgm != null && GodotObject.IsInstanceValid(CurrentBgm))
		{
			CurrentBgm.Stop();
			CurrentBgm.QueueFree();
			CurrentBgm = null;
		}
	}
}
