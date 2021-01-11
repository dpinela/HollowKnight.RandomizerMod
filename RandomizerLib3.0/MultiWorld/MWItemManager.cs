using System;
using System.Collections.Generic;
using System.Linq;
using static RandomizerLib.Logging.LogHelper;

namespace RandomizerLib.MultiWorld
{
    public class MWItemManager
    {
        private List<Dictionary<string, string>> transitions;
        internal MWProgressionManager pm;
        internal MWVanillaManager vm;

        private List<RandoSettings> settings;

        public Dictionary<MWItem, MWItem> nonShopItems;
        public Dictionary<MWItem, List<MWItem>> shopItems;
        public Dictionary<MWItem, int> locationOrder; // the order in which a location was first removed from the pool (filled with progression, moved to standby, first shop item, etc). The order of an item will be the order of its location.
        public List<MWItem> duplicatedItems;

        public HashSet<MWItem> recentProgression;

        private List<MWItem> unplacedLocations;
        private List<MWItem> unplacedItems;
        private List<MWItem> unplacedProgression;
        private List<MWItem> standbyLocations;
        private List<MWItem> standbyItems;
        private List<MWItem> standbyProgression;

        private Queue<bool> progressionFlag;
        internal Queue<MWItem> updateQueue;
        private bool delinearizeShops; // if there are 12 or fewer shop items, we do not add shops back into randomization after they've been filled once, until the end of second pass
        public bool normalFillShops; // if there are fewer than 5 shop items, we do not include shops in randomization at all, until the end of second pass

        private HashSet<MWItem> reachableLocations;
        public HashSet<MWItem> randomizedLocations;
        public HashSet<MWItem> randomizedItems;

        private int players;

        public int availableCount => reachableLocations.Intersect(unplacedLocations).Count();

