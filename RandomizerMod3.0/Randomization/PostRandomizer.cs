using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RandomizerMod.LogHelper;

using RandomizerLib;
using RandomizerLib.MultiWorld;
using Newtonsoft.Json;
using RandomizerMod.Actions;

namespace RandomizerMod.Randomization
{
    internal static class PostRandomizer
    {
        public static void PostRandomizationTasks(RandoResult result)
        {
            VanillaManager.SetupVanilla(result.settings);

            RandomizerMod.Instance.Settings.MWPlayerId = result.playerId;
            RandomizerMod.Instance.Settings.MWNumPlayers = result.players;
            RandomizerMod.Instance.Settings.MWRandoId = result.randoId;
            RandomizerMod.Instance.Settings.SetMWNames(result.nicknames);
            LanguageStringManager.SetMWNames(result.nicknames);

            SaveAllPlacements(result);
            if (RandomizerMod.Instance.Settings.CreateSpoilerLog) RandoLogger.LogAllToSpoiler(result);

            RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);

            if (RandomizerMod.Instance.Settings.IsMW)
            {
                RandomizerMod.Instance.mwConnection.JoinRando(RandomizerMod.Instance.Settings.MWRandoId, RandomizerMod.Instance.Settings.MWPlayerId);
            }
        }

        private static void SaveAllPlacements(RandoResult result)
        {
            if (result.settings.RandomizeTransitions)
            {
                foreach (KeyValuePair<string, string> kvp in result.transitionPlacements)
                {
                    RandomizerMod.Instance.Settings.AddTransitionPlacement(kvp.Key, kvp.Value);
                    // For map tracking
                    //     RandoLogger.LogTransitionToTracker(kvp.Key, kvp.Value);
                }
            }

            foreach (KeyValuePair<string, int> kvp in result.variableCosts)
            {
                RandomizerMod.Instance.Settings.AddNewCost(kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<MWItem, int> kvp in result.shopCosts)
            {
                if (kvp.Key.PlayerId == result.playerId && VanillaManager.GetVanillaItems(result.settings).Contains(kvp.Key.Item)) continue;
                RandomizerMod.Instance.Settings.AddShopCost(kvp.Key.ToString(), kvp.Value);
            }

            foreach (var (item, shop) in VanillaManager.ItemPlacements.Where(p => LogicManager.ShopNames.Contains(p.Item2)))
            {
                RandomizerMod.Instance.Settings.AddShopCost(item, LogicManager.GetItemDef(item).shopCost);
            }

            foreach (KeyValuePair<MWItem, string> kvp in result.itemPlacements)
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(kvp.Key.ToString(), kvp.Value);
            }

            for (int i = 0; i < result.startItems.Count; i++)
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(result.startItems[i], "Equipped_(" + i + ")");
            }

            foreach (var kvp in result.locationOrder)
            {
                RandomizerMod.Instance.Settings.AddOrderedLocation(kvp.Key, kvp.Value);
            }

            RandomizerMod.Instance.Settings.StartName = result.settings.StartName;
            StartDef startDef = LogicManager.GetStartLocation(result.settings.StartName);
            RandomizerMod.Instance.Settings.StartSceneName = startDef.sceneName;
            RandomizerMod.Instance.Settings.StartRespawnMarkerName = StartSaveChanges.RESPAWN_MARKER_NAME;
            RandomizerMod.Instance.Settings.StartRespawnType = 0;
            RandomizerMod.Instance.Settings.StartMapZone = (int)startDef.zone;
            RandomizerMod.Instance.Settings.Randomizer = true;
        }
    }
}
