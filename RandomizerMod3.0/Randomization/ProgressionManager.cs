using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using static RandomizerMod.LogHelper;

namespace RandomizerMod.Randomization
{
    public class ProgressionManager : IProgressionManager
    {
        public int[] obtained;

        private ItemManager shareIm;
        private TransitionManager shareTm;

        private Dictionary<string, int> grubLocations;
        private Dictionary<string, int> essenceLocations;
        private Dictionary<string, int> modifiedCosts;
        private bool temp;
        private bool share = true;
        public HashSet<string> tempItems;

        public ProgressionManager(RandomizerState state, ItemManager im = null, TransitionManager tm = null, int[] progression = null, bool concealRandomItems = false, Dictionary<string, int> modifiedCosts = null)
        {
            shareIm = im;
            shareTm = tm;

            obtained = new int[LogicManager.bitMaskMax + 1];
            if (progression != null) progression.CopyTo(obtained, 0);

            this.modifiedCosts = modifiedCosts;

            FetchEssenceLocations(state, concealRandomItems, im);
            FetchGrubLocations(state, im);

            ApplyDifficultySettings();
            RecalculateEssence();
            RecalculateGrubs();
        }

        public bool CanGet(string item)
        {
            return LogicManager.ParseProcessedLogic(item, obtained, modifiedCosts);
        }

        public void Add(string item)
        {
            item = LogicManager.RemovePrefixSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[a.Item2] |= a.Item1;
            if (temp)
            {
                tempItems.Add(item);
            }
            if (share)
            {
                Share(item);
            }
            RecalculateGrubs();
            RecalculateEssence();
            UpdateWaypoints();
        }

        public void Add(IEnumerable<string> items)
        {
            foreach (string item in items.Select(i => LogicManager.RemovePrefixSuffix(i)))
            {
                if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
                {
                    RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                    return;
                }
                obtained[a.Item2] |= a.Item1;
                if (temp)
                {
                    tempItems.Add(item);
                }
                if (share)
                {
                    Share(item);
                }
            }
            RecalculateGrubs();
            RecalculateEssence();
            UpdateWaypoints();
        }

        public void AddTemp(string item)
        {
            temp = true;
            if (tempItems == null)
            {
                tempItems = new HashSet<string>();
            }
            Add(item);
        }

        private void Share(string item)
        {
            if (shareIm != null && shareIm.recentProgression != null)
            {
                shareIm.recentProgression.Add(item);
            }

            if (shareTm != null && shareTm.recentProgression != null)
            {
                shareTm.recentProgression.Add(item);
            }
        }

        private void ToggleShare(bool value)
        {
            share = value;
        }

        public void Remove(string item)
        {
            item = LogicManager.RemovePrefixSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[a.Item2] &= ~a.Item1;
            if (LogicManager.grubProgression.Contains(item)) RecalculateGrubs();
            if (LogicManager.essenceProgression.Contains(item)) RecalculateEssence();
        }

        public void RemoveTempItems()
        {
            temp = false;
            foreach (string item in tempItems)
            {
                Remove(item);
            }
            tempItems = new HashSet<string>();
        }

        public void SaveTempItems()
        {
            temp = false;
            
            tempItems = new HashSet<string>();
        }

        public bool Has(string item)
        {
            item = LogicManager.RemovePrefixSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return false;
            }
            return (obtained[a.Item2] & a.Item1) == a.Item1;
        }

        public void UpdateWaypoints()
        {
            if (RandomizerMod.Instance.Settings.RandomizeRooms) return;

            foreach(string waypoint in LogicManager.Waypoints)
            {
                if (!Has(waypoint) && CanGet(waypoint))
                {
                    Add(waypoint);
                }
            }
        }

        private void ApplyDifficultySettings()
        {
            bool tempshare = share;
            share = false;

            if (RandomizerMod.Instance.Settings.ShadeSkips) Add("SHADESKIPS");
            if (RandomizerMod.Instance.Settings.AcidSkips) Add("ACIDSKIPS");
            if (RandomizerMod.Instance.Settings.SpikeTunnels) Add("SPIKETUNNELS");
            if (RandomizerMod.Instance.Settings.SpicySkips) Add("SPICYSKIPS");
            if (RandomizerMod.Instance.Settings.FireballSkips) Add("FIREBALLSKIPS");
            if (RandomizerMod.Instance.Settings.DarkRooms) Add("DARKROOMS");
            if (RandomizerMod.Instance.Settings.MildSkips) Add("MILDSKIPS");
            if (!RandomizerMod.Instance.Settings.Cursed) Add("NOTCURSED");
            if (RandomizerMod.Instance.Settings.Cursed) Add("CURSED");

            share = tempshare;
        }