        public bool anyLocations => unplacedLocations.Any();
        public bool anyNonShopLocations => unplacedLocations.Any(loc => !LogicManager.ShopNames.Contains(loc.Item));
        public bool anyItems => unplacedItems.Any();
        public bool canGuess => unplacedProgression.Any(i => LogicManager.GetItemDef(i.Item).itemCandidate);
        internal MWItemManager(int players,
                               List<Dictionary<string, string>> transitions,
                               Random rnd,
                               List<RandoSettings> settings,
                               List<List<string>> startItems,
                               List<List<string>> startProgression,
                               List<Dictionary<string, int>> modifiedCosts = null)
        {
            this.players = players;

            this.transitions = transitions;
            pm = new MWProgressionManager(
                players,
                settings,
                RandomizerState.InProgress,
                this,
                null,
                modifiedCosts: modifiedCosts
                );
            vm = new MWVanillaManager(players, settings);

            this.settings = settings;

            nonShopItems = new Dictionary<MWItem, MWItem>();
            shopItems = new Dictionary<MWItem, List<MWItem>>();
            locationOrder = new Dictionary<MWItem, int>();

            unplacedLocations = new List<MWItem>();
            unplacedItems = new List<MWItem>();
            unplacedProgression = new List<MWItem>();
            standbyLocations = new List<MWItem>();
            standbyItems = new List<MWItem>();
            standbyProgression = new List<MWItem>();
            recentProgression = new HashSet<MWItem>();

            progressionFlag = new Queue<bool>();
            updateQueue = new Queue<MWItem>();

            for (int i = 0; i < players; i++)
            {
                foreach (string shopName in LogicManager.ShopNames)
                {
                    shopItems.Add(new MWItem(i, shopName), new List<MWItem>());
                }
            }

            randomizedItems = GetRandomizedItems(startItems);
            randomizedLocations = GetRandomizedLocations();
            List<MWItem> items = randomizedItems.ToList();
            List<MWItem> locations = randomizedLocations.ToList();

            unplacedLocations = new List<MWItem>();
            while (locations.Any())
            {
                MWItem l = locations[rnd.Next(locations.Count)];
                unplacedLocations.Add(l);
                locations.Remove(l);
            }

            // Keep track of current item candidates per player
            int[] candidatesPerPlayer = new int[players];

            while (items.Any())
            {
                MWItem i = items[rnd.Next(items.Count)];

                if (LogicManager.GetItemDef(i.Item).itemCandidate)
                {
                    int min = candidatesPerPlayer.Min();

                    // Good item socialism: don't give more candidate items to those who have at least 2 more than the minimum
                    if (candidatesPerPlayer[i.PlayerId] >= min + 2)
                    {
                        Log($"Trying to place {i} when player {i.PlayerId + 1} already has {candidatesPerPlayer[i.PlayerId]} candidate items (min: {min})");
                        // Try to pick an item for someone who has less candidate items placed
                        List<MWItem> replacement = new List<MWItem>(items.Where(mwItem => candidatesPerPlayer[mwItem.PlayerId] < min + 2 && LogicManager.GetItemDef(mwItem.Item).itemCandidate));
                        Log($"{replacement.Count} possible replacements for {i}");
                        if (replacement.Count > 0)
                        {
                            i = replacement[rnd.Next(replacement.Count)];
                        }
                    }
                    candidatesPerPlayer[i.PlayerId]++;
                }

                // For now, only do this in 1 player since delaying one players checks just means they don't get to play
                if (players == 1 && settings[i.PlayerId].Cursed)
                {
                    if (LogicManager.GetItemDef(i.Item).majorItem) i = items[rnd.Next(items.Count)];
                }

                if (!LogicManager.GetItemDef(i.Item).progression)
                {
                    unplacedItems.Add(i);
                    progressionFlag.Enqueue(false);
                }
                else
                {
                    unplacedProgression.Add(i);
                    progressionFlag.Enqueue(true);
                }
                items.Remove(i);
            }

            reachableLocations = new HashSet<MWItem>();

            for (int i = 0; i < players; i++)
            {
                foreach (string item in startItems[i]) unplacedItems.Remove(new MWItem(i, item));
                foreach (string item in startProgression[i])
                {
                    unplacedProgression.Remove(new MWItem(i, item));
                    pm.Add(new MWItem(i, item));
                    UpdateReachableLocations(new MWItem(i, item));
                }
            }

            int shopItemCount = unplacedItems.Count + unplacedProgression.Count - (unplacedLocations.Count - 5 * players);
            normalFillShops = shopItemCount >= 5 * players;
            delinearizeShops = shopItemCount > 12 * players;
            if (!normalFillShops)
            {
                LogWarn("Entering randomization with insufficient items to fill all shops.");
                for (int i = 0; i < players; i++)
                {
                    foreach (string s in LogicManager.ShopNames) unplacedLocations.Remove(new MWItem(i, s));
                }
            }
        }

