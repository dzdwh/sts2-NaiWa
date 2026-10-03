#if !TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Logging;
using NaiWa.Characters;
using NaiWaChar = NaiWa.Characters.NaiWa;

namespace NaiWa.scripts;

/// <summary>
/// Fix for THE_FUTURE_OF_POTIONS (and similar events) freezing on mod characters.
///
/// Root cause: Events like TheFutureOfPotions filter cards by rarity AND type.
/// For custom card pools with limited cards per rarity+type, the filtered pool
/// may be smaller than the requested card count. After the first pick is
/// blacklisted, subsequent calls to CreateForReward have nothing to pick from,
/// throwing InvalidOperationException that freezes the UI.
///
/// Fix: Harmony prefix on CardFactory.CreateForReward that catches the
/// "couldn't generate a valid card" exception for mod characters and falls
/// back to a broader card pool (all non-basic cards from the character's pool).
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForReward),
    new Type[] { typeof(Player), typeof(int), typeof(CardCreationOptions) })]
public static class NaiWaCardRewardFallback
{
    /// <summary>
    /// Thread-safe flag to prevent infinite recursion when we retry the
    /// original method call inside our Prefix.
    /// </summary>
    [ThreadStatic]
    private static bool _suppressPatch;

    [HarmonyPrefix]
    static bool Prefix(
        Player player,
        int cardCount,
        CardCreationOptions options,
        ref IEnumerable<CardCreationResult> __result)
    {
        // Already in fallback retry — let original method run normally
        if (_suppressPatch) return true;

        // Only intercept for NaiWa — vanilla characters are unaffected
        if (player?.Character is not NaiWaChar) return true;

        try
        {
            // Try the original method first (this will re-enter our Prefix,
            // but _suppressPatch will be true, so it passes through)
            _suppressPatch = true;
            // CreateForReward is lazy. Materialize it inside the try block so a
            // depleted filtered pool is caught here instead of escaping later
            // when CardReward enumerates the result.
            __result = CardFactory.CreateForReward(player, cardCount, options).ToList();
            return false; // success — use this result instead of calling original
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("couldn't generate a valid card"))
        {
            // Original failed due to insufficient cards in filtered pool.
            // Fall back to broader selection below.
        }
        finally
        {
            _suppressPatch = false;
        }

        // Generate fallback cards from the entire character pool
        __result = CreateFallbackReward(player, cardCount, options);
        return false; // skip original — we provided a fallback
    }

    /// <summary>
    /// Generates fallback cards from the character's entire unlocked card pool
    /// (non-basic, non-ancient), ignoring any type restriction in the original filter.
    /// </summary>
    private static IEnumerable<CardCreationResult> CreateFallbackReward(
        Player player, int cardCount, CardCreationOptions options)
    {
        var result = new List<CardCreationResult>();

        try
        {
            var allCards = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
                .ToList();

            if (allCards.Count == 0) return result;

            var rng = options.RngOverride ?? player.PlayerRng.Rewards;
            var blacklist = new List<CardModel>();

            for (int i = 0; i < cardCount; i++)
            {
                var available = allCards.Where(c => !blacklist.Contains(c.CanonicalInstance)).ToList();
                if (available.Count == 0) break;

                var pick = rng.NextItem(available);
                if (pick == null) break;

                blacklist.Add(pick.CanonicalInstance);
                var created = player.RunState.CreateCard(pick, player);
                result.Add(new CardCreationResult(created));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaRewardFallback] Fallback card generation failed: {ex.Message}");
        }

        return result;
    }
}
#endif
