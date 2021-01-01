using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using static RandomizerMod.LogHelper;
using System.Security.Policy;

namespace RandomizerMod.Randomization
{
    public class ItemManager
    {
        private TransitionManager tm;
        internal ProgressionManager pm;
        internal VanillaManager vm;

        private RandoSettings settings;

        public Dictionary<string, string> nonShopItems;
        public Dictionary<string, List<string>> shopItems;
        public Dictionary<string, int> locationOrder; // the order in which a location was first removed from the pool (filled with progression, moved to standby, first shop item, etc). The order of an item will be the order of its location.
        public List<string> duplicatedItems;

        public HashSet<string> recentProgression;

        private List<string> unplacedLocations;
        private List<string> unplacedItems;
        private List<string> unplacedProgression;
        private List<string> standbyLocations;
        private List<string> standbyItems;
        private List<string> standbyProgression;

        private Queue<bool> progressionFlag;
        internal Queue<string> updateQueue;
        private bool delinearizeShops; // if there are 12 or fewer shop items, we do not add shops back into randomization after they've been filled once, until the end of second pass
        public bool normalFillShops; // if there are fewer than 5 shop items, we do not include shops in randomization at all, until the end of second pass

        private HashSet<string> reachableLocations;
        public HashSet<string> randomizedLocations;
        public HashSet<string> randomizedItems;
        public HashSet<string> allLocations;

        public int availableCount => reachableLocations.Intersect(unplacedLocations).Count();

        public bool anyLocations => unplacedLocations.Any();
        public bool anyItems => unplacedItems.Any();
        public bool canGuess => unplacedProgression.Any(i => LogicManager.GetItemDef(i).itemCandidate);
        internal ItemManager(TransitionManager tm,
                             Random rnd,
                             RandoSettings settings,
                             List<string> startItems,
                             List<string> startProgression,
                             Dictionary<string, int> modifiedCosts = null,
                             VanillaManager vm = null)
        {
            this.tm = tm;
            pm = new ProgressionManager(
                RandomizerState.InProgress,
                this,
                tm,
                modifiedCosts: modifiedCosts
                ); ;
            if (vm != null)
            {
                this.vm = vm;
            }
            else
            {
                this.vm = new VanillaManager();
            }

            this.settings = settings;

            nonShopItems = new Dictionary<string, string>();
            shopItems = new Dictionary<string, List<string>>();
            locationOrder = new Dictionary<string, int>();

            unplacedLocations = new List<string>();
            unplacedItems = new List<string>();
            unplacedProgression = new List<string>();
            standbyLocations = new List<string>();
            standbyItems = new List<string>();
            standbyProgression = new List<string>();
            recentProgression = new HashSet<string>();

            progressionFlag = new Queue<bool>();
            updateQueue = new Queue<string>();

            foreach (string shopName in LogicManager.ShopNames)
            {
                shopItems.Add(shopName, new List<string>());
            }

            randomizedItems = GetRandomizedItems(startItems);
            randomizedLocations = GetRandomizedLocations();
            List<string> items = randomizedItems.ToList();
            List<string> locations = randomizedLocations.ToList();
            randomizedLocations = new HashSet<string>(locations);
            allLocations = new HashSet<string>(LogicManager.ItemNames);
            allLocations.UnionWith(LogicManager.ShopNames);

            while (locations.Any())
            {
                string l = locations[rnd.Next(locations.Count)];
                unplacedLocations.Add(l);
                locations.Remove(l);
            }

            while (items.Any())
            {
                string i = items[rnd.Next(items.Count)];

                if (settings.Cursed)
                {
                    if (LogicManager.GetItemDef(i).majorItem) i = items[rnd.Next(items.Count)];
                }

                if (!LogicManager.GetItemDef(i).progression)
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

            reachableLocations = new HashSet<string>();

            foreach (string item in startItems) unplacedItems.Remove(item);
            foreach (string item in startProgression)
            {
                unplacedProgression.Remove(item);
                pm.Add(item);
                UpdateReachableLocations(item);
            }

            int shopItemCount = unplacedItems.Count + unplacedProgression.Count - unplacedLocations.Count + 5;
            normalFillShops = shopItemCount >= 5;
            delinearizeShops = shopItemCount > 12;
            if (!normalFillShops)
            {
                LogWarn("Entering randomization with insufficient items to fill all shops.");
                foreach (string s in LogicManager.ShopNames) unplacedLocations.Remove(s);
            }
        }

        private HashSet<string> GetRandomizedItems(List<string> startItems) // not suitable outside randomizer, because it can't compute duplicate items
        {
            HashSet<string> items = new HashSet<string>();

            if (settings.RandomizeDreamers) items.UnionWith(LogicManager.GetItemsByPool("Dreamer"));
            if (settings.RandomizeSkills) items.UnionWith(LogicManager.GetItemsByPool("Skill"));
            if (settings.RandomizeCharms) items.UnionWith(LogicManager.GetItemsByPool("Charm"));
            if (settings.RandomizeKeys) items.UnionWith(LogicManager.GetItemsByPool("Key"));
            if (settings.RandomizeMaskShards) items.UnionWith(LogicManager.GetItemsByPool("Mask"));
            if (settings.RandomizeVesselFragments) items.UnionWith(LogicManager.GetItemsByPool("Vessel"));
            if (settings.RandomizePaleOre) items.UnionWith(LogicManager.GetItemsByPool("Ore"));
            if (settings.RandomizeCharmNotches) items.UnionWith(LogicManager.GetItemsByPool("Notch"));
            if (settings.RandomizeGeoChests) items.UnionWith(LogicManager.GetItemsByPool("Geo"));
            if (settings.RandomizeRancidEggs) items.UnionWith(LogicManager.GetItemsByPool("Egg"));
            if (settings.RandomizeRelics) items.UnionWith(LogicManager.GetItemsByPool("Relic"));
            if (settings.RandomizeMaps) items.UnionWith(LogicManager.GetItemsByPool("Map"));
            if (settings.RandomizeStags) items.UnionWith(LogicManager.GetItemsByPool("Stag"));
            if (settings.RandomizeGrubs) items.UnionWith(LogicManager.GetItemsByPool("Grub"));
            if (settings.RandomizeWhisperingRoots) items.UnionWith(LogicManager.GetItemsByPool("Root"));
            if (settings.RandomizeRocks) items.UnionWith(LogicManager.GetItemsByPool("Rock"));
            if (settings.RandomizeSoulTotems) items.UnionWith(LogicManager.GetItemsByPool("Soul"));
            if (settings.RandomizePalaceTotems) items.UnionWith(LogicManager.GetItemsByPool("PalaceSoul"));
            if (settings.RandomizeLoreTablets) items.UnionWith(LogicManager.GetItemsByPool("Lore"));
            if (settings.RandomizeLifebloodCocoons) items.UnionWith(LogicManager.GetItemsByPool("Cocoon"));

            if (settings.Cursed)
            {
                items.Remove("Shade_Soul");
                items.Remove("Descending_Dark");
                items.Remove("Abyss_Shriek");

                int i = 0;

                List<string> iterate = items.ToList();
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
                            items.Remove(item);
                            items.Add("1_Geo_(" + i + ")");
                            i++;
                            break;
                    }
                }

                items.UnionWith(LogicManager.GetItemsByPool("Cursed"));
            }