        private void FetchGrubLocations(RandomizerState state, ItemManager im = null)
        {
            switch (state)
            {
                default:
                    grubLocations = LogicManager.GetItemsByPool("Grub").ToDictionary(grub => grub, grub => 1);
                    break;

                case RandomizerState.InProgress when RandomizerMod.Instance.Settings.RandomizeGrubs:
                    grubLocations = new Dictionary<string, int>();
                    break;

                case RandomizerState.Validating when RandomizerMod.Instance.Settings.RandomizeGrubs && im != null:
                    grubLocations = im.nonShopItems.Where(kvp => LogicManager.GetItemDef(kvp.Value).pool == "Grub").ToDictionary(kvp => kvp.Value, kvp => 1);
                    foreach (var kvp in im.shopItems)
                    {
                        if (kvp.Value.Any(item => LogicManager.GetItemDef(item).pool == "Grub"))
                        {
                            grubLocations.Add(kvp.Key, kvp.Value.Count(item => LogicManager.GetItemDef(item).pool == "Grub"));
                        }
                    }
                    break;

                case RandomizerState.Completed when RandomizerMod.Instance.Settings.RandomizeGrubs:
                    grubLocations = RandomizerMod.Instance.Settings.ItemPlacements
                        .Where(pair => LogicManager.GetItemDef(pair.Item1).pool == "Grub" && !LogicManager.ShopNames.Contains(pair.Item2))
                        .ToDictionary(pair => pair.Item2, kvp => 1);
                    foreach (string shop in LogicManager.ShopNames)
                    {
                        if (RandomizerMod.Instance.Settings.ItemPlacements.Any(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Grub"))
                        {
                            grubLocations.Add(shop, RandomizerMod.Instance.Settings.ItemPlacements.Count(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Grub"));
                        }
                    }
                    break;
            }
        }

        private void FetchEssenceLocations(RandomizerState state, bool concealRandomItems, ItemManager im = null)
        {
            essenceLocations = LogicManager.GetItemsByPool("Essence_Boss")
                .ToDictionary(item => item, item => LogicManager.GetItemDef(item).geo);

            switch (state)
            {
                default:
                    foreach (string root in LogicManager.GetItemsByPool("Root"))
                    {
                        essenceLocations.Add(root, LogicManager.GetItemDef(root).geo);
                    }
                    break;
                case RandomizerState.InProgress when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots:
                case RandomizerState.Completed when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && concealRandomItems:
                    break;
                case RandomizerState.Validating when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && im != null:
                    foreach (var kvp in im.nonShopItems)
                    {
                        if (LogicManager.GetItemDef(kvp.Value).pool == "Root")
                        {
                            essenceLocations.Add(kvp.Key, LogicManager.GetItemDef(kvp.Value).geo);
                        }
                    }
                    foreach (var kvp in im.shopItems)
                    {
                        foreach (string item in kvp.Value)
                        {
                            if (LogicManager.GetItemDef(item).pool == "Root")
                            {
                                if (!essenceLocations.ContainsKey(kvp.Key))
                                {
                                    essenceLocations.Add(kvp.Key, 0);
                                }
                                essenceLocations[kvp.Key] += LogicManager.GetItemDef(item).geo;
                            }
                        }
                    }
                    break;
                case RandomizerState.Completed when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && !concealRandomItems:
                    foreach (var pair in RandomizerMod.Instance.Settings.ItemPlacements)
                    {
                        if (LogicManager.GetItemDef(pair.Item1).pool == "Root" && !LogicManager.ShopNames.Contains(pair.Item2))
                        {
                            essenceLocations.Add(pair.Item2, LogicManager.GetItemDef(pair.Item1).geo);
                        }
                    }
                    foreach (string shop in LogicManager.ShopNames)
                    {
                        if (RandomizerMod.Instance.Settings.ItemPlacements.Any(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Root"))
                        {
                            essenceLocations.Add(shop, 0);
                            foreach (var pair in RandomizerMod.Instance.Settings.ItemPlacements)
                            {
                                if (pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Root")
                                {
                                    essenceLocations[shop] += LogicManager.GetItemDef(pair.Item1).geo;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void RecalculateEssence()
        {
            int essence = 0;

            foreach (string location in essenceLocations.Keys)
            {
                if (CanGet(location))
                {
                    essence += essenceLocations[location];
                }
                if (essence >= Randomizer.MAX_ESSENCE_COST + LogicManager.essenceTolerance()) break;
            }
            obtained[LogicManager.essenceIndex] = essence;
        }

        public void RecalculateGrubs()
        {
            int grubs = 0;

            foreach (string location in grubLocations.Keys)
            {
                if (CanGet(location))
                {
                    grubs += grubLocations[location];
                }
                if (grubs >= Randomizer.MAX_GRUB_COST + LogicManager.grubTolerance()) break;
            }

            obtained[LogicManager.grubIndex] = grubs;
        }

        public void AddGrubLocation(string location)
        {
            if (!grubLocations.ContainsKey(location))
            {
                grubLocations.Add(location, 1);
            }
            else
            {
                grubLocations[location]++;
            }
        }

        public void AddEssenceLocation(string location, int essence)
        {
            if (!essenceLocations.ContainsKey(location))
            {
                essenceLocations.Add(location, essence);
            }
            else
            {
                essenceLocations[location] += essence;
            }
        }

        // useful for debugging
        public string ListObtainedProgression()
        {
            string progression = string.Empty;
            foreach (string item in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(item).progression && Has(item)) progression += item + ", ";
            }

            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                foreach (string transition in LogicManager.TransitionNames())
                {
                    if (Has(transition)) progression += transition + ", ";
                }
            }
            
            return progression;
        }
        public void SpeedTest()
        {
            Stopwatch watch = new Stopwatch();
            foreach (string item in LogicManager.ItemNames)
            {
                watch.Reset();
                watch.Start();
                string result = CanGet(item).ToString();
                double elapsed = watch.Elapsed.TotalSeconds;
                Log("Parsed logic for " + item + " with result " + result + " in " + watch.Elapsed.TotalSeconds);
            }
        }
    }
}
