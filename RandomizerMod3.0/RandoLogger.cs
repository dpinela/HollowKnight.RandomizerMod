using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Modding;
using RandomizerMod.Randomization;
using static RandomizerMod.LogHelper;

using RandomizerLib;
using RandomizerLib.MultiWorld;

namespace RandomizerMod
{
    public static class RandoLogger
    {
        public static ProgressionManager pm;
        public static HashSet<string> obtainedLocations;
        public static HashSet<string> uncheckedLocations;
        public static HashSet<string> randomizedLocations;
        public static HashSet<string> obtainedTransitions;
        public static HashSet<string> uncheckedTransitions;
        public static HashSet<string> randomizedTransitions;

        private static void MakeHelperLists()
        {
            {
                randomizedLocations = ItemManager.GetRandomizedLocations(RandomizerMod.Instance.Settings.RandomizerSettings);
                obtainedLocations = new HashSet<string>(RandomizerMod.Instance.Settings.GetLocationsFound());
                uncheckedLocations = new HashSet<string>();
                pm = new ProgressionManager(RandomizerMod.Instance.Settings.RandomizerSettings, RandomizerState.Completed, concealRandomItems: true);

                if (RandomizerMod.Instance.Settings.RandomizeRooms)
                {
                    pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).roomTransition);
                }
                else
                {
                    pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).waypoint);
                    if (RandomizerMod.Instance.Settings.RandomizeAreas)
                    {
                        pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).areaTransition);
                    }
                }


                foreach (string item in RandomizerMod.Instance.Settings.GetItemsFound())
                {
                    (int player, string shortItem) = LogicManager.ExtractPlayerID(item);
                    if (LogicManager.GetItemDef(item).progression && (!RandomizerMod.Instance.Settings.IsMW || player == RandomizerMod.Instance.Settings.MWPlayerId))
                    {
                        pm.Add(LogicManager.RemovePrefixSuffix(shortItem));
                    }
                }

                if (RandomizerMod.Instance.Settings.RandomizeTransitions)
                {
                    obtainedTransitions = new HashSet<string>();
                    uncheckedTransitions = new HashSet<string>();
                    randomizedTransitions = new HashSet<string>(LogicManager.TransitionNames(RandomizerMod.Instance.Settings.RandomizerSettings));

                    foreach (string transition in RandomizerMod.Instance.Settings.GetTransitionsFound())
                    {
                        obtainedTransitions.Add(transition);
                        pm.Add(transition);
                    }
                }
            }

            foreach (string location in randomizedLocations)
            {
                string altLocation = location; // clumsy way to be able to switch out items without spoiling their costs

                if (obtainedLocations.Contains(location)) continue;

                if (!LogicManager.ShopNames.Contains(location))
                {
                    if (LogicManager.GetItemDef(location).costType == CostType.Essence)
                    {
                        altLocation = "Seer";
                    }
                    else if (LogicManager.GetItemDef(location).costType == CostType.Grub)
                    {
                        altLocation = "Grubfather";
                    }
                }

                if (pm.CanGet(altLocation))
                {
                    uncheckedLocations.Add(altLocation);
                }
            }

            if (!RandomizerMod.Instance.Settings.RandomizeTransitions) return;

            foreach (string transition in randomizedTransitions)
            {
                if (obtainedTransitions.Contains(transition))
                {
                    continue;
                }
                if (pm.Has(transition))
                {
                    obtainedTransitions.Add(transition);
                }
                else if (pm.CanGet(transition))
                {
                    uncheckedTransitions.Add(transition);
                }
            }
        }

        public static void LogHelper(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerHelperLog.txt"), message + Environment.NewLine);
        }

        public static void UpdateHelperLog()
        {
            new Thread(() =>
            {
                RandoSettings settings = RandomizerMod.Instance.Settings.RandomizerSettings;
                Stopwatch helperWatch = new Stopwatch();
                helperWatch.Start();

                string log = string.Empty;
                void AddToLog(string message) => log += message + Environment.NewLine;

                MakeHelperLists();

                AddToLog($"Current scene: {GameManager.instance.sceneName}");
                if (settings.RandomizeTransitions)
                {
                    if (!string.IsNullOrEmpty(RandomizerMod.Instance.LastRandomizedEntrance) && !string.IsNullOrEmpty(RandomizerMod.Instance.LastRandomizedExit))
                    {
                        AddToLog($"Last randomized transition: {{{RandomizerMod.Instance.LastRandomizedEntrance}}}-->{{{RandomizerMod.Instance.LastRandomizedExit}}}");
                    }
                    else
                    {
                        AddToLog($"Last randomized transition: n/a");
                    }
                }

                if (!settings.RandomizeGrubs)
                {
                    // TODO: Fix this
                    //AddToLog(Environment.NewLine + "Reachable grubs: " + pm.obtained[LogicManager.grubIndex]);
                }
                if (!settings.RandomizeWhisperingRoots)
                {
                    AddToLog("Reachable essence: " + pm.obtained[LogicManager.essenceIndex]);
                }

                // UNCHECKED ITEMS
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE ITEM LOCATIONS");
                    AddToLog($"There are {uncheckedLocations.Count} unchecked reachable locations.");

                    Dictionary<string, List<string>> AreaSortedItems = new Dictionary<string, List<string>>();
                    List<string> shops = LogicManager.ShopNames.Union(new List<string> { "Seer", "Grubfather" }).ToList();

                    foreach (string location in uncheckedLocations)
                    {
                        if (shops.Contains(location))
                        {
                            if (!AreaSortedItems.ContainsKey("Shops"))
                            {
                                AreaSortedItems.Add("Shops", new List<string>());
                            }
                            AreaSortedItems["Shops"].Add(location);
                            continue;
                        }

                        if (AreaSortedItems.ContainsKey(LogicManager.GetItemDef(location).areaName)) continue;

                        AreaSortedItems.Add(
                            LogicManager.GetItemDef(location).areaName,
                            uncheckedLocations.Where(loc => !shops.Contains(loc) && LogicManager.GetItemDef(loc).areaName == LogicManager.GetItemDef(location).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedItems)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string location in area.Value)
                        {
                            AddToLog(" - " + location.Replace('_', ' '));
                        }
                    }
                }

                // UNCHECKED TRANSITIONS (AREA RANDOMIZER VERSION)
                if (settings.RandomizeAreas)
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE TRANSITIONS");

                    Dictionary<string, List<string>> AreaSortedTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in uncheckedTransitions)
                    {
                        if (AreaSortedTransitions.ContainsKey(LogicManager.GetTransitionDef(transition, settings).areaName)) continue;

                        AreaSortedTransitions.Add(
                            LogicManager.GetTransitionDef(transition, settings).areaName,
                            uncheckedTransitions.Where(t => LogicManager.GetTransitionDef(t, settings).areaName == LogicManager.GetTransitionDef(transition, settings).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedTransitions)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string transition in area.Value)
                        {
                            AddToLog(" - " + transition);
                        }
                    }
                }
                else if (settings.RandomizeRooms)
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE TRANSITIONS");

                    Dictionary<string, List<string>> SceneSortedTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in uncheckedTransitions)
                    {
                        if (SceneSortedTransitions.ContainsKey(LogicManager.GetTransitionDef(transition, settings).sceneName.Split('-').First())) continue;

                        SceneSortedTransitions.Add(
                            LogicManager.GetTransitionDef(transition, settings).sceneName.Split('-').First(),
                            uncheckedTransitions.Where
                                (t => LogicManager.GetTransitionDef(t, settings).sceneName.Split('-').First()
                                    == LogicManager.GetTransitionDef(transition, settings).sceneName.Split('-').First()).ToList()
                            );
                    }

                    foreach (var room in SceneSortedTransitions)
                    {
                        AddToLog(Environment.NewLine + room.Key.Replace('_', ' '));
                        foreach (string transition in room.Value)
                        {
                            AddToLog(" - " + transition);
                        }
                    }
                }

                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "CHECKED ITEM LOCATIONS");
                    Dictionary<string, List<string>> AreaSortedItems = new Dictionary<string, List<string>>();
                    List<string> shops = LogicManager.ShopNames.Union(new List<string> { "Seer", "Grubfather" }).ToList();

                    foreach (string location in obtainedLocations)
                    {
                        if (shops.Contains(location))
                        {
                            if (!AreaSortedItems.ContainsKey("Shops"))
                            {
                                AreaSortedItems.Add("Shops", new List<string>());
                            }
                            AreaSortedItems["Shops"].Add(location);
                            continue;
                        }

                        if (AreaSortedItems.ContainsKey(LogicManager.GetItemDef(location).areaName)) continue;

                        AreaSortedItems.Add(
                            LogicManager.GetItemDef(location).areaName,
                            obtainedLocations.Where(loc => !shops.Contains(loc) && LogicManager.GetItemDef(loc).areaName == LogicManager.GetItemDef(location).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedItems)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string location in area.Value)
                        {
                            AddToLog(" - " + location.Replace('_', ' '));
                        }
                    }
                }

                helperWatch.Stop();
                File.Create(Path.Combine(Application.persistentDataPath, "RandomizerHelperLog.txt")).Dispose();
                LogHelper("Generating helper log:");
                LogHelper(log);
                LogHelper("Generated helper log in " + helperWatch.Elapsed.TotalSeconds + " seconds.");
            }).Start();
        }

        public static void LogTracker(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerTrackerLog.txt"), message + Environment.NewLine);
        }
        public static void InitializeTracker(RandoResult result)
        {
            File.Create(Path.Combine(Application.persistentDataPath, "RandomizerTrackerLog.txt")).Dispose();
            string log = "Starting tracker log for new randomizer file.";
            void AddToLog(string s) => log += "\n" + s;
            AddToLog("SETTINGS");
            AddToLog($"Seed: {result.settings.Seed}");
            AddToLog($"Mode: " + // :)
                        $"{(result.settings.RandomizeRooms ? (result.settings.ConnectAreas ? "Connected-Area Room Randomizer" : "Room Randomizer") : (result.settings.RandomizeAreas ? "Area Randomizer" : "Item Randomizer"))}");
            if (result.players > 1)
            {
                AddToLog($"Multiworld players: {result.players}");
                AddToLog($"Multiworld player ID: {result.playerId}");
            }
            AddToLog($"Cursed: {result.settings.Cursed}");
            AddToLog($"Start location: {result.settings.StartName}");
            AddToLog($"Random start items: {result.settings.RandomizeStartItems}");
            AddToLog("REQUIRED SKIPS");
            AddToLog($"Mild skips: {result.settings.MildSkips}");
            AddToLog($"Shade skips: {result.settings.ShadeSkips}");
            AddToLog($"Fireball skips: {result.settings.FireballSkips}");
            AddToLog($"Acid skips: {result.settings.AcidSkips}");
            AddToLog($"Spike tunnels: {result.settings.SpikeTunnels}");
            AddToLog($"Dark Rooms: {result.settings.DarkRooms}");
            AddToLog($"Spicy skips: {result.settings.SpicySkips}");
            AddToLog("RANDOMIZED Pools");
            AddToLog($"Dreamers: {result.settings.RandomizeDreamers}");
            AddToLog($"Skills: {result.settings.RandomizeSkills}");
            AddToLog($"Charms: {result.settings.RandomizeCharms}");
            AddToLog($"Keys: {result.settings.RandomizeKeys}");
            AddToLog($"Geo chests: {result.settings.RandomizeGeoChests}");
            AddToLog($"Mask shards: {result.settings.RandomizeMaskShards}");
            AddToLog($"Vessel fragments: {result.settings.RandomizeVesselFragments}");
            AddToLog($"Pale ore: {result.settings.RandomizePaleOre}");
            AddToLog($"Charm notches: {result.settings.RandomizeCharmNotches}");
            AddToLog($"Rancid eggs: {result.settings.RandomizeRancidEggs}");
            AddToLog($"Relics: {result.settings.RandomizeRelics}");
            AddToLog($"Stags: {result.settings.RandomizeStags}");
            AddToLog($"Maps: {result.settings.RandomizeMaps}");
            AddToLog($"Grubs: {result.settings.RandomizeGrubs}");
            AddToLog($"Whispering roots: {result.settings.RandomizeWhisperingRoots}");
            AddToLog($"Geo rocks: {result.settings.RandomizeRocks}");
            AddToLog($"Soul totems: {result.settings.RandomizeSoulTotems}");
            AddToLog($"Lore tablets: {result.settings.RandomizeLoreTablets}");
            AddToLog($"Lifeblood cocoons: {result.settings.RandomizeLifebloodCocoons}");
            AddToLog($"Palace totems: {result.settings.RandomizePalaceTotems}");
            AddToLog($"Duplicate major items: {result.settings.DuplicateMajorItems}");
            AddToLog("QUALITY OF LIFE");
            AddToLog($"Grubfather: {result.settings.Grubfather}");
            AddToLog($"Salubra: {result.settings.CharmNotch}");
            AddToLog($"Early geo: {result.settings.EarlyGeo}");
            AddToLog($"Extra platforms: {result.settings.ExtraPlatforms}");
            AddToLog($"Levers: {result.settings.LeverSkips}");
            AddToLog($"Jiji: {result.settings.Jiji}");
            LogTracker(log);
        }
        public static void LogTransitionToTracker(string entrance, string exit)
        {
            string message = string.Empty;
            if (RandomizerMod.Instance.Settings.RandomizeAreas)
            {
                string area1 = LogicManager.GetTransitionDef(entrance, RandomizerMod.Instance.Settings.RandomizerSettings).areaName.Replace('_', ' ');
                string area2 = LogicManager.GetTransitionDef(exit, RandomizerMod.Instance.Settings.RandomizerSettings).areaName.Replace('_', ' ');
                message = $"TRANSITION --- {{{entrance}}}-->{{{exit}}}" +
                    $"\n                ({area1} to {area2})";
            }
            else if (RandomizerMod.Instance.Settings.RandomizeRooms)
            {
                message = $"TRANSITION --- {{{entrance}}}-->{{{exit}}}";
            }

            LogTracker(message);
        }
        public static void LogItemToTracker(string item, string location)
        {
            // don't spoil duplicate items!
            if (LogicManager.GetItemDef(item).majorItem && RandomizerMod.Instance.Settings.DuplicateMajorItems)
            {
                item = LogicManager.RemovePrefixSuffix(item) + $"({new System.Random().Next(10)}?)";
            }

            string message = $"ITEM --- {{{item}}} at {{{location}}}";
            LogTracker(message);
        }

        public static void LogHintToTracker(string hint, bool jiji = true, bool quirrel = false)
        {
            if (jiji) LogTracker("HINT " + RandomizerMod.Instance.Settings.JijiHintCounter + " --- " + hint);
            else if (quirrel) LogTracker("HINT (QUIRREL) --- " + hint);
            else LogTracker("HINT --- " + hint);
        }

        public static void LogSpoiler(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerSpoilerLog.txt"), message + Environment.NewLine);
        }

        public static void InitializeSpoiler(RandoResult result)
        {
            File.Create(Path.Combine(Application.persistentDataPath, "RandomizerSpoilerLog.txt")).Dispose();
            LogSpoiler("Randomization completed with seed: " + result.settings.Seed);

            if (result.players > 1)
            {
                string players = result.nicknames[0];
                for (int i = 1; i < result.nicknames.Count; i++)
                {
                    players += ", " + result.nicknames[i];
                }
                LogSpoiler("MW Players: " + players);
            }
        }

        public static void LogTransitionToSpoiler(string entrance, string exit)
        {
            string message = "Entrance " + entrance + " linked to exit " + exit;
            LogSpoiler(message);
        }

        public static void LogItemToSpoiler(string item, string location)
        {
            string message = $"Putting item \"{item.Replace('_', ' ')}\" at \"{location.Replace('_', ' ')}\"";
            LogSpoiler(message);
        }
        public static void LogAllToSpoiler(RandoResult result)
        {
            RandomizerMod.Instance.Log("Generating spoiler log...");
            new Thread(() =>
            {
                Stopwatch spoilerWatch = new Stopwatch();
                spoilerWatch.Start();

                string log = string.Empty;
                void AddToLog(string message) => log += message + Environment.NewLine;

                AddToLog(GetItemSpoiler(result));
                if (result.settings.RandomizeTransitions) AddToLog(GetTransitionSpoiler(result.settings, result.transitionPlacements.Select(kvp => (kvp.Key, kvp.Value)).ToArray()));

                try
                {
                    AddToLog(Environment.NewLine + "SETTINGS");
                    AddToLog($"Seed: {result.settings.Seed}");
                    AddToLog($"Mode: " + // :)
                        $"{(result.settings.RandomizeRooms ? (result.settings.ConnectAreas ? "Connected-Area Room Randomizer" : "Room Randomizer") : (result.settings.RandomizeAreas ? "Area Randomizer" : "Item Randomizer"))}");
                    if (result.players > 1)
                    {
                        AddToLog($"Multiworld players: {result.players}");
                        AddToLog($"Multiworld player ID: {result.playerId + 1}");
                    }
                    AddToLog($"Cursed: {result.settings.Cursed}");
                    AddToLog($"Start location: {result.settings.StartName}");
                    AddToLog($"Random start items: {result.settings.RandomizeStartItems}");
                    AddToLog("REQUIRED SKIPS");
                    AddToLog($"Mild skips: {result.settings.MildSkips}");
                    AddToLog($"Shade skips: {result.settings.ShadeSkips}");
                    AddToLog($"Fireball skips: {result.settings.FireballSkips}");
                    AddToLog($"Acid skips: {result.settings.AcidSkips}");
                    AddToLog($"Spike tunnels: {result.settings.SpikeTunnels}");
                    AddToLog($"Dark Rooms: {result.settings.DarkRooms}");
                    AddToLog($"Spicy skips: {result.settings.SpicySkips}");
                    AddToLog("RANDOMIZED LOCATIONS");
                    AddToLog($"Dreamers: {result.settings.RandomizeDreamers}");
                    AddToLog($"Skills: {result.settings.RandomizeSkills}");
                    AddToLog($"Charms: {result.settings.RandomizeCharms}");
                    AddToLog($"Keys: {result.settings.RandomizeKeys}");
                    AddToLog($"Geo chests: {result.settings.RandomizeGeoChests}");
                    AddToLog($"Mask shards: {result.settings.RandomizeMaskShards}");
                    AddToLog($"Vessel fragments: {result.settings.RandomizeVesselFragments}");
                    AddToLog($"Pale ore: {result.settings.RandomizePaleOre}");
                    AddToLog($"Charm notches: {result.settings.RandomizeCharmNotches}");
                    AddToLog($"Rancid eggs: {result.settings.RandomizeRancidEggs}");
                    AddToLog($"Relics: {result.settings.RandomizeRelics}");
                    AddToLog($"Stags: {result.settings.RandomizeStags}");
                    AddToLog($"Maps: {result.settings.RandomizeMaps}");
                    AddToLog($"Grubs: {result.settings.RandomizeGrubs}");
                    AddToLog($"Whispering roots: {result.settings.RandomizeWhisperingRoots}");
                    AddToLog($"Lifeblood cocoons: {result.settings.RandomizeLifebloodCocoons}");
                    AddToLog($"Duplicate major items: {result.settings.DuplicateMajorItems}");
                    AddToLog("QUALITY OF LIFE");
                    AddToLog($"Grubfather: {result.settings.Grubfather}");
                    AddToLog($"Salubra: {result.settings.CharmNotch}");
                    AddToLog($"Early geo: {result.settings.EarlyGeo}");
                    AddToLog($"Extra platforms: {result.settings.ExtraPlatforms}");
                    AddToLog($"Levers: {result.settings.LeverSkips}");
                    AddToLog($"Jiji: {result.settings.Jiji}");
                }
                catch
                {
                    AddToLog("Error logging randomizer settings!?!?");
                }

                spoilerWatch.Stop();
                LogSpoiler(log);
                LogSpoiler("Generated spoiler log in " + spoilerWatch.Elapsed.TotalSeconds + " seconds.");
            }).Start();
        }

        private static string GetTransitionSpoiler(RandoSettings settings, (string, string)[] transitionPlacements)
        {
            string log = string.Empty;
            void AddToLog(string message) => log += message + Environment.NewLine;

            try
            {
                if (settings.RandomizeAreas)
                {
                    Dictionary<string, List<string>> areaTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in LogicManager.TransitionNames(settings))
                    {
                        string area = LogicManager.GetTransitionDef(transition, settings).areaName;
                        if (!areaTransitions.ContainsKey(area))
                        {
                            areaTransitions[area] = new List<string>();
                        }
                    }

                    foreach ((string, string) pair in transitionPlacements)
                    {
                        string area = LogicManager.GetTransitionDef(pair.Item1, settings).areaName;
                        areaTransitions[area].Add(pair.Item1 + " --> " + pair.Item2);
                    }

                    AddToLog(Environment.NewLine + "TRANSITIONS");
                    foreach (KeyValuePair<string, List<string>> kvp in areaTransitions)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            AddToLog(Environment.NewLine + kvp.Key.Replace('_', ' ') + ":");
                            foreach (string transition in kvp.Value) AddToLog(transition);
                        }
                    }
                }

                if (settings.RandomizeRooms)
                {
                    Dictionary<string, List<string>> roomTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in LogicManager.TransitionNames(settings))
                    {
                        string room = LogicManager.GetTransitionDef(transition, settings).sceneName;
                        if (!roomTransitions.ContainsKey(room))
                        {
                            roomTransitions[room] = new List<string>();
                        }
                    }

                    foreach ((string, string) pair in transitionPlacements)
                    {
                        string room = LogicManager.GetTransitionDef(pair.Item1, settings).sceneName;
                        roomTransitions[room].Add(pair.Item1 + " --> " + pair.Item2);
                    }

                    AddToLog(Environment.NewLine + "TRANSITIONS");
                    foreach (KeyValuePair<string, List<string>> kvp in roomTransitions)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            AddToLog(Environment.NewLine + kvp.Key.Replace('_', ' ') + ":");
                            foreach (string transition in kvp.Value) AddToLog(transition);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RandomizerMod.Instance.LogError("Error while creating transition spoiler log: " + e);
            }
            return log;
        }

        private static string GetItemSpoiler(RandoResult result)
        {
            string log = string.Empty;
            void AddToLog(string message) => log += message + Environment.NewLine;

            // If this is a single player world, leave out MW tags
            string MWItemToString(MWItem item) => result.players > 1 ? item.ToString() : item.Item;

            (int, MWItem, MWItem)[] orderedILPairs = new (int, MWItem, MWItem)[result.itemPlacements.Count];

            int i = 0;
            foreach (var kvp in result.itemPlacements)
            {
                orderedILPairs[i++] = (result.locationOrder[kvp.Value], kvp.Key, kvp.Value);
            }

            try
            {
                orderedILPairs = orderedILPairs.OrderBy(triplet => triplet.Item1).ToArray();

                Dictionary<MWItem, List<string>> areaItemLocations = new Dictionary<MWItem, List<string>>();
                foreach (var triplet in orderedILPairs)
                {
                    MWItem location = triplet.Item3;
                    if (LogicManager.TryGetItemDef(location, out ReqDef locationDef))
                    {
                        MWItem area = new MWItem(location.PlayerId, locationDef.areaName);
                        if (!areaItemLocations.ContainsKey(area))
                        {
                            areaItemLocations[area] = new List<string>();
                        }
                    }
                    else if (!areaItemLocations.ContainsKey(location))
                    {
                        areaItemLocations[location] = new List<string>();
                    }
                }

                List<string> progression = new List<string>();
                foreach ((int, MWItem, MWItem) pair in orderedILPairs)
                {
                    string cost = "";
                    if (LogicManager.TryGetItemDef(pair.Item3, out ReqDef itemDef)) {
                        if (itemDef.cost != 0) cost = $" [{(result.variableCosts.ContainsKey(pair.Item3) ? result.variableCosts[pair.Item3] : itemDef.cost)} {itemDef.costType.ToString("g")}]";
                    }
                    else cost = $" [{result.shopCosts[pair.Item2]} Geo]";

                    if (LogicManager.GetItemDef(pair.Item2.Item).progression) progression.Add($"({pair.Item1}) {MWItemToString(pair.Item2)}<---at--->{MWItemToString(pair.Item3)}{cost}");
                    if (LogicManager.TryGetItemDef(pair.Item3, out ReqDef locationDef))
                    {
                        areaItemLocations[new MWItem(pair.Item3.PlayerId, locationDef.areaName)].Add($"({pair.Item1}) {MWItemToString(pair.Item2)}<---at--->{MWItemToString(pair.Item3)}{cost}");
                    }
                    else areaItemLocations[pair.Item3].Add($"{MWItemToString(pair.Item2)}{cost}");
                }

                AddToLog(Environment.NewLine + "PROGRESSION ITEMS");
                foreach (string item in progression) AddToLog(item.Replace('_', ' '));

                AddToLog(Environment.NewLine + "ALL ITEMS");
                foreach (KeyValuePair<MWItem, List<string>> kvp in areaItemLocations)
                {
                    if (kvp.Value.Count > 0)
                    {
                        string title = kvp.Key.Item;
                        if (LogicManager.ShopNames.Contains(title)) title = $"({orderedILPairs.First(triplet => triplet.Item3 == kvp.Key).Item1}) {title}";
                        title = CleanAreaName(title);
                        if (result.players > 1) title = $"MW({kvp.Key.PlayerId + 1}) {title}";
                        AddToLog(Environment.NewLine + title + ":");
                        foreach (string item in kvp.Value) AddToLog(item.Replace('_', ' '));
                    }
                }
            }
            catch (Exception e)
            {
                RandomizerMod.Instance.LogError("Error while creating item spoiler log: " + e);
            }
            return log;
        }

        public static string CleanAreaName(string name)
        {
            string newName = name.Replace('_', ' ');
            switch (newName)
            {
                case "Kings Pass":
                    newName = "King's Pass";
                    break;
                case "Queens Station":
                    newName = "Queen's Station";
                    break;
                case "Kings Station":
                    newName = "King's Station";
                    break;
                case "Queens Gardens":
                    newName = "Queen's Gardens";
                    break;
                case "Hallownests Crown":
                    newName = "Hallownest's Crown";
                    break;
                case "Kingdoms Edge":
                    newName = "Kingdom's Edge";
                    break;
                case "Weavers Den":
                    newName = "Weaver's Den";
                    break;
                case "Beasts Den":
                    newName = "Beast's Den";
                    break;
                case "Spirits Glade":
                    newName = "Spirit's Glade";
                    break;
                case "Ismas Grove":
                    newName = "Isma's Grove";
                    break;
                case "Teachers Archives":
                    newName = "Teacher's Archives";
                    break;
            }
            return newName;
        }
    }
}