        private HashSet<MWItem> GetRandomizedItems(List<List<string>> startItems) // not suitable outside randomizer, because it can't compute duplicate items
        {
            HashSet<MWItem> items = new HashSet<MWItem>();
            duplicatedItems = new List<MWItem>();

            for (int i = 0; i < players; i++)
            {
                HashSet<string> playerItems = new HashSet<string>();

                if (settings[i].RandomizeDreamers) playerItems.UnionWith(LogicManager.GetItemsByPool("Dreamer"));
                if (settings[i].RandomizeSkills) playerItems.UnionWith(LogicManager.GetItemsByPool("Skill"));
                if (settings[i].RandomizeCharms) playerItems.UnionWith(LogicManager.GetItemsByPool("Charm"));
                if (settings[i].RandomizeKeys) playerItems.UnionWith(LogicManager.GetItemsByPool("Key"));
                if (settings[i].RandomizeMaskShards) playerItems.UnionWith(LogicManager.GetItemsByPool("Mask"));
                if (settings[i].RandomizeVesselFragments) playerItems.UnionWith(LogicManager.GetItemsByPool("Vessel"));
                if (settings[i].RandomizePaleOre) playerItems.UnionWith(LogicManager.GetItemsByPool("Ore"));
                if (settings[i].RandomizeCharmNotches) playerItems.UnionWith(LogicManager.GetItemsByPool("Notch"));
                if (settings[i].RandomizeGeoChests) playerItems.UnionWith(LogicManager.GetItemsByPool("Geo"));
                if (settings[i].RandomizeRancidEggs) playerItems.UnionWith(LogicManager.GetItemsByPool("Egg"));
                if (settings[i].RandomizeRelics) playerItems.UnionWith(LogicManager.GetItemsByPool("Relic"));
                if (settings[i].RandomizeMaps) playerItems.UnionWith(LogicManager.GetItemsByPool("Map"));
                if (settings[i].RandomizeStags) playerItems.UnionWith(LogicManager.GetItemsByPool("Stag"));
                if (settings[i].RandomizeGrubs) playerItems.UnionWith(LogicManager.GetItemsByPool("Grub"));
                if (settings[i].RandomizeWhisperingRoots) playerItems.UnionWith(LogicManager.GetItemsByPool("Root"));
                if (settings[i].RandomizeRocks) playerItems.UnionWith(LogicManager.GetItemsByPool("Rock"));
                if (settings[i].RandomizeSoulTotems) playerItems.UnionWith(LogicManager.GetItemsByPool("Soul"));
                if (settings[i].RandomizePalaceTotems) playerItems.UnionWith(LogicManager.GetItemsByPool("PalaceSoul"));
                if (settings[i].RandomizeLoreTablets) playerItems.UnionWith(LogicManager.GetItemsByPool("Lore"));
                if (settings[i].RandomizeLifebloodCocoons) playerItems.UnionWith(LogicManager.GetItemsByPool("Cocoon"));

                if (settings[i].Cursed)
                {
                    playerItems.Remove("Shade_Soul");
                    playerItems.Remove("Descending_Dark");
                    playerItems.Remove("Abyss_Shriek");

                    List<string> iterate = playerItems.ToList();
                    foreach (string item in iterate)
                    {
                        switch (LogicManager.GetItemDef(item).pool)
                        {
                            case "Mask":
                            case "Vessel":
                            case "Ore":
                            case "Notch":
                            case "Geo":
                            case "Egg":
                            case "Relic":
                            case "Rock":
                            case "Soul":
                            case "PalaceSoul":
                            case "Lore":
                                playerItems.Remove(item);
                                playerItems.Add("1_Geo_(" + i + ")");
                                i++;
                                break;
                        }
                    }

                    playerItems.UnionWith(LogicManager.GetItemsByPool("Cursed"));
                }

                if (settings[i].DuplicateMajorItems)
                {
                    List<string> playerDupeItems = new List<string>();
                    foreach (string majorItem in LogicManager.ItemNames.Where(_item => LogicManager.GetItemDef(_item).majorItem).ToList())
                    {
                        if (startItems[i].Contains(majorItem)) continue;
                        if (settings[i].Cursed && (majorItem == "Vengeful_Spirit" || majorItem == "Desolate_Dive" || majorItem == "Howling_Wraiths")) continue;
                        playerDupeItems.Add(majorItem);
                    }

                    foreach (string item in playerDupeItems)
                    {
                        duplicatedItems.Add(new MWItem(i, item));
                    }
                }

                foreach (string item in playerItems)
                {
                    items.Add(new MWItem(i, item));
                }
            }

            return items;
        }

        public HashSet<MWItem> GetRandomizedLocations()
        {
            HashSet<MWItem> locations = new HashSet<MWItem>();
            for (int i = 0; i < players; i++)
            {
                foreach (string loc in GetRandomizedLocations(settings[i]))
                {
                    locations.Add(new MWItem(i, loc));
                }
            }
            return locations;
        }