            if (settings.DuplicateMajorItems)
            {
                duplicatedItems = new List<string>();
                foreach (string majorItem in LogicManager.ItemNames.Where(_item => LogicManager.GetItemDef(_item).majorItem).ToList())
                {
                    if (startItems.Contains(majorItem)) continue;
                    if (settings.Cursed && (majorItem == "Vengeful_Spirit" || majorItem == "Desolate_Dive" || majorItem == "Howling_Wraiths")) continue;
                    duplicatedItems.Add(majorItem);
                }
            }

            return items;
        }

        public HashSet<string> GetRandomizedLocations()
        {
            return GetRandomizedLocations(settings);
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
            reachableLocations = new HashSet<string>(
                randomizedLocations.Union(VanillaManager.progressionLocations).Where(val => pm.CanGet(val))
            );
        }

        public void UpdateReachableLocations(string newThing = null)
        {
            if (newThing != null)
            {
                pm.Add(newThing);
                updateQueue.Enqueue(newThing);
            }

            HashSet<string> potentialLocations;
            HashSet<string> potentialTransitions = new HashSet<string>();

            while (updateQueue.Any())
            {
                potentialLocations = LogicManager.GetLocationsByProgression(recentProgression);
                if (settings.RandomizeTransitions)
                {
                    potentialTransitions = LogicManager.GetTransitionsByProgression(recentProgression);
                }
                recentProgression = new HashSet<string>();

                string item = updateQueue.Dequeue();
                foreach (string location in potentialLocations)
                {
                    if (pm.CanGet(location))
                    {
                        reachableLocations.Add(location);
                        if (VanillaManager.progressionLocations.Contains(location)) vm.UpdateVanillaLocations(this, location, true, pm);
                    }
                }
                
                if (settings.RandomizeTransitions)
                {
                    if (tm.transitionPlacements.TryGetValue(item, out string transition1) && !pm.Has(transition1))
                    {
                        pm.Add(transition1);
                        updateQueue.Enqueue(transition1);
                    }
                    foreach (string transition in potentialTransitions)
                    {
                        if (!pm.Has(transition) && pm.CanGet(transition))
                        {
                            pm.Add(transition);
                            updateQueue.Enqueue(transition);
                            if (tm.transitionPlacements.TryGetValue(transition, out string transition2) && !pm.Has(transition2))
                            {
                                pm.Add(transition2);
                                updateQueue.Enqueue(transition2);
                            }
                        }
                    }
                }
            }
        }

