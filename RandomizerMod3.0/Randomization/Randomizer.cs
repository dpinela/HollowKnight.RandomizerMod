using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using RandomizerMod.Actions;
using static RandomizerMod.LogHelper;
using System.Text;
using static RandomizerMod.Randomization.PreRandomizer;
using static RandomizerMod.Randomization.PostRandomizer;
using static RandomizerMod.Randomization.SpanningTree;

namespace RandomizerMod.Randomization
{
    public enum RandomizerState
    {
        None,
        InProgress,
        Validating,
        Completed
    }

    internal class Randomizer
    {
        public const int MAX_GRUB_COST = 23;
        public const int MAX_ESSENCE_COST = 900;

        private Random rand = null;

        Dictionary<string, int> modifiedCosts;
        public string StartName;
        public StartDef startDef => LogicManager.GetStartLocation(StartName);        
        public string startTransition => RandomizerMod.Instance.Settings.RandomizeRooms ? startDef.roomTransition : startDef.areaTransition;

        private RandoSettings settings;

        public void Randomize()
        {
            settings = RandomizerMod.Instance.Settings.RandomizerSettings;
            rand = new Random(settings.Seed);
            VanillaManager.SetupVanilla();

            ItemManager im = null;
            TransitionManager tm = null;

            List<string> startItems = null;

            bool randoSuccess = false;
            while (!randoSuccess)
            {
                List<string> startProgression;
                startItems = null;
                StartName = null;

                try
                {
                    RandomizerMod.Instance.Settings.ResetPlacements();
                    modifiedCosts = RandomizeNonShopCosts(rand, settings);
                    (startItems, startProgression) = RandomizeStartingItems(rand, settings);
                    StartName = RandomizeStartingLocation(rand, settings, startProgression);
                    if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm = TransitionRandomizer.RandomizeTransitions(RandomizerMod.Instance.Settings.RandomizerSettings, rand, StartName, startItems, startProgression);
                    im = RandomizeItems(tm, startItems, startProgression);

                    randoSuccess = true;
                } catch (RandomizationError) { }
            }

            PostRandomizationTasks(im, tm, StartName, startItems, modifiedCosts);
            RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);
        }

        private ItemManager RandomizeItems(TransitionManager tm, List<string> startItems, List<string> startProgression)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Log("");
            Log("Beginning item randomization...");

            ItemManager im = new ItemManager(tm, rand, RandomizerMod.Instance.Settings.RandomizerSettings, startItems, startProgression, modifiedCosts);

            FirstPass(im, startProgression);
            SecondPass(im);
            if (!ValidateItemRandomization(im, tm, startItems, startProgression))
            {
                throw new RandomizationError();
            }