        public static HashSet<string> GetRandomizedLocations(RandoSettings settings)
        {
            HashSet<string> locations = new HashSet<string>();
            if (settings.RandomizeDreamers) locations.UnionWith(LogicManager.GetItemsByPool("Dreamer"));
            if (settings.RandomizeSkills) locations.UnionWith(LogicManager.GetItemsByPool("Skill"));
            if (settings.RandomizeCharms) locations.UnionWith(LogicManager.GetItemsByPool("Charm"));
            if (settings.RandomizeKeys) locations.UnionWith(LogicManager.GetItemsByPool("Key"));
            if (settings.RandomizeMaskShards) locations.UnionWith(LogicManager.GetItemsByPool("Mask"));
            if (settings.RandomizeVesselFragments) locations.UnionWith(LogicManager.GetItemsByPool("Vessel"));
            if (settings.RandomizePaleOre) locations.UnionWith(LogicManager.GetItemsByPool("Ore"));
            if (settings.RandomizeCharmNotches) locations.UnionWith(LogicManager.GetItemsByPool("Notch"));
            if (settings.RandomizeGeoChests) locations.UnionWith(LogicManager.GetItemsByPool("Geo"));
            if (settings.RandomizeRancidEggs) locations.UnionWith(LogicManager.GetItemsByPool("Egg"));
            if (settings.RandomizeRelics) locations.UnionWith(LogicManager.GetItemsByPool("Relic"));
            if (settings.RandomizeMaps) locations.UnionWith(LogicManager.GetItemsByPool("Map"));
            if (settings.RandomizeStags) locations.UnionWith(LogicManager.GetItemsByPool("Stag"));
            if (settings.RandomizeGrubs) locations.UnionWith(LogicManager.GetItemsByPool("Grub"));
            if (settings.RandomizeWhisperingRoots) locations.UnionWith(LogicManager.GetItemsByPool("Root"));
            if (settings.RandomizeRocks) locations.UnionWith(LogicManager.GetItemsByPool("Rock"));
            if (settings.RandomizeSoulTotems) locations.UnionWith(LogicManager.GetItemsByPool("Soul"));
            if (settings.RandomizePalaceTotems) locations.UnionWith(LogicManager.GetItemsByPool("PalaceSoul"));
            if (settings.RandomizeLoreTablets) locations.UnionWith(LogicManager.GetItemsByPool("Lore"));
            if (settings.RandomizeLifebloodCocoons) locations.UnionWith(LogicManager.GetItemsByPool("Cocoon"));
            if (settings.Cursed) locations.UnionWith(LogicManager.GetItemsByPool("Cursed"));

            locations = new HashSet<string>(locations.Where(item => LogicManager.GetItemDef(item).type != ItemType.Shop));
            locations.UnionWith(LogicManager.ShopNames);

            return locations;
        }

        public void ResetReachableLocations()
        {
            reachableLocations = new HashSet<MWItem>(randomizedLocations.Union(vm.progressionLocations).Where(val => pm.CanGet(val)));
        }

        public void UpdateReachableLocations(MWItem newThing = null)
        {
            if (newThing != null)
            {
                pm.Add(newThing);
                updateQueue.Enqueue(newThing);
            }

            HashSet<MWItem> potentialLocations;
            HashSet<MWItem> potentialTransitions = new HashSet<MWItem>();

            // Seems like this would never be called while recent progression has items from a different player
            // So, just assume that everything in their is progression for the player in "new thing"
            while (updateQueue.Any())
            {
                MWItem item = updateQueue.Dequeue();
                int id = item.PlayerId;

                potentialLocations = LogicManager.GetLocationsByProgression(recentProgression, settings);
                if (settings[id].RandomizeTransitions)
                {
                    potentialTransitions = LogicManager.GetTransitionsByProgression(recentProgression, settings);
                }
                recentProgression = new HashSet<MWItem>();

                foreach (MWItem location in potentialLocations)
                {
                    if (pm.CanGet(location))
                    {
                        reachableLocations.Add(location);
                        if (vm.progressionLocations.Contains(location)) vm.UpdateVanillaLocations(this, location, true, pm);
                    }
                }

                if (settings[id].RandomizeTransitions)
                {
                    if (transitions[id].TryGetValue(item.Item, out string transition1) && !pm.Has(new MWItem(id, transition1)))
                    {
                        pm.Add(new MWItem(id, transition1));
                        updateQueue.Enqueue(new MWItem(id, transition1));
                    }
                    foreach (MWItem transition in potentialTransitions)
                    {
                        if (!pm.Has(transition) && pm.CanGet(transition))
                        {
                            pm.Add(transition);
                            updateQueue.Enqueue(transition);
                            if (transitions[transition.PlayerId].TryGetValue(transition.Item, out string transition2) && !pm.Has(new MWItem(id, transition2)))
                            {
                                pm.Add(new MWItem(id, transition2));
                                updateQueue.Enqueue(new MWItem(id, transition2));
                            }
                        }
                    }
                }
            }
        }

