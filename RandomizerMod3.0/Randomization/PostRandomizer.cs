using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RandomizerMod.LogHelper;
using static RandomizerMod.Randomization.Randomizer;

namespace RandomizerMod.Randomization
{
    internal static class PostRandomizer
    {
        public static void PostRandomizationTasks(ItemManager im, TransitionManager tm, string StartName, List<string> startItems, Dictionary<string, int> modifiedCosts)
        {
            RemovePlaceholders(im);
            SaveAllPlacements(im, tm, StartName, startItems, modifiedCosts);
            //No vanilla'd loctions in the spoiler log, please!
            (int, string, string)[] orderedILPairs = RandomizerMod.Instance.Settings.ItemPlacements.Except(VanillaManager.ItemPlacements)
                .Select(pair => (pair.Item2.StartsWith("Equip") ? 0 : im.locationOrder[pair.Item2], pair.Item1, pair.Item2)).ToArray();
            if (RandomizerMod.Instance.Settings.CreateSpoilerLog) RandoLogger.LogAllToSpoiler(orderedILPairs, RandomizerMod.Instance.Settings._transitionPlacements.Select(kvp => (kvp.Key, kvp.Value)).ToArray(), modifiedCosts);
        }

        private static void RemovePlaceholders(ItemManager im)
        {
            if (RandomizerMod.Instance.Settings.DuplicateMajorItems)
            {
                // Duplicate items should not be placed very early in logic
                int minimumDepth = Math.Min(im.locationOrder.Count / 5, im.locationOrder.Count - 2 * im.duplicatedItems.Count);
                int maximumDepth = im.locationOrder.Count;
                bool ValidIndex(int i)
                {
                    string location = im.locationOrder.FirstOrDefault(kvp => kvp.Value == i).Key;
                    return !string.IsNullOrEmpty(location) && !LogicManager.ShopNames.Contains(location) && !LogicManager.GetItemDef(im.nonShopItems[location]).progression;
                }
                List<int> allowedDepths = Enumerable.Range(minimumDepth, maximumDepth).Where(i => ValidIndex(i)).ToList();
                Random rand = new Random(RandomizerMod.Instance.Settings.Seed + 29);

                foreach (string majorItem in im.duplicatedItems)
                {
                    while (allowedDepths.Any())
                    {
                        int depth = allowedDepths[rand.Next(allowedDepths.Count)];
                        string location = im.locationOrder.First(kvp => kvp.Value == depth).Key;
                        string swapItem = im.nonShopItems[location];
                        string toShop = LogicManager.ShopNames.OrderBy(shop => im.shopItems[shop].Count).First();

                        im.nonShopItems[location] = majorItem + "_(1)";
                        im.shopItems[toShop].Add(swapItem);
                        allowedDepths.Remove(depth);
                        break;
                    }
                }
            }
        }

        private static void SaveAllPlacements(ItemManager im, TransitionManager tm, string StartName, List<String> startItems, Dictionary<string, int> modifiedCosts)
        {
            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                foreach (KeyValuePair<string, string> kvp in tm.transitionPlacements)
                {
                    RandomizerMod.Instance.Settings.AddTransitionPlacement(kvp.Key, kvp.Value);
                    // For map tracking
                    //     RandoLogger.LogTransitionToTracker(kvp.Key, kvp.Value);
                }
            }

            foreach (KeyValuePair<string, int> kvp in modifiedCosts)
            {
                RandomizerMod.Instance.Settings.AddNewCost(kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<string, List<string>> kvp in im.shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (VanillaManager.ItemPlacements.Contains((item, kvp.Key))) continue;
                    RandomizeShopCost(item);
                }
            }

            foreach (var (item, shop) in VanillaManager.ItemPlacements.Where(p => LogicManager.ShopNames.Contains(p.Item2)))
            {
                RandomizerMod.Instance.Settings.AddShopCost(item, LogicManager.GetItemDef(item).shopCost);
            }

            foreach ((string, string) pair in GetPlacedItemPairs(im))
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(pair.Item1, pair.Item2);
            }

            for (int i = 0; i < startItems.Count; i++)
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(startItems[i], "Equipped_(" + i + ")");
            }

            foreach (var kvp in im.locationOrder)
            {
                RandomizerMod.Instance.Settings.AddOrderedLocation(kvp.Key, kvp.Value);
            }

            RandomizerMod.Instance.Settings.StartName = StartName;
            StartDef startDef = LogicManager.GetStartLocation(StartName);
            RandomizerMod.Instance.Settings.StartSceneName = startDef.sceneName;
            RandomizerMod.Instance.Settings.StartRespawnMarkerName = StartSaveChanges.RESPAWN_MARKER_NAME;
            RandomizerMod.Instance.Settings.StartRespawnType = 0;
            RandomizerMod.Instance.Settings.StartMapZone = (int)startDef.zone;
        }

        public static void RandomizeShopCost(string item)
        {
            int cost;
            ReqDef def = LogicManager.GetItemDef(item);

            /*
            if (!RandomizerMod.Instance.Settings.GetRandomizeByPool(def.pool))
            {
                // This probably isn't ever called
                cost = def.cost;
            }
            else
            */
            {
                Random rand = new Random(RandomizerMod.Instance.Settings.Seed + item.GetHashCode()); // make shop item cost independent from prior randomization

                int baseCost = 100;
                int increment = 10;
                int maxCost = 500;

                int priceFactor = 1;
                if (def.geo > 0) priceFactor = 0;
                if (item.StartsWith("Soul_Totem") || item.StartsWith("Lore_Tablet")) priceFactor = 0;
                if (item.StartsWith("Rancid") || item.StartsWith("Mask")) priceFactor = 2;
                if (item.StartsWith("Pale_Ore") || item.StartsWith("Charm_Notch")) priceFactor = 3;
                if (item == "Focus") priceFactor = 10;
                if (item.StartsWith("Godtuner") || item.StartsWith("Collector") || item.StartsWith("World_Sense")) priceFactor = 0;
                cost = baseCost + increment * rand.Next(1 + (maxCost - baseCost)/increment); // random from 100 to 500 inclusive, multiples of 10
                cost *= priceFactor;
            }

            cost = Math.Max(cost, 1);
            RandomizerMod.Instance.Settings.AddShopCost(item, cost);
        }

        public static List<(string, string)> GetPlacedItemPairs(ItemManager im)
        {
            List<(string, string)> pairs = new List<(string, string)>();
            foreach (KeyValuePair<string, List<string>> kvp in im.shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    pairs.Add((item, kvp.Key));
                }
            }
            foreach (KeyValuePair<string, string> kvp in im.nonShopItems)
            {
                pairs.Add((kvp.Value, kvp.Key));
            }

            //Vanilla Item Placements (for RandomizerActions, Hints, Logs, etc)
            foreach ((string, string) pair in VanillaManager.ItemPlacements)
            {
                pairs.Add((pair.Item1, pair.Item2));
            }

            return pairs;
        }

        public static void LogItemPlacements(ItemManager im, ProgressionManager pm)
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
