using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RandomizerLib.Logging.LogHelper;
using static RandomizerLib.Randomizer;

namespace RandomizerLib
{
    internal static class SpanningTree
    {
        public static void BuildAreaSpanningTree(TransitionManager tm, Random rand, RandoSettings settings, string startTransition)
        {
            List<string> areas = new List<string>();
            Dictionary<string, List<string>> areaTransitions = new Dictionary<string, List<string>>();

            foreach (string transition in LogicManager.TransitionNames(settings))
            {
                if (transition == startTransition) continue;
                TransitionDef def = LogicManager.GetTransitionDef(transition, settings);
                string areaName = def.areaName;
                if (new List<string> { "Dirtmouth", "Forgotten_Crossroads", "Resting_Grounds" }.Contains(areaName)) areaName = "Kings_Station";
                if (new List<string> { "Ancient_Basin", "Kingdoms_Edge" }.Contains(areaName)) areaName = "Deepnest";

                if (!areas.Contains(areaName) && !def.deadEnd && !def.isolated)
                {
                    areas.Add(areaName);
                    areaTransitions.Add(areaName, new List<string>());
                }
            }

            foreach (string transition in LogicManager.TransitionNames(settings))
            {
                if (transition == startTransition) continue;
                TransitionDef def = LogicManager.GetTransitionDef(transition, settings);
                string areaName = def.areaName;
                if (def.oneWay == 0 && areas.Contains(areaName)) areaTransitions[areaName].Add(transition);
            }

            BuildSpanningTree(tm, rand, settings, areaTransitions);
        }

        public static void BuildRoomSpanningTree(TransitionManager tm, Random rand, RandoSettings settings, string startTransition)
        {
            List<string> rooms = new List<string>();
            Dictionary<string, List<string>> roomTransitions = new Dictionary<string, List<string>>();

            foreach (string transition in LogicManager.TransitionNames(settings))
            {
                if (transition == startTransition) continue;
                TransitionDef def = LogicManager.GetTransitionDef(transition, settings);
                string roomName = def.sceneName;
                if (new List<string> { "Crossroads_46", "Crossroads_46b" }.Contains(roomName)) roomName = "Crossroads_46";
                if (new List<string> { "Abyss_03", "Abyss_03_b", "Abyss_03_c" }.Contains(roomName)) roomName = "Abyss_03";
                if (new List<string> { "Ruins2_10", "Ruins2_10b" }.Contains(roomName)) roomName = "Ruins2_10";

                if (!rooms.Contains(roomName) && !def.deadEnd && !def.isolated)
                {
                    rooms.Add(roomName);
                    roomTransitions.Add(roomName, new List<string>());
                }
            }

            foreach (string transition in LogicManager.TransitionNames(settings))
            {
                if (transition == startTransition) continue;
                TransitionDef def = LogicManager.GetTransitionDef(transition, settings);
                string roomName = def.sceneName;
                if (def.oneWay == 0 && rooms.Contains(roomName)) roomTransitions[roomName].Add(transition);
            }

            BuildSpanningTree(tm, rand, settings, roomTransitions);
        }

        public static void BuildCARSpanningTree(TransitionManager tm, Random rand, RandoSettings settings, string startTransition)
        {
            List<string> areas = new List<string>();
            Dictionary<string, List<string>> rooms = new Dictionary<string, List<string>>();
            foreach (string t in tm.unplacedTransitions)
            {
                if (t == startTransition) continue;
                if (!LogicManager.GetTransitionDef(t, settings).isolated || !LogicManager.GetTransitionDef(t, settings).deadEnd)
                {
                    if (!areas.Contains(LogicManager.GetTransitionDef(t, settings).areaName))
                    {
                        areas.Add(LogicManager.GetTransitionDef(t, settings).areaName);
                        rooms.Add(LogicManager.GetTransitionDef(t, settings).areaName, new List<string>());
                    }


                    if (!rooms[LogicManager.GetTransitionDef(t, settings).areaName].Contains(LogicManager.GetTransitionDef(t, settings).sceneName))
                        rooms[LogicManager.GetTransitionDef(t, settings).areaName].Add(LogicManager.GetTransitionDef(t, settings).sceneName);
                }
            }

            var areaTransitions = new Dictionary<string, Dictionary<string, List<string>>>(); // [area][scene][transition]
            foreach (string area in areas) areaTransitions.Add(area, new Dictionary<string, List<string>>());
            foreach (var kvp in rooms) foreach (string room in kvp.Value) areaTransitions[kvp.Key].Add(room, new List<string>());
            foreach (string t in tm.unplacedTransitions)
            {
                if (t == startTransition) continue;
                TransitionDef def = LogicManager.GetTransitionDef(t, settings);
                if (!areas.Contains(def.areaName) || !areaTransitions[def.areaName].ContainsKey(def.sceneName)) continue;
                areaTransitions[def.areaName][def.sceneName].Add(t);
            }
            foreach (string area in areas) BuildSpanningTree(tm, rand, settings, areaTransitions[area]);
            var worldTransitions = new Dictionary<string, List<string>>();
            foreach (string area in areas)
            {
                worldTransitions.Add(area, new List<string>());
            }
            foreach (string t in tm.unplacedTransitions)
            {
                if (t == startTransition) continue;
                if (areas.Contains(LogicManager.GetTransitionDef(t, settings).areaName) && rooms[LogicManager.GetTransitionDef(t, settings).areaName].Contains(LogicManager.GetTransitionDef(t, settings).sceneName))
                {
                    worldTransitions[LogicManager.GetTransitionDef(t, settings).areaName].Add(t);
                }
            }
            BuildSpanningTree(tm, rand, settings, worldTransitions);
        }