        public MWItem FindNextLocation(MWProgressionManager _pm = null)
        {
            if (_pm != null) pm = _pm;
            return unplacedLocations.FirstOrDefault(location => pm.CanGet(location));
        }
        public MWItem NextLocation(bool checkLogic = true)
        {
            return unplacedLocations.First(location => !checkLogic || reachableLocations.Contains(location));
        }
        public MWItem NextItem(bool checkFlag = true)
        {
            if (checkFlag && progressionFlag.Any() && progressionFlag.Dequeue() && unplacedProgression.Any()) return unplacedProgression.First();
            if (unplacedItems.Any()) return unplacedItems.First();
            if (unplacedProgression.Any()) return unplacedProgression.First();
            if (standbyItems.Any()) return standbyItems.First();
            if (standbyProgression.Any()) return standbyProgression.First();
            throw new IndexOutOfRangeException();
        }
        public MWItem GuessItem()
        {
            return unplacedProgression.First(item => LogicManager.GetItemDef(item.Item).itemCandidate);
        }

        public MWItem ForceItem()
        {
            Queue<MWItem> progressionQueue = new Queue<MWItem>();
            List<MWItem> tempProgression = new List<MWItem>();

            void UpdateTransitions()
            {
                foreach (MWItem transition in LogicManager.GetTransitionsByProgression(pm.tempItems, settings))
                {
                    if (!pm.Has(transition) && pm.CanGet(transition))
                    {
                        tempProgression.Add(transition);
                        progressionQueue.Enqueue(transition);
                        pm.Add(transition);
                        if (transitions[transition.PlayerId].TryGetValue(transition.Item, out string transition2))
                        {
                            tempProgression.Add(new MWItem(transition.PlayerId, transition2));
                            progressionQueue.Enqueue(new MWItem(transition.PlayerId, transition2));
                            pm.Add(new MWItem(transition.PlayerId, transition2));
                        }
                    }
                }
            }
            bool CheckForNewLocations()
            {
                foreach (MWItem location in LogicManager.GetLocationsByProgression(pm.tempItems, settings))
                {
                    if (randomizedLocations.Contains(location) && !reachableLocations.Contains(location) && pm.CanGet(location))
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < unplacedProgression.Count; i++)
            {
                bool found = false;
                MWItem item = unplacedProgression[i];
                pm.AddTemp(item);
                if (CheckForNewLocations()) found = true;
                else if (settings[item.PlayerId].RandomizeTransitions)
                {
                    UpdateTransitions();
                    while (progressionQueue.Any())
                    {
                        progressionQueue.Dequeue();
                        UpdateTransitions();
                        found = found || CheckForNewLocations();
                    }
                }
                pm.RemoveTempItems();
                if (found)
                {
                    return item;
                }
            }
            return null;
        }
        public void Delinearize(Random rand)
        {
            // add back shops for rare consideration for late progression
            if (delinearizeShops && unplacedProgression.Count > 0 && rand.Next(8) == 0)
            {
                for (int i = 0; i < players; i++)
                {
                    if (settings[i].Cursed) continue;

                    unplacedLocations.Insert(rand.Next(unplacedLocations.Count), new MWItem(i, LogicManager.ShopNames[rand.Next(LogicManager.ShopNames.Length)]));
                }
            }

            // release junk item paired with location from standby for rerandomization, assuming there are enough standby locations for all standby progression items. Note location order is not reset
            if (standbyLocations.Count > standbyProgression.Count && standbyItems.Any() && rand.Next(2) == 0)
            {
                int index = rand.Next(standbyLocations.Count);
                MWItem location = standbyLocations[index];
                MWItem item = standbyItems[0];
                standbyLocations.RemoveAt(index);
                standbyItems.RemoveAt(0);
                unplacedItems.Add(item);
                unplacedLocations.Insert(rand.Next(unplacedLocations.Count), location);
            }
        }

        public void TransferStandby()
        {
            standbyItems.AddRange(unplacedItems);
            unplacedItems = new List<MWItem>();
            unplacedItems.AddRange(standbyProgression);
            unplacedItems.AddRange(unplacedProgression);
            unplacedItems.AddRange(standbyItems);

            standbyLocations.AddRange(unplacedLocations);
            unplacedLocations = standbyLocations;
        }

        public void PlaceItem(MWItem item, MWItem location)
        {
            if (shopItems.ContainsKey(location)) shopItems[location].Add(item);
            else nonShopItems.Add(location, item);
            UpdateOrder(location);
            unplacedLocations.Remove(location);

            if (LogicManager.GetItemDef(item.Item).progression)
            {
                unplacedProgression.Remove(item);
                UpdateReachableLocations(item);
            }
            else unplacedItems.Remove(item);

            if (LogicManager.GetItemsByPool("Grub").Contains(item.Item))
            {
                pm.AddGrubLocation(item.PlayerId, location);
            }
            else if (LogicManager.GetItemsByPool("Root").Contains(item.Item))
            {
                pm.AddEssenceLocation(item.PlayerId, location, LogicManager.GetItemDef(item.Item).geo);
            }
        }

        public void PlaceItemFromStandby(MWItem item, MWItem location)
        {
            if (shopItems.ContainsKey(location)) shopItems[location].Add(item);
            else nonShopItems.Add(location, item);
            UpdateOrder(location);
            unplacedLocations.Remove(location);
            unplacedItems.Remove(item);
        }

        public void PlaceProgressionToStandby(MWItem item)
        {
            unplacedProgression.Remove(item);
            standbyProgression.Add(item);
            UpdateReachableLocations(item);
        }

        public void PlaceJunkItemToStandby(MWItem item, MWItem location)
        {
            standbyItems.Add(item);
            standbyLocations.Add(location);
            UpdateOrder(location);
            unplacedItems.Remove(item);
            unplacedLocations.Remove(location);
        }

        public void UpdateOrder(MWItem location)
        {
            if (!locationOrder.ContainsKey(location)) locationOrder[location] = locationOrder.Count + 1;
        }

        // debugging stuff

/*        public void LogLocationStatus(string loc)
        {
            if (unplacedLocations.Contains(loc)) RandomizerMod.Instance.Log($"{loc} unfilled.");
            else if (nonShopItems.ContainsKey(loc)) RandomizerMod.Instance.Log($"{loc} filled with {nonShopItems[loc]}");
            else Log($"{loc} not found.");
        }

        private void LogDataConflicts()
        {
            string stuff = pm.ListObtainedProgression();
            foreach (string _item in stuff.Split(','))
            {
                string item = _item.Trim();
                if (string.IsNullOrEmpty(item)) continue;
                if (!nonShopItems.ContainsValue(item) && !standbyProgression.Contains(item))
                {
                    if (LogicManager.ShopNames.All(shop => !shopItems[shop].Contains(item)))
                    {
                        LogWarn("Found " + item + " in inventory, unable to trace origin.");
                    }
                }
            }
        }*/
    }
}
