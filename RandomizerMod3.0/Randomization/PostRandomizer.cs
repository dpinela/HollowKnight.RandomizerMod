using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RandomizerMod.LogHelper;

using RandomizerLib;
using RandomizerLib.MultiWorld;

namespace RandomizerMod.Randomization
{
    internal static class PostRandomizer
    {
        public static void PostRandomizationTasks(RandoResult result)
        {
            VanillaManager.SetupVanilla(result.settings);

            SaveAllPlacements(result);
            //No vanilla'd loctions in the spoiler log, please!
            /*(int, string, string)[] orderedILPairs = RandomizerMod.Instance.Settings.ItemPlacements.Except(VanillaManager.ItemPlacements)
                .Select(pair => (pair.Item2.StartsWith("Equip") ? 0 : im.locationOrder[pair.Item2], pair.Item1, pair.Item2)).ToArray();
            if (RandomizerMod.Instance.Settings.CreateSpoilerLog) RandoLogger.LogAllToSpoiler(orderedILPairs, RandomizerMod.Instance.Settings._transitionPlacements.Select(kvp => (kvp.Key, kvp.Value)).ToArray(), modifiedCosts);*/
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
                if (kvp.Key.playerId != result.playerId) continue;
                if (VanillaManager.GetVanillaItems(result.settings).Contains(kvp.Key.item)) continue;
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

            /*foreach (var kvp in im.locationOrder)
            {
                RandomizerMod.Instance.Settings.AddOrderedLocation(kvp.Key, kvp.Value);
            }*/

            RandomizerMod.Instance.Settings.StartName = result.settings.StartName;
            StartDef startDef = LogicManager.GetStartLocation(result.settings.StartName);
            RandomizerMod.Instance.Settings.StartSceneName = startDef.sceneName;
            RandomizerMod.Instance.Settings.StartRespawnMarkerName = StartSaveChanges.RESPAWN_MARKER_NAME;
            RandomizerMod.Instance.Settings.StartRespawnType = 0;
            RandomizerMod.Instance.Settings.StartMapZone = (int)startDef.zone;
        }

        public static void LogItemPlacements(RandoResult result)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("All Item Placements:");
            foreach ((string, string) pair in GetPlacedItemPairs(im))
            {
                ReqDef def = LogicManager.GetItemDef(pair.Item1);
                if (def.progression) sb.AppendLine($"--{pm.CanGet(pair.Item2)} - {pair.Item1} -at- {pair.Item2}");
            }

            Log(sb.ToString());
        }
    }
}