        public string FindNextLocation(ProgressionManager _pm = null)
        {
            if (_pm != null) pm = _pm;
            return unplacedLocations.FirstOrDefault(location => pm.CanGet(location));
        }
        public string NextLocation(bool checkLogic = true)
        {
            return unplacedLocations.First(location => !checkLogic || reachableLocations.Contains(location));
        }
        public string NextItem(bool checkFlag = true)
        {
            if (checkFlag && progressionFlag.Any() && progressionFlag.Dequeue() && unplacedProgression.Any()) return unplacedProgression.First();
            if (unplacedItems.Any()) return unplacedItems.First();
            if (unplacedProgression.Any()) return unplacedProgression.First();
            if (standbyItems.Any()) return standbyItems.First();
            if (standbyProgression.Any()) return standbyProgression.First();
            throw new IndexOutOfRangeException();
        }
        public string GuessItem()
        {
            return unplacedProgression.First(item => LogicManager.GetItemDef(item).itemCandidate);
        }
        public string ForceItem()
        {
            Queue<string> progressionQueue = new Queue<string>();
            List<string> tempProgression = new List<string>();

            void UpdateTransitions()
            {
                foreach (string transition in LogicManager.GetTransitionsByProgression(pm.tempItems))
                {
                    if (!pm.Has(transition) && pm.CanGet(transition))
                    {
                        tempProgression.Add(transition);
                        progressionQueue.Enqueue(transition);
                        pm.Add(transition);
                        if (tm.transitionPlacements.TryGetValue(transition, out string transition2))
                        {
                            tempProgression.Add(transition2);
                            progressionQueue.Enqueue(transition2);
                            pm.Add(transition2);
                        }
                    }
                }
            }
            bool CheckForNewLocations()
            {
                foreach (string location in LogicManager.GetLocationsByProgression(pm.tempItems))
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
                string item = unplacedProgression[i];
                pm.AddTemp(item);
                if (CheckForNewLocations()) found = true;
                else if (settings.RandomizeTransitions)
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
            if (settings.Cursed) return;

            // add back shops for rare consideration for late progression
            if (delinearizeShops && unplacedProgression.Count > 0 && rand.Next(8) == 0)
            {
                unplacedLocations.Insert(rand.Next(unplacedLocations.Count), LogicManager.ShopNames[rand.Next(LogicManager.ShopNames.Length)]);
            }

            // release junk item paired with location from standby for rerandomization, assuming there are enough standby locations for all standby progression items. Note location order is not reset
            if (standbyLocations.Count > standbyProgression.Count && standbyItems.Any() && rand.Next(2) == 0)
            {
                int index = rand.Next(standbyLocations.Count);
                string location = standbyLocations[index];
                string item = standbyItems[0];
                standbyLocations.RemoveAt(index);
                standbyItems.RemoveAt(0);
                unplacedItems.Add(item);
                unplacedLocations.Insert(rand.Next(unplacedLocations.Count), location);
            }
        }

        public void TransferStandby()
        {
            standbyItems.AddRange(unplacedItems);
            unplacedItems = new List<string>();
            unplacedItems.AddRange(standbyProgression);
            unplacedItems.AddRange(unplacedProgression);
            unplacedItems.AddRange(standbyItems);

            standbyLocations.AddRange(unplacedLocations);
            unplacedLocations = standbyLocations;
        }

        public void PlaceItem(string item, string location)
        {
            if (shopItems.ContainsKey(location)) shopItems[location].Add(item);
            else nonShopItems.Add(location, item);
            UpdateOrder(location);
            unplacedLocations.Remove(location);

            if (LogicManager.GetItemDef(item).progression)
            {
                unplacedProgression.Remove(item);
                UpdateReachableLocations(item);
            }
            else unplacedItems.Remove(item);

            if (LogicManager.GetItemsByPool("Grub").Contains(item))
            {
                pm.AddGrubLocation(location);
            }
            else if (LogicManager.GetItemsByPool("Root").Contains(item))
            {
                pm.AddEssenceLocation(location, LogicManager.GetItemDef(item).geo);
            }
        }

        public void PlaceItemFromStandby(string item, string location)
        {
            if (shopItems.ContainsKey(location)) shopItems[location].Add(item);
            else nonShopItems.Add(location, item);
            UpdateOrder(location);
            unplacedLocations.Remove(location);
            unplacedItems.Remove(item);
        }

        public void PlaceProgressionToStandby(string item)
        {
            unplacedProgression.Remove(item);
            standbyProgression.Add(item);
            UpdateReachableLocations(item);
        }

        public void PlaceJunkItemToStandby(string item, string location)
        {
            standbyItems.Add(item);
            standbyLocations.Add(location);
            UpdateOrder(location);
            unplacedItems.Remove(item);
            unplacedLocations.Remove(location);
        }

        public void UpdateOrder(string location)
        {
            if (!locationOrder.ContainsKey(location)) locationOrder[location] = locationOrder.Count + 1;
        }

        // debugging stuff

        public void LogLocationStatus(string loc)
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
        }
    }
}