            RandomizerMod.Instance.Log("Item randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");
            return im;
        }


        private void FirstPass(ItemManager im, List<string> startProgression)
        {
            Log("Beginning first pass of item placement...");

            bool overflow = false;

            {
                im.ResetReachableLocations();
                im.vm.ResetReachableLocations(im);

                foreach (string item in startProgression) im.UpdateReachableLocations(item);
                Log("Finished first update");
            }

            while (true)
            {
                string placeItem;
                string placeLocation;

                switch (im.availableCount)
                {
                    case 0:
                        if (im.anyLocations)
                        {
                            if (im.canGuess)
                            {
                                if (!overflow) Log("Entered overflow state with 0 reachable locations after placing " + im.nonShopItems.Count + " locations");
                                overflow = true;
                                placeItem = im.GuessItem();
                                im.PlaceProgressionToStandby(placeItem);
                                continue;
                            }
                        }
                        return;
                    case 1:
                        placeItem = im.ForceItem();
                        if (placeItem is null)
                        {
                            if (im.canGuess)
                            {
                                if (!overflow) Log("Entered overflow state with 1 reachable location after placing " + im.nonShopItems.Count + " locations");
                                overflow = true;
                                placeItem = im.GuessItem();
                                im.PlaceProgressionToStandby(placeItem);
                                continue;
                            }
                            else placeItem = im.NextItem();
                        }
                        else
                        {
                            im.Delinearize(rand);
                        }
                        placeLocation = im.NextLocation();
                        break;
                    default:
                        placeItem = im.NextItem();
                        placeLocation = im.NextLocation();
                        break;
                }

                //Log($"i: {placeItem}, l: {placeLocation}, o: {overflow}, p: {LogicManager.GetItemDef(placeItem).progression}");

                if (!overflow && !LogicManager.GetItemDef(placeItem).progression)
                {
                    im.PlaceJunkItemToStandby(placeItem, placeLocation);
                }
                else
                {
                    im.PlaceItem(placeItem, placeLocation);
                }
            }
        }

        private void SecondPass(ItemManager im)
        {
            Log("Beginning second pass of item placement...");
            im.TransferStandby();

            // We fill the remaining locations and shops with the leftover junk
            while (im.anyItems)
            {
                string placeItem = im.NextItem(checkFlag: false);
                string placeLocation;

                if (im.anyLocations) placeLocation = im.NextLocation(checkLogic: false);
                else placeLocation = LogicManager.ShopNames[rand.Next(5)];

                im.PlaceItemFromStandby(placeItem, placeLocation);
            }

            // try to guarantee no empty shops
            if (im.normalFillShops && im.shopItems.Any(kvp => !kvp.Value.Any()))
            {
                Log("Exited randomizer with empty shop. Attempting repair...");
                Dictionary<string, List<string>> nonprogressionShopItems = im.shopItems.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(i => !LogicManager.GetItemDef(i).progression).ToList());
                if (nonprogressionShopItems.Select(kvp => kvp.Value.Count).Aggregate(0, (total,next) => total + next) >= 5)
                {
                    int i = 0;
                    while(im.shopItems.FirstOrDefault(kvp => !kvp.Value.Any()).Key is string emptyShop && nonprogressionShopItems.FirstOrDefault(kvp => kvp.Value.Count > 1).Key is string fullShop)
                    {
                        string item = im.shopItems[fullShop].First();
                        im.shopItems[emptyShop].Add(item);
                        im.shopItems[fullShop].Remove(item);
                        nonprogressionShopItems[emptyShop].Add(item);
                        nonprogressionShopItems[fullShop].Remove(item);
                        i++;
                        if (i > 5)
                        {
                            LogError("Emergency exit from shop repair.");
                            break;
                        }
                    }
                }
                Log("Successfully repaired shops.");
            }

            if (im.anyLocations) LogError("Exited item randomizer with unfilled locations.");
        }

        private bool ValidateItemRandomization(ItemManager im, TransitionManager tm, List<string> startItems, List<string> startProgression)
        {
            RandomizerMod.Instance.Log("Beginning item placement validation...");

            List<string> unfilledLocations;
            if (im.normalFillShops) unfilledLocations = im.randomizedLocations.Except(im.nonShopItems.Keys).Except(im.shopItems.Keys).ToList();
            else unfilledLocations = im.randomizedLocations.Except(im.nonShopItems.Keys).Except(LogicManager.ShopNames).ToList();
            
            if (unfilledLocations.Any())
            {
                Log("Unable to validate!");
                string m = "The following locations were not filled: ";
                foreach (string l in unfilledLocations) m += l + ", ";
                Log(m);
                return false;
            }

            HashSet<(string,string)> LIpairs = new HashSet<(string,string)>(im.nonShopItems.Select(kvp => (kvp.Key, kvp.Value)));
            foreach (var kvp in im.shopItems)
            {
                LIpairs.UnionWith(kvp.Value.Select(i => (kvp.Key, i)));
            }

            var lookup = LIpairs.ToLookup(pair => pair.Item2, pair => pair.Item1).Where(x => x.Count() > 1);
            if (lookup.Any())
            {
                Log("Unable to validate!");
                string m = "The following items were placed multiple times: ";
                foreach (var x in lookup) m += x.Key + ", ";
                Log(m);
                string l = "The following locations were filled by these items: ";
                foreach (var x in lookup) foreach (string k in x) l += k + ", ";
                Log(l);
                return false;
            }

            /*
            // Potentially useful debug logs
            foreach (string item in im.GetRandomizedItems())
            {
                if (im.nonShopItems.Any(kvp => kvp.Value == item))
                {
                    Log($"Placed {item} at {im.nonShopItems.First(kvp => kvp.Value == item).Key}");
                }
                else if (im.shopItems.Any(kvp => kvp.Value.Contains(item)))
                {
                    Log($"Placed {item} at {im.shopItems.First(kvp => kvp.Value.Contains(item)).Key}");
                }
                else LogError($"Unable to find where {item} was placed.");
            }
            foreach (string location in im.GetRandomizedLocations())
            {
                if (im.nonShopItems.TryGetValue(location, out string item))
                {
                    Log($"Filled {location} with {item}");
                }
                else if (im.shopItems.ContainsKey(location))
                {
                    Log($"Filled {location}");
                }
                else LogError($"{location} was not filled.");
            }
            */

            ProgressionManager pm = new ProgressionManager(
                RandomizerState.Validating,
                im,
                tm,
                modifiedCosts: modifiedCosts
                );
            pm.Add(startProgression);

            HashSet<string> locations = new HashSet<string>(im.randomizedLocations.Union(VanillaManager.progressionLocations));
            HashSet<string> transitions = new HashSet<string>();
            HashSet<string> items = im.randomizedItems;
            items.ExceptWith(startItems);

            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                transitions.UnionWith(LogicManager.TransitionNames());
                tm.ResetReachableTransitions();
                tm.UpdateReachableTransitions(pm, startTransition);
            }

            im.vm.ResetReachableLocations(im, false, pm);

            int passes = 0;
            while (locations.Any() || items.Any() || transitions.Any())
            {
                if (RandomizerMod.Instance.Settings.RandomizeTransitions) transitions.ExceptWith(tm.reachableTransitions);

                foreach (string location in locations.Where(loc => pm.CanGet(loc)).ToList())
                {
                    locations.Remove(location);

                    if (VanillaManager.progressionLocations.Contains(location))
                    {
                        im.vm.UpdateVanillaLocations(im, location, false, pm);
                        if (RandomizerMod.Instance.Settings.RandomizeTransitions && !LogicManager.ShopNames.Contains(location)) tm.UpdateReachableTransitions(pm, location, true);
                        else if (RandomizerMod.Instance.Settings.RandomizeTransitions)
                        {
                            foreach (string i in VanillaManager.progressionShopItems[location])
                            {
                                tm.UpdateReachableTransitions(pm, i, true);
                            }
                        }
                    }

                    else if (im.nonShopItems.TryGetValue(location, out string item))
                    {
                        items.Remove(item);
                        
                        if (LogicManager.GetItemDef(item).progression)
                        {
                            pm.Add(item);
                            if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm.UpdateReachableTransitions(pm, item, true);
                        }
                    }

                    else if (im.shopItems.TryGetValue(location, out List<string> shopItems))
                    {
                        foreach (string newItem in shopItems)
                        {
                            items.Remove(newItem);
                            if (LogicManager.GetItemDef(newItem).progression)
                            {
                                pm.Add(newItem);
                                if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm.UpdateReachableTransitions(pm, newItem, true);
                            }
                        }
                    }
                    
                    else
                    {
                        Log("Unable to validate!");
                        Log($"Location {location} did not correspond to any known placement.");
                        return false;
                    }
                }

                passes++;
                if (passes > 400)
                {
                    Log("Unable to validate!");
                    Log("Progression: " + pm.ListObtainedProgression() + Environment.NewLine + "Grubs: " + pm.obtained[LogicManager.grubIndex] + Environment.NewLine + "Essence: " + pm.obtained[LogicManager.essenceIndex]);
                    string m = string.Empty;
                    foreach (string s in items) m += s + ", ";
                    Log("Unable to get items: " + m);
                    m = string.Empty;
                    foreach (string s in locations) m += s + ", ";
                    Log("Unable to get locations: " + m);
                    m = string.Empty;
                    foreach (string s in transitions) m += s + ",";
                    Log("Unable to get transitions: " + m);
                    LogItemPlacements(im, pm);
                    return false;
                }
            }
            //LogItemPlacements(pm);
            Log("Validation successful.");
            return true;
        }
    }
}
