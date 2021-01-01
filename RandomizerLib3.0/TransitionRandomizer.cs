using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static RandomizerLib.Logging.LogHelper;
using static RandomizerLib.SpanningTree;

namespace RandomizerLib
{
    internal static class TransitionRandomizer
    {
        public static TransitionManager RandomizeTransitions(RandoSettings settings, Random rand, string startName, List<string> startItems, List<string> startProgression)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            StartDef startDef = LogicManager.GetStartLocation(startName);
            string startTransition = settings.RandomizeRooms ? startDef.roomTransition : startDef.areaTransition;

            VanillaManager.SetupVanilla(settings);

            TransitionManager tm;

            while (true)
            {
                Log("\n" +
                    "Beginning transition randomization...");
                tm = new TransitionManager(rand, settings);

                try
                {
                    PlaceOneWayTransitions(tm, rand, settings);

                    if (settings.RandomizeAreas) BuildAreaSpanningTree(tm, rand, settings, startTransition);
                    else if (settings.RandomizeRooms && settings.ConnectAreas) BuildCARSpanningTree(tm, rand, settings, startTransition);
                    else if (settings.RandomizeRooms && !settings.ConnectAreas) BuildRoomSpanningTree(tm, rand, settings, startTransition);
                    else
                    {
                        LogError("Ambiguous settings passed to transition randomizer.");
                        throw new NotSupportedException();
                    }

                    PlaceIsolatedTransitions(settings, tm, rand, startTransition);

                    // Create temporary item manager for ConnectStartToGraph and CompleteTransitionGraph
                    ItemManager im = new ItemManager(tm, rand, settings, startItems, startProgression);

                    im.ResetReachableLocations();
                    im.vm.ResetReachableLocations(im);
                    tm.ResetReachableTransitions();

                    ConnectStartToGraph(settings, tm, im, rand, startTransition);
                    CompleteTransitionGraph(tm, im, settings, startTransition);
                }
                catch (RandomizationError)
                {
                    LogWarn("Error encountered while randomizing transitions, attempting again...");
                    continue;
                }

                if (ValidateTransitionRandomization(tm, settings, startProgression, startTransition)) break;
            }
            watch.Stop();
            Log("Transition randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");

            return tm;
        }

        private static void PlaceOneWayTransitions(TransitionManager tm, Random rand, RandoSettings settings)
        {
            List<string> oneWayEntrances = LogicManager.TransitionNames(settings).Where(transition => LogicManager.GetTransitionDef(transition, settings).oneWay == 1).ToList();
            List<string> oneWayExits = LogicManager.TransitionNames(settings).Where(transition => LogicManager.GetTransitionDef(transition, settings).oneWay == 2).ToList();
            List<string> horizontalOneWays = oneWayEntrances.Where(t => !LogicManager.GetTransitionDef(t, settings).doorName.StartsWith("b")).ToList();

            while (horizontalOneWays.Any())
            {
                string horizontalEntrance = horizontalOneWays.First();
                string downExit = oneWayExits[rand.Next(oneWayExits.Count)];

                tm.PlaceOneWayPair(horizontalEntrance, downExit);
                oneWayEntrances.Remove(horizontalEntrance);
                horizontalOneWays.Remove(horizontalEntrance);
                oneWayExits.Remove(downExit);
            }

            DirectedTransitions directed = new DirectedTransitions(rand, settings);
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

        private static void PlaceIsolatedTransitions(RandoSettings settings, TransitionManager tm, Random rand, string startTransition)
        {
            List<string> isolatedTransitions = tm.unplacedTransitions.Where(transition => LogicManager.GetTransitionDef(transition, settings).isolated).ToList();
            List<string> nonisolatedTransitions = tm.unplacedTransitions.Where(transition => !LogicManager.GetTransitionDef(transition, settings).isolated).ToList();
            DirectedTransitions directed = new DirectedTransitions(rand, settings);
            isolatedTransitions.Remove(startTransition);
            nonisolatedTransitions.Remove(startTransition);
            directed.Add(nonisolatedTransitions);

            bool connectAreas = settings.ConnectAreas;
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

        private static void ConnectStartToGraph(RandoSettings settings, TransitionManager tm, ItemManager im, Random rand, string startTransition)
        {
            Log("Attaching start to graph...");

            {   // keeping local variables out of the way
                DirectedTransitions d = new DirectedTransitions(rand, settings);
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
                if (!settings.RandomizeSkills)
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

                DirectedTransitions directed = new DirectedTransitions(rand, settings);
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
        private static void CompleteTransitionGraph(TransitionManager tm, ItemManager im, RandoSettings settings, string startTransition)
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
            Log("All transitions placed? " + (tm.transitionPlacements.Count == LogicManager.TransitionNames(settings).Count(t => LogicManager.GetTransitionDef(t, settings).oneWay != 2)));
        }

        private static bool ValidateTransitionRandomization(TransitionManager tm, RandoSettings settings, List<string> startProgression, string startTransition)
        {
            Log("Beginning transition placement validation...");

            ProgressionManager pm = new ProgressionManager(
                settings,
                RandomizerState.Validating,
                null,
                tm
                );
            pm.Add(startProgression);
            pm.Add(LogicManager.ItemNames.Where(i => LogicManager.GetItemDef(i).progression));

            tm.ResetReachableTransitions();
            tm.UpdateReachableTransitions(pm, startTransition);

            bool validated = tm.reachableTransitions.SetEquals(LogicManager.TransitionNames(settings));

            if (!validated)
            {
                Log("Transition placements failed to validate!");
                foreach (string t in LogicManager.TransitionNames(settings).Except(tm.reachableTransitions)) Log(t);
            }
            else Log("Validation successful.");
            return validated;
        }
    }
}