        public static void BuildSpanningTree(TransitionManager tm, Random rand, RandoSettings settings, Dictionary<string, List<string>> sortedTransitions, string first = null)
        {
            List<string> remaining = sortedTransitions.Keys.ToList();
            while (first == null)
            {
                first = remaining[rand.Next(remaining.Count)];
                if (!sortedTransitions[first].Any(t => !LogicManager.GetTransitionDef(t, settings).isolated)) first = null;
            }
            remaining.Remove(first);
            List<DirectedTransitions> directed = new List<DirectedTransitions>();
            directed.Add(new DirectedTransitions(rand, settings));
            directed[0].Add(sortedTransitions[first].Where(t => !LogicManager.GetTransitionDef(t, settings).isolated).ToList());
            int failsafe = 0;

            while (remaining.Any())
            {
                bool placed = false;
                failsafe++;
                if (failsafe > 500 || !directed[0].AnyCompatible())
                {
                    Log("Triggered failsafe on round " + failsafe + " in BuildSpanningTree, where first transition set was: " + first + " with count: " + sortedTransitions[first].Count);
                    throw new RandomizationError();
                }

                string nextRoom = remaining[rand.Next(remaining.Count)];

                foreach (DirectedTransitions dt in directed)
                {
                    List<string> nextAreaTransitions = sortedTransitions[nextRoom].Where(transition => !LogicManager.GetTransitionDef(transition, settings).deadEnd && dt.Test(transition)).ToList();
                    List<string> newTransitions = sortedTransitions[nextRoom].Where(transition => !LogicManager.GetTransitionDef(transition, settings).isolated).ToList();

                    if (!nextAreaTransitions.Any())
                    {
                        continue;
                    }

                    string transitionTarget = nextAreaTransitions[rand.Next(nextAreaTransitions.Count)];
                    string transitionSource = dt.GetNextTransition(transitionTarget);

                    tm.PlaceTransitionPair(transitionSource, transitionTarget);
                    remaining.Remove(nextRoom);

                    dt.Add(newTransitions);
                    dt.Remove(transitionTarget, transitionSource);
                    placed = true;
                    break;
                }
                if (placed) continue;
                else
                {
                    DirectedTransitions dt = new DirectedTransitions(rand, settings);
                    dt.Add(sortedTransitions[nextRoom].Where(transition => !LogicManager.GetTransitionDef(transition, settings).isolated).ToList());
                    directed.Add(dt);
                    remaining.Remove(nextRoom);
                }
            }
            //Log("Completed first pass of BuildSpanningTree with " + directed.Count + " connected component(s).");
            for (int i = 0; i < directed.Count; i++)
            {
                DirectedTransitions dt = directed[i];
                DirectedTransitions dt1 = null;
                string transition1 = null;
                string transition2 = null;

                foreach (var dt2 in directed)
                {
                    if (dt == dt2) continue;

                    if (dt.left && dt2.right)
                    {
                        transition1 = dt.leftTransitions[rand.Next(dt.leftTransitions.Count)];
                        transition2 = dt2.rightTransitions[rand.Next(dt2.rightTransitions.Count)];
                        dt1 = dt2;
                        break;
                    }
                    else if (dt.right && dt2.left)
                    {
                        transition1 = dt.rightTransitions[rand.Next(dt.rightTransitions.Count)];
                        transition2 = dt2.leftTransitions[rand.Next(dt2.leftTransitions.Count)];
                        dt1 = dt2;
                        break;
                    }
                    else if (dt.top && dt2.bot)
                    {
                        transition1 = dt.topTransitions[rand.Next(dt.topTransitions.Count)];
                        transition2 = dt2.botTransitions[rand.Next(dt2.botTransitions.Count)];
                        dt1 = dt2;
                        break;
                    }
                    else if (dt.bot && dt2.top)
                    {
                        transition1 = dt.botTransitions[rand.Next(dt.botTransitions.Count)];
                        transition2 = dt2.topTransitions[rand.Next(dt2.topTransitions.Count)];
                        dt1 = dt2;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(transition1))
                {
                    tm.PlaceTransitionPair(transition1, transition2);
                    dt1.Add(dt.AllTransitions);
                    dt1.Remove(transition1, transition2);
                    directed.Remove(dt);
                    i = -1;
                }
            }
            //Log("Exited BuildSpanningTree with " + directed.Count + " connected component(s).");
        }

    }
}
