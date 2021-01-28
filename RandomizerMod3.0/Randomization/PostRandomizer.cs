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
            if (RandomizerMod.Instance.Settings.CreateSpoilerLog)
            {
                if (RandomizerMod.Instance.Settings.IsMW)
                {
                    RandoLogger.LogSpoiler(result.spoiler);
                }
                else
                {
                    RandoLogger.LogAllToSpoiler(result);
                }
            }

            RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);

            if (RandomizerMod.Instance.Settings.IsMW)
            {
                try
                {
                    if (!RandomizerMod.Instance.mwConnection.IsConnected())
                    {
                        RandomizerMod.Instance.mwConnection.Connect();
                    }
                    RandomizerMod.Instance.mwConnection.JoinRando(RandomizerMod.Instance.Settings.MWRandoId, RandomizerMod.Instance.Settings.MWPlayerId);
                } catch (Exception) {}
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

            foreach (KeyValuePair<MWItem, int> kvp in result.variableCosts.Where(kvp => kvp.Key.PlayerId == result.playerId))
            {
                RandomizerMod.Instance.Settings.AddNewCost(kvp.Key.Item, kvp.Value);
            }

            foreach (var (item, shop) in VanillaManager.ItemPlacements.Where(p => LogicManager.ShopNames.Contains(p.Item2)))
            {
                RandomizerMod.Instance.Settings.AddShopCost(item, LogicManager.GetItemDef(item).shopCost);
            }

            foreach (var kvp in result.itemPlacements.Where(kvp => kvp.Value.PlayerId == result.playerId))
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(kvp.Key.ToString(), kvp.Value.Item);

                if (LogicManager.ShopNames.Contains(kvp.Value.Item) && !(kvp.Key.PlayerId == result.playerId && VanillaManager.GetVanillaItems(result.settings).Contains(kvp.Key.Item)))
                {
                    RandomizerMod.Instance.Settings.AddShopCost(kvp.Key.ToString(), result.shopCosts[kvp.Key]);
                }
            }

            for (int i = 0; i < result.startItems.Count; i++)
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(result.startItems[i], "Equipped_(" + i + ")");
            }

            foreach (var kvp in result.locationOrder.Where(kvp => kvp.Key.PlayerId == result.playerId))
            {
                RandomizerMod.Instance.Settings.AddOrderedLocation(kvp.Key.Item, kvp.Value);
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
