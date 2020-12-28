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

        private bool overflow;

        public List<string> startProgression;
        public List<string> startItems;

        public string StartName;
        public StartDef startDef => LogicManager.GetStartLocation(StartName);        
        public string startTransition => RandomizerMod.Instance.Settings.RandomizeRooms ? startDef.roomTransition : startDef.areaTransition;


        public void Randomize()
        {
            rand = new Random(RandomizerMod.Instance.Settings.Seed);
            VanillaManager.SetupVanilla();

            ItemManager im = null;
            TransitionManager tm = null;

            bool randoSuccess = false;
            while (!randoSuccess)
            {
                overflow = false;
                startProgression = null;
                startItems = null;
                StartName = null;

                try
                {
                    RandomizerMod.Instance.Settings.ResetPlacements();
                    RandomizeNonShopCosts(rand);
                    (startItems, startProgression) = RandomizeStartingItems(rand);
                    StartName = RandomizeStartingLocation(rand, startProgression);
                    if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm = RandomizeTransitions(startItems, startProgression);
                    im = RandomizeItems(tm);

                    randoSuccess = true;
                } catch (RandomizationError) { }
            }

            PostRandomizationTasks(im, tm, StartName, startItems);
            RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);
        }

        private TransitionManager RandomizeTransitions(List<string> startItems, List<string> startProgression)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            TransitionManager tm = new TransitionManager(rand);

            while (true)
            {
                Log("\n" +
                    "Beginning transition randomization...");

                try
                {
                    PlaceOneWayTransitions(tm);

                    if (RandomizerMod.Instance.Settings.RandomizeAreas) BuildAreaSpanningTree(tm, rand, startTransition);
                    else if (RandomizerMod.Instance.Settings.RandomizeRooms && RandomizerMod.Instance.Settings.ConnectAreas) BuildCARSpanningTree(tm, rand, startTransition);
                    else if (RandomizerMod.Instance.Settings.RandomizeRooms && !RandomizerMod.Instance.Settings.ConnectAreas) BuildRoomSpanningTree(tm, rand, startTransition);
                    else
                    {
                        LogError("Ambiguous settings passed to transition randomizer.");
                        throw new NotSupportedException();
                    }

                    PlaceIsolatedTransitions(tm);

                    // Create temporary item manager for ConnectStartToGraph and CompleteTransitionGraph
                    VanillaManager vm = new VanillaManager();
                    ItemManager im = new ItemManager(tm, vm, rand, startItems, startProgression);

                    im.ResetReachableLocations();
                    vm.ResetReachableLocations(im);
                    tm.ResetReachableTransitions();

                    ConnectStartToGraph(tm, im);
                    CompleteTransitionGraph(tm, im);
                }
                catch (RandomizationError)
                {
                    LogWarn("Error encountered while randomizing transitions, attempting again...");
                    continue;
                }

                if (ValidateTransitionRandomization(tm, startProgression)) break;
            }
            watch.Stop();
            RandomizerMod.Instance.Log("Transition randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");

            return tm;
        }
        private ItemManager RandomizeItems(TransitionManager tm)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Log("");
            Log("Beginning item randomization...");

            VanillaManager vm = new VanillaManager();
            ItemManager im = new ItemManager(tm, vm, rand, startItems, startProgression);

            FirstPass(im, vm);
            SecondPass(im);
            if (!ValidateItemRandomization(im, vm, tm))
            {
                throw new RandomizationError();
            }

            RandomizerMod.Instance.Log("Item randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");
            return im;
        }

        private void PlaceOneWayTransitions(TransitionManager tm)
        {
            List<string> oneWayEntrances = LogicManager.TransitionNames().Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 1).ToList();
            List<string> oneWayExits = LogicManager.TransitionNames().Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 2).ToList();
            List<string> horizontalOneWays = oneWayEntrances.Where(t => !LogicManager.GetTransitionDef(t).doorName.StartsWith("b")).ToList();

            while (horizontalOneWays.Any())
            {
                string horizontalEntrance = horizontalOneWays.First();
                string downExit = oneWayExits[rand.Next(oneWayExits.Count)];

                tm.PlaceOneWayPair(horizontalEntrance, downExit);
                oneWayEntrances.Remove(horizontalEntrance);
                horizontalOneWays.Remove(horizontalEntrance);
                oneWayExits.Remove(downExit);
            }

            DirectedTransitions directed = new DirectedTransitions(rand);
            directed.Add(oneWayExits);
            while (oneWayEntrances.Any())
            {
                string entrance = oneWayEntrances[rand.Next(oneWayEntrances.Count)];
                string exit = directed.GetNextTransition(entrance);

                tm.PlaceOneWayPair(entrance, exit);
                oneWayEntrances.Remove(entrance);
                oneWayExits.Remove(exit);
                directed.Remove(exit);
            }
        }

        private void PlaceIsolatedTransitions(TransitionManager tm)
        {
            List<string> isolatedTransitions = tm.unplacedTransitions.Where(transition => LogicManager.GetTransitionDef(transition).isolated).ToList();
            List<string> nonisolatedTransitions = tm.unplacedTransitions.Where(transition => !LogicManager.GetTransitionDef(transition).isolated).ToList();
            DirectedTransitions directed = new DirectedTransitions(rand);
            isolatedTransitions.Remove(startTransition);
            nonisolatedTransitions.Remove(startTransition);
            directed.Add(nonisolatedTransitions);

            bool connectAreas = RandomizerMod.Instance.Settings.ConnectAreas;
            while (isolatedTransitions.Any())
            {
                string transition1 = isolatedTransitions[rand.Next(isolatedTransitions.Count)];
                string transition2 = directed.GetNextTransition(transition1, favorSameArea: connectAreas);
                if (transition2 is null)
                {
                    Log("Ran out of nonisolated transitions during preplacement!");
                    throw new RandomizationError();
                }
                tm.PlaceStandbyPair(transition1, transition2);
                isolatedTransitions.Remove(transition1);
                directed.Remove(transition2);
            }
        }

        private void ConnectStartToGraph(TransitionManager tm, ItemManager im)
        {
            Log("Attaching start to graph...");

            {   // keeping local variables out of the way
                DirectedTransitions d = new DirectedTransitions(rand);
                d.Add(startTransition);
                string transition2 = tm.ForceTransition(im.pm, d);
                if (transition2 is null) // this should happen extremely rarely, but it has to be handled
                {
                    Log("No way out of start?!?");
                    Log("Was the start transition already placed? " + tm.transitionPlacements.ContainsKey(startTransition));
                    throw new RandomizationError();
                }
                tm.PlaceTransitionPair(startTransition, transition2, im.pm);
            }

            while (true)
            {
                if (!RandomizerMod.Instance.Settings.RandomizeSkills)
                {
                    // it is essentially impossible to generate a transition randomizer without one of these accessible
                    if (im.pm.CanGet("Mantis_Claw") || im.pm.CanGet("Mothwing_Cloak") || im.pm.CanGet("Shade_Cloak")) 
                    {
                        return;
                    }
                        
                }
                else if (im.FindNextLocation() != null) return;

                tm.UnloadReachableStandby(im.pm);
                List<string> placeableTransitions = tm.reachableTransitions.Intersect(tm.unplacedTransitions.Union(tm.standbyTransitions.Keys)).ToList();
                if (!placeableTransitions.Any())
                {
                    Log("Could not connect start to map--ran out of placeable transitions.");
                    foreach (string t in tm.reachableTransitions) Log(t);
                    throw new RandomizationError();
                }

                DirectedTransitions directed = new DirectedTransitions(rand);
                directed.Add(placeableTransitions);

                if (tm.ForceTransition(im.pm, directed) is string transition1)
                {
                    string transition2 = directed.GetNextTransition(transition1);
                    tm.PlaceTransitionPair(transition1, transition2, im.pm);
                }
                else
                {
                    Log("Could not connect start to map--ran out of progression transitions.");
                    throw new RandomizationError();
                }
            }
        }

        private void CompleteTransitionGraph(TransitionManager tm, ItemManager im)
        {
            int failsafe = 0;
            Log("Beginning full placement of transitions...");

            while (tm.unplacedTransitions.Any())
            {
                failsafe++;
                if (failsafe > 120)
                {
                    Log("Aborted randomization on too many passes. At the time, there were:");
                    Log("Unplaced transitions: " + tm.unplacedTransitions.Count);
                    Log("Reachable transitions: " + tm.reachableTransitions.Count);
                    Log("Reachable unplaced transitions, directionally compatible: " + tm.placeableCount);
                    Log("Reachable item locations: " + im.availableCount);
                    foreach (string t in tm.unplacedTransitions) Log(t + ", in reachable: " + tm.reachableTransitions.Contains(t) + ", is reachable: " + im.pm.CanGet(t));
                    throw new RandomizationError();
                }

                if (im.canGuess && im.availableCount > 1) // give randomized progression as locations are available
                {
                    if (im.FindNextLocation() is string placeLocation)
                    {
                        string placeItem = im.GuessItem();
                        im.PlaceItem(placeItem, placeLocation);
                        tm.UpdateReachableTransitions(im.pm, placeItem, true);
                    }
                }

                int placeableCount = tm.placeableCount;
                if (placeableCount < 4) tm.UpdateReachableTransitions(im.pm, startTransition);
                if (placeableCount == 0 && im.availableCount == 0)
                {
                    Log("Ran out of locations?!?");
                    throw new RandomizationError();
                }
                else if (placeableCount > 2)
                {
                    tm.UnloadReachableStandby(im.pm);
                    string transition1 = tm.NextTransition();
                    string transition2 = tm.dt.GetNextTransition(transition1);
                    tm.PlaceTransitionPair(transition1, transition2, im.pm);
                    //Log($">2 place: {transition1}, {transition2}");
                    continue;
                }
                else if (tm.unplacedTransitions.Count == 2)
                {
                    string transition1 = tm.unplacedTransitions[0];
                    string transition2 = tm.unplacedTransitions[1];
                    tm.PlaceTransitionPair(transition1, transition2, im.pm);
                    //Log($"last place: {transition1}, {transition2}");
                    continue;
                }
                else if (placeableCount != 0)
                {
                    if (tm.ForceTransition(im.pm) is string transition1)
                    {
                        string transition2 = tm.dt.GetNextTransition(transition1);
                        tm.PlaceTransitionPair(transition1, transition2, im.pm);
                        //Log($"force place: {transition1}, {transition2}");
                        continue;
                    }
                }
                // Last ditch effort to save the seed. The list is ordered by which items are heuristically likely to unlock transitions at this point.
                if (im.FindNextLocation() is string lastLocation)
                {
                    foreach (string item in new List<string> { "Mantis_Claw", "Monarch_Wings", "Desolate_Dive", "Isma's_Tear", "Crystal_Heart", "Mothwing_Cloak", "Shade_Cloak" })
                    {
                        if (!im.pm.Has(item))
                        {
                            im.PlaceItem(item, lastLocation);
                            tm.UpdateReachableTransitions(im.pm, item, true);
                            break;
                        }
                    }
                    continue;
                }
            }
            Log("Placing last reserved transitions...");
            tm.UnloadStandby();
            Log("All transitions placed? " + (tm.transitionPlacements.Count == LogicManager.TransitionNames().Count(t => LogicManager.GetTransitionDef(t).oneWay != 2)));
        }

        private void FirstPass(ItemManager im, VanillaManager vm)
        {
            Log("Beginning first pass of item placement...");

            {
                im.ResetReachableLocations();
                vm.ResetReachableLocations(im);

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

        private bool ValidateTransitionRandomization(TransitionManager tm, List<string> startProgression)
        {
            Log("Beginning transition placement validation...");

            ProgressionManager pm = new ProgressionManager(
                RandomizerState.Validating
                );
            pm.Add(startProgression);
            pm.Add(LogicManager.ItemNames.Where(i => LogicManager.GetItemDef(i).progression));

            tm.ResetReachableTransitions();
            tm.UpdateReachableTransitions(pm, startTransition);
            
            bool validated = tm.reachableTransitions.SetEquals(LogicManager.TransitionNames());

            if (!validated)
            {
                Log("Transition placements failed to validate!");
                foreach (string t in LogicManager.TransitionNames().Except(tm.reachableTransitions)) Log(t);
            }
            else Log("Validation successful.");
            return validated;
        }

        private bool ValidateItemRandomization(ItemManager im, VanillaManager vm, TransitionManager tm)
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
                tm
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

            vm.ResetReachableLocations(im, false, pm);

            int passes = 0;
            while (locations.Any() || items.Any() || transitions.Any())
            {
                if (RandomizerMod.Instance.Settings.RandomizeTransitions) transitions.ExceptWith(tm.reachableTransitions);

                foreach (string location in locations.Where(loc => pm.CanGet(loc)).ToList())
                {
                    locations.Remove(location);

                    if (VanillaManager.progressionLocations.Contains(location))
                    {
                        vm.UpdateVanillaLocations(im, location, false, pm);
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
