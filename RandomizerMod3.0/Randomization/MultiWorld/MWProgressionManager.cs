using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod.Randomization.MultiWorld
{
    public class MWProgressionManager
    {
        public List<int[]> obtained;

        private MWItemManager shareIm;
        private TransitionManager shareTm;

        private List<RandoSettings> settings;

        private List<Dictionary<MWItem, int>> grubLocations;
        private List<Dictionary<MWItem, int>> essenceLocations;
        private List<Dictionary<string, int>> modifiedCosts;
        private bool temp;
        private bool share = true;
        public HashSet<MWItem> tempItems;

        private int players;

        public MWProgressionManager(int players, List<RandoSettings> settings, RandomizerState state, MWItemManager im = null, TransitionManager tm = null, List<int[]> progression = null, bool concealRandomItems = false, List<Dictionary<string, int>> modifiedCosts = null)
        {
            this.players = players;
            this.settings = settings;

            shareIm = im;
            shareTm = tm;

            obtained = new List<int[]>();
            for (int i = 0; i < players; i++)
            {
                obtained.Add(new int[LogicManager.bitMaskMax + 1]);
            }
            if (progression != null)
            {
                for (int i = 0; i < players; i++)
                {
                    progression[i].CopyTo(obtained[i], 0);
                }
            }

            this.modifiedCosts = modifiedCosts;

            FetchEssenceLocations(state, concealRandomItems, im);
            FetchGrubLocations(state, im);

            ApplyDifficultySettings();
            RecalculateEssence();
            RecalculateGrubs();
        }

        public bool CanGet(MWItem mwItem)
        {
            return LogicManager.ParseProcessedLogic(mwItem.item, obtained[mwItem.playerId], modifiedCosts[mwItem.playerId]);
        }

        public void Add(MWItem mwItem)
        {
            string item = LogicManager.RemovePrefixSuffix(mwItem.item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[mwItem.playerId][a.Item2] |= a.Item1;
            if (temp)
            {
                tempItems.Add(mwItem);
            }
            if (share)
            {
                Share(mwItem);
            }
            RecalculateGrubs();
            RecalculateEssence();
            UpdateWaypoints();
        }

        public void Add(IEnumerable<MWItem> mwItems)
        {
            foreach (MWItem mwItem in mwItems)
            {
                string item = LogicManager.RemovePrefixSuffix(mwItem.item);
                if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
                {
                    RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                    return;
                }
                obtained[mwItem.playerId][a.Item2] |= a.Item1;
                if (temp)
                {
                    tempItems.Add(mwItem);
                }
                if (share)
                {
                    Share(mwItem);
                }
            }
            RecalculateGrubs();
            RecalculateEssence();
            UpdateWaypoints();
        }

        public void AddTemp(MWItem item)
        {
            temp = true;
            if (tempItems == null)
            {
                tempItems = new HashSet<MWItem>();
            }
            Add(item);
        }

        private void Share(MWItem item)
        {
            if (shareIm != null && shareIm.recentProgression != null)
            {
                shareIm.recentProgression[item.playerId].Add(item.item);
            }

            if (shareTm != null && shareTm.recentProgression != null)
            {
                //shareTm.recentProgression.Add(item);
            }
        }

        private void ToggleShare(bool value)
        {
            share = value;
        }

        public void Remove(MWItem mwItem)
        {
            string item = LogicManager.RemovePrefixSuffix(mwItem.item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[mwItem.playerId][a.Item2] &= ~a.Item1;
            if (LogicManager.grubProgression.Contains(item)) RecalculateGrubs();
            if (LogicManager.essenceProgression.Contains(item)) RecalculateEssence();
        }

        public void RemoveTempItems()
        {
            temp = false;
            foreach (MWItem mwItem in tempItems)
            {
                Remove(mwItem);
            }
            tempItems = new HashSet<MWItem>();
        }

        public void SaveTempItems()
        {
            temp = false;

            tempItems = new HashSet<MWItem>();
        }

        public bool Has(MWItem mwItem)
        {
            string item = LogicManager.RemovePrefixSuffix(mwItem.item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return false;
            }
            return (obtained[mwItem.playerId][a.Item2] & a.Item1) == a.Item1;
        }

        public void UpdateWaypoints()
        {
            for (int i = 0; i < players; i++)
            {
                if (settings[i].RandomizeRooms) return;

                foreach (string waypoint in LogicManager.Waypoints)
                {
                    MWItem wpItem = new MWItem(i, waypoint);
                    if (!Has(wpItem) && CanGet(wpItem))
                    {
                        Add(wpItem);
                    }
                }
            }
        }

        private void ApplyDifficultySettings()
        {
            bool tempshare = share;
            share = false;

            for (int i = 0; i < players; i++)
            {
                if (settings[i].ShadeSkips)    Add(new MWItem(i, "SHADESKIPS"));
                if (settings[i].AcidSkips)     Add(new MWItem(i, "ACIDSKIPS"));
                if (settings[i].SpikeTunnels)  Add(new MWItem(i, "SPIKETUNNELS"));
                if (settings[i].SpicySkips)    Add(new MWItem(i, "SPICYSKIPS"));
                if (settings[i].FireballSkips) Add(new MWItem(i, "FIREBALLSKIPS"));
                if (settings[i].DarkRooms)     Add(new MWItem(i, "DARKROOMS"));
                if (settings[i].MildSkips)     Add(new MWItem(i, "MILDSKIPS"));
                if (!settings[i].Cursed)       Add(new MWItem(i, "NOTCURSED"));
                if (settings[i].Cursed)        Add(new MWItem(i, "CURSED"));
            }

            share = tempshare;
        }

        private void FetchGrubLocations(RandomizerState state, MWItemManager im = null)
        {
            grubLocations = new List<Dictionary<MWItem, int>>();
            for (int i = 0; i < players; i++)
            {
                grubLocations.Add(new Dictionary<MWItem, int>());
                FetchGrubLocations(i, state, im);
            }
        }

        // TODO: extracting MW locations when completed (for helper?)
        private void FetchGrubLocations(int id, RandomizerState state, MWItemManager im = null)
        {
            switch (state)
            {
                default:
                    grubLocations[id] = LogicManager.GetItemsByPool("Grub").ToDictionary(grub => new MWItem(id, grub), grub => 1);
                    break;

                case RandomizerState.InProgress when settings[id].RandomizeGrubs:
                    break;

                case RandomizerState.Validating when settings[id].RandomizeGrubs && im != null:
                    grubLocations[id] = im.nonShopItems.Where(kvp => kvp.Value.playerId == id && LogicManager.GetItemDef(kvp.Value.item).pool == "Grub").ToDictionary(kvp => kvp.Value, kvp => 1);
                    foreach (var kvp in im.shopItems)
                    {
                        if (kvp.Value.Any(item => LogicManager.GetItemDef(item.item).pool == "Grub"))
                        {
                            grubLocations[id].Add(kvp.Key, kvp.Value.Count(item => item.playerId == id && LogicManager.GetItemDef(item.item).pool == "Grub"));
                        }
                    }
                    break;
                    /*case RandomizerState.Completed when settings[id].RandomizeGrubs:
                        grubLocations = settings[id].ItemPlacements
                            .Where(pair => LogicManager.GetItemDef(pair.Item1).pool == "Grub" && !LogicManager.ShopNames.Contains(pair.Item2))
                            .ToDictionary(pair => pair.Item2, kvp => 1);
                        foreach (string shop in LogicManager.ShopNames)
                        {
                            if (RandomizerMod.Instance.Settings.ItemPlacements.Any(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Grub"))
                            {
                                grubLocations.Add(shop, RandomizerMod.Instance.Settings.ItemPlacements.Count(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == "Grub"));
                            }
                        }
                        break;*/
            }
        }
        private void FetchEssenceLocations(RandomizerState state, bool concealRandomItems, MWItemManager im = null)
        {
            essenceLocations = new List<Dictionary<MWItem, int>>();
            for (int i = 0; i < players; i++)
            {
                essenceLocations.Add(new Dictionary<MWItem, int>());
                FetchEssenceLocations(i, state, concealRandomItems, im);
            }
        }

        private void FetchEssenceLocations(int id, RandomizerState state, bool concealRandomItems, MWItemManager im = null)
        {
            essenceLocations[id] = LogicManager.GetItemsByPool("Essence_Boss")
                .ToDictionary(item => new MWItem(id, item), item => LogicManager.GetItemDef(item).geo);

            switch (state)
            {
                default:
                    foreach (string root in LogicManager.GetItemsByPool("Root"))
                    {
                        essenceLocations[id].Add(new MWItem(id, root), LogicManager.GetItemDef(root).geo);
                    }
                    break;
                case RandomizerState.InProgress when settings[id].RandomizeWhisperingRoots:
                case RandomizerState.Completed when settings[id].RandomizeWhisperingRoots && concealRandomItems:
                    break;
                case RandomizerState.Validating when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && im != null:
                    foreach (var kvp in im.nonShopItems)
                    {
                        if (kvp.Value.playerId == id && LogicManager.GetItemDef(kvp.Value.item).pool == "Root")
                        {
                            essenceLocations[id].Add(kvp.Key, LogicManager.GetItemDef(kvp.Value.item).geo);
                        }
                    }
                    foreach (var kvp in im.shopItems)
                    {
                        foreach (MWItem item in kvp.Value)
                        {
                            if (item.playerId == id && LogicManager.GetItemDef(item.item).pool == "Root")
                            {
                                if (!essenceLocations[id].ContainsKey(kvp.Key))
                                {
                                    essenceLocations[id].Add(kvp.Key, 0);
                                }
                                essenceLocations[id][kvp.Key] += LogicManager.GetItemDef(item.item).geo;
                            }
                        }
                    }
                    break;
                    /*case RandomizerState.Completed when RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && !concealRandomItems:
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

                        break;*/
            }
        }

        public void RecalculateEssence()
        {
            for (int i = 0; i < players; i++)
            {
                int essence = 0;

                foreach (MWItem location in essenceLocations[i].Keys)
                {
                    if (CanGet(location))
                    {
                        essence += essenceLocations[i][location];
                    }
                    if (essence >= Randomizer.MAX_ESSENCE_COST + LogicManager.essenceTolerance(settings[i])) break;
                }
                obtained[i][LogicManager.essenceIndex] = essence;
            }
        }

        public void RecalculateGrubs()
        {
            for (int i = 0; i < players; i++)
            {
                int grubs = 0;

                foreach (MWItem location in grubLocations[i].Keys)
                {
                    if (CanGet(location))
                    {
                        grubs += grubLocations[i][location];
                    }
                    if (grubs >= Randomizer.MAX_GRUB_COST + LogicManager.grubTolerance(settings[i])) break;
                }

                obtained[i][LogicManager.grubIndex] = grubs;
            }
        }

        public void AddGrubLocation(int grubPlayer, MWItem location)
        {
            if (!grubLocations[grubPlayer].ContainsKey(location))
            {
                grubLocations[grubPlayer].Add(location, 1);
            }
            else
            {
                grubLocations[grubPlayer][location]++;
            }
        }

        public void AddEssenceLocation(int essencePlayer, MWItem location, int essence)
        {
            if (!essenceLocations[essencePlayer].ContainsKey(location))
            {
                essenceLocations[essencePlayer].Add(location, essence);
            }
            else
            {
                essenceLocations[essencePlayer][location] += essence;
            }
        }
    }
}
