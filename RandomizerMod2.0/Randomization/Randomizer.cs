﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using RandomizerMod.Actions;

using Random = System.Random;

namespace RandomizerMod.Randomization
{
    internal static class Randomizer
    {
        private static Dictionary<string, int> additiveCounts;

        private static Dictionary<string, List<string>> shopItems;
        public static Dictionary<string, string> nonShopItems;

        private static List<string> unobtainedLocations;
        private static List<string> unobtainedItems;
        private static long obtainedProgression;
        private static List<string> storedItems; //Nonrandomized progression items. Randomizer checks if any new storedItems are accessible on each round
        public static List<string> randomizedItems; //Non-geo, non-shop randomized items. Mainly used as a candidates list for the hint shop.
        private static List<string> geoItems;
        private static List<string> shopNames;
        private static List<string> reachableShops;
        private static List<string> junkStandby;
        private static List<string> progressionStandby;
        private static List<string> locationStandby;
        private static long settingsList;
        private static List<string> reachableLocations;

        private static int randomizerAttempts;
        private static int shopMax;

        private static List<RandomizerAction> actions;

        private static bool overflow;
        private static bool initialized;
        private static bool randomized;
        private static bool validated;
        public static bool Done { get; private set; }

        public static RandomizerAction[] Actions => actions.ToArray();

        public static void Randomize()
        {
            RandomizerMod.Instance.Log("Randomizing with seed: " + RandomizerMod.Instance.Settings.Seed);
            RandomizerMod.Instance.Log("Mode - " + (RandomizerMod.Instance.Settings.NoClaw ? "No Claw" : "Standard"));
            RandomizerMod.Instance.Log("Shade skips - " + RandomizerMod.Instance.Settings.ShadeSkips);
            RandomizerMod.Instance.Log("Acid skips - " + RandomizerMod.Instance.Settings.AcidSkips);
            RandomizerMod.Instance.Log("Spike tunnel skips - " + RandomizerMod.Instance.Settings.SpikeTunnels);
            RandomizerMod.Instance.Log("Misc skips - " + RandomizerMod.Instance.Settings.MiscSkips);
            RandomizerMod.Instance.Log("Fireball skips - " + RandomizerMod.Instance.Settings.FireballSkips);
            RandomizerMod.Instance.Log("Mag skips - " + RandomizerMod.Instance.Settings.MagSkips);

            Random rand = new Random(RandomizerMod.Instance.Settings.Seed);

            #region General item randomizer

            Stopwatch randomizerWatch = new Stopwatch();
            Stopwatch validationWatch = new Stopwatch();

            initialized = false;
            randomizerAttempts = 0;

            while (true)
            {
                if (!initialized)
                {
                    randomizerWatch.Start();
                    SetupVariables();
                    randomizerAttempts++;
                    reachableLocations = new List<string>();
                    foreach (string location in unobtainedLocations)
                    {
                        if (!reachableLocations.Contains(location) && LogicManager.ParseProcessedLogic(location, obtainedProgression)) reachableLocations.Add(location);
                    }
                    initialized = true;
                    RandomizerMod.Instance.Log("Beginning first pass...");

                }

                else if (!randomized)
                {
                    string placeItem = string.Empty;
                    string placeLocation = string.Empty;
                    List<string> progressionItems = new List<string>();
                    List<string> candidateItems = new List<string>();
                    int reachableCount = reachableLocations.Count;

                    // Check for progression items from a nonrandomized category
                    foreach (string itemName in storedItems)
                    {
                        if ((LogicManager.progressionBitMask[itemName] & obtainedProgression) != LogicManager.progressionBitMask[itemName] &&  LogicManager.ParseProcessedLogic(itemName, obtainedProgression))
                        {
                            obtainedProgression |= LogicManager.progressionBitMask[itemName];
                        }
                    }

                    // Acquire unweighted accessible locations
                    foreach (string location in unobtainedLocations)
                    {
                        if (!reachableLocations.Contains(location) && LogicManager.ParseProcessedLogic(location, obtainedProgression)) reachableLocations.Add(location);
                    }
                    reachableCount = reachableLocations.Count;

                    // First, we place all geo items, to avoid them ending up in shops
                    if (geoItems.Count > 0)
                    {
                        // Traditional early geo pickup
                        if (RandomizerMod.Instance.Settings.RandomizeCharms && unobtainedLocations.Contains("Fury_of_the_Fallen"))
                        {
                            string[] furyGeoContenders = geoItems.Where(item => LogicManager.GetItemDef(item).geo > 100).ToArray();
                            string furyGeoItem = furyGeoContenders[rand.Next(furyGeoContenders.Length)];

                            unobtainedItems.Remove(furyGeoItem);
                            unobtainedLocations.Remove("Fury_of_the_Fallen");
                            nonShopItems.Add("Fury_of_the_Fallen", furyGeoItem);
                            geoItems.Remove(furyGeoItem);
                            continue;
                        }
                        // If charms aren't randomized, then we always have vanilla or random geo at FK chest
                        else if (!RandomizerMod.Instance.Settings.RandomizeCharms && unobtainedLocations.Contains("False_Knight_Chest"))
                        {
                            string[] furyGeoContenders = geoItems.Where(item => LogicManager.GetItemDef(item).geo > 100).ToArray();
                            string furyGeoItem = furyGeoContenders[rand.Next(furyGeoContenders.Length)];

                            unobtainedItems.Remove(furyGeoItem);
                            unobtainedLocations.Remove("False_Knight_Chest");
                            reachableLocations.Remove("False_Knight_Chest");
                            nonShopItems.Add("False_Knight_Chest", furyGeoItem);
                            geoItems.Remove(furyGeoItem);
                            continue;
                        }

                        else
                        {
                            string geoItem = geoItems[rand.Next(geoItems.Count)];
                            List<string> geoCandidates = unobtainedLocations.Except(reachableLocations).ToList(); // Pick geo locations which aren't in sphere 0, since fury is there
                            geoCandidates = geoCandidates.Where(location => !LogicManager.ShopNames.Contains(location) && LogicManager.GetItemDef(location).cost == 0).ToList(); // Another precaution - no geo pickups placed in shops or at toll items
                            string geoLocation = geoCandidates[rand.Next(geoCandidates.Count)];
                            unobtainedItems.Remove(geoItem);
                            unobtainedLocations.Remove(geoLocation);
                            nonShopItems.Add(geoLocation, geoItem);
                            geoItems.Remove(geoItem);
                            continue;
                        }
                    }

                    //Then, we place items randomly while there are many reachable spaces
                    else if (reachableCount > 1 && unobtainedItems.Count > 0)
                    {
                        placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                        placeLocation = reachableLocations[rand.Next(reachableLocations.Count)];
                    }
                    // This path handles forcing progression items when few random locations are left
                    else if (reachableCount == 1)
                    {
                        progressionItems = GetProgressionItems(); // Progression items which open new locations
                        candidateItems = GetCandidateItems(); // Filtered list of progression items which have compound item logic
                        if (progressionItems.Count > 0)
                        {
                            placeItem = progressionItems[rand.Next(progressionItems.Count)];
                            placeLocation = reachableLocations[0];
                            if (LogicManager.GetItemDef(placeItem).isGoodItem) placeItem = progressionItems[rand.Next(progressionItems.Count)]; // Something like Claw/Wings gets an extra reroll to incentivize more complex randomizations
                        }
                        else if (unobtainedLocations.Count > 1 && candidateItems.Count > 0)
                        {
                            overflow = true;
                            placeItem = candidateItems[rand.Next(candidateItems.Count)];
                            progressionStandby.Add(placeItem); // Note that we don't have enough locations to place candidate items here, so they go onto a standby list until the second pass
                            unobtainedItems.Remove(placeItem);
                            obtainedProgression |= LogicManager.progressionBitMask[placeItem];
                            continue;
                        }
                        else // This is how the last reachable location is filled
                        {
                            placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                            placeLocation = reachableLocations[0];
                        }
                    }
                    else // No reachable locations, ready to proceed to next stage
                    {
                        randomized = true;
                        overflow = true;
                        continue;
                    }


                    // Until first overflow items are forced, we keep junk locations for later reshuffling
                    if (!overflow && !LogicManager.GetItemDef(placeItem).progression)
                    {
                        junkStandby.Add(placeItem);
                        locationStandby.Add(placeLocation);
                        reachableLocations.Remove(placeLocation);
                        unobtainedLocations.Remove(placeLocation);
                        unobtainedItems.Remove(placeItem);
                    }
                    else
                    {
                        reachableLocations.Remove(placeLocation);
                        unobtainedLocations.Remove(placeLocation);
                        unobtainedItems.Remove(placeItem);
                        if (LogicManager.GetItemDef(placeItem).progression)
                        {
                            obtainedProgression |= LogicManager.progressionBitMask[placeItem];
                            foreach (string location in unobtainedLocations)
                            {
                                if (!reachableLocations.Contains(location) && LogicManager.ParseProcessedLogic(location, obtainedProgression))
                                {
                                    reachableLocations.Add(location);
                                }
                            }
                        }

                        if (placeItem == "Shopkeeper's_Key" && !overflow) reachableShops.Add("Sly_(Key)"); //Reachable shops are those where we can place required items in the second pass. Important because Shopkey will not be forced as progression if shopMax < 5

                        if (shopItems.ContainsKey(placeLocation))
                        {
                            shopItems[placeLocation].Add(placeItem);
                        }
                        else
                        {
                            nonShopItems.Add(placeLocation, placeItem);
                        }
                        continue;
                    }
                }

                else if (overflow)
                {
                    foreach (string placeItem in junkStandby) unobtainedItems.Add(placeItem);
                    RandomizerMod.Instance.Log("First pass randomization complete.");
                    RandomizerMod.Instance.Log("Unused locations: " + unobtainedLocations.Count);
                    RandomizerMod.Instance.Log("Unused items: " + unobtainedItems.Count);
                    RandomizerMod.Instance.Log("Remaining required items: " + progressionStandby.Count);
                    RandomizerMod.Instance.Log("Reserved locations: " + locationStandby.Count);
                    RandomizerMod.Instance.Log("Beginning second pass...");

                    // First, we have to guarantee that items used in the logic chain are accessible
                    foreach (string placeItem in progressionStandby)
                    {
                        if (locationStandby.Count > 0)
                        {
                            string placeLocation = locationStandby[rand.Next(locationStandby.Count)];
                            locationStandby.Remove(placeLocation);
                            if (shopItems.ContainsKey(placeLocation))
                            {
                                shopItems[placeLocation].Add(placeItem);
                            }
                            else
                            {
                                nonShopItems.Add(placeLocation, placeItem);
                            }
                        }
                        else
                        {
                            string placeLocation = reachableShops[rand.Next(reachableShops.Count)];
                            shopItems[placeLocation].Add(placeItem);
                        }
                    }
                    
                    // We fill the remaining locations and shops with the leftover junk
                    while(unobtainedItems.Count > 0)
                    {
                        string placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                        unobtainedItems.Remove(placeItem);
                        if (unobtainedLocations.Count > 0)
                        {
                            string placeLocation = unobtainedLocations[rand.Next(unobtainedLocations.Count)];
                            unobtainedLocations.Remove(placeLocation);
                            if (shopItems.ContainsKey(placeLocation))
                            {
                                shopItems[placeLocation].Add(placeItem);
                            }
                            else
                            {
                                nonShopItems.Add(placeLocation, placeItem);
                            }
                        }
                        else if (locationStandby.Count > 0)
                        {
                            string placeLocation = locationStandby[rand.Next(locationStandby.Count)];
                            locationStandby.Remove(placeLocation);
                            if (shopItems.ContainsKey(placeLocation))
                            {
                                shopItems[placeLocation].Add(placeItem);
                            }
                            else
                            {
                                nonShopItems.Add(placeLocation, placeItem);
                            }
                        }
                        else
                        {
                            string placeLocation = shopNames[rand.Next(5)];
                            shopItems[placeLocation].Add(placeItem);
                        }
                    }
                    randomizerWatch.Stop();
                    RandomizerMod.Instance.Log("Seed generation completed in " + randomizerWatch.Elapsed.TotalSeconds + " seconds.");
                    randomizerWatch.Reset();
                    overflow = false;
                    
                }

                else if (!validated)
                {
                    validationWatch.Start();
                    RandomizerMod.Instance.Log("Beginning seed validation...");
                    List<string> floorItems = nonShopItems.Keys.ToList();
                    List<string> currentItemKeys = new List<string>();
                    List<string> currentItemValues = new List<string>();
                    long obtained = settingsList;
                    int passes = 0;
                    while (randomizedItems.Except(currentItemValues).Any())
                    {
                        foreach (string itemName in floorItems)
                        {
                            if (!currentItemKeys.Contains(itemName) && LogicManager.ParseProcessedLogic(itemName, obtained))
                            {
                                currentItemKeys.Add(itemName);
                                currentItemValues.Add(nonShopItems[itemName]);
                                if (LogicManager.GetItemDef(nonShopItems[itemName]).progression) obtained |= LogicManager.progressionBitMask[nonShopItems[itemName]];
                            }
                        }
                        foreach (string shopName in shopNames)
                        {
                            if (!currentItemKeys.Contains(shopName) && LogicManager.ParseProcessedLogic(shopName, obtained))
                            {
                                currentItemKeys.Add(shopName);
                                foreach (string newItem in shopItems[shopName])
                                {
                                    currentItemValues.Add(newItem);
                                    if (LogicManager.GetItemDef(newItem).progression) obtained |= LogicManager.progressionBitMask[newItem];
                                }
                            }
                        }
                        foreach (string itemName in storedItems)
                        {
                            if ((LogicManager.progressionBitMask[itemName] & obtained) != LogicManager.progressionBitMask[itemName] && LogicManager.ParseProcessedLogic(itemName, obtained))
                            {
                                obtained |= LogicManager.progressionBitMask[itemName];
                            }
                        }
                        passes++;
                        if (passes > 100) break;
                    }
                    if (passes > 100)
                    {
                        validationWatch.Stop();
                        validationWatch.Reset();
                        RandomizerMod.Instance.Log("Failed to validate! Attempting new randomization...");
                        initialized = false;
                        continue;
                    }
                    validationWatch.Stop();
                    RandomizerMod.Instance.Log("Seed validation completed in " + validationWatch.Elapsed.TotalSeconds + " seconds.");
                    validationWatch.Reset();
                    validated = true;
                }
                else break;
            }

            RandomizerMod.Instance.Log("Finished randomization with " + randomizerAttempts + " attempt(s).");
            LogAllPlacements();

            // Create a randomly ordered list of all "real" items in floor locations
            List<string> possibleHintLocations = nonShopItems.Keys.ToList();
            List<string> goodPools = new List<string> { "Skill", "Charm", "Key" };
            while(possibleHintLocations.Count > 0)
            {
                string location = possibleHintLocations[rand.Next(possibleHintLocations.Count)];
                string item = nonShopItems[location];
                if (goodPools.Contains(LogicManager.GetItemDef(item).pool))
                {
                    RandomizerMod.Instance.Settings.hintItems.Add(item);
                }
                possibleHintLocations.Remove(location);
            }
            RandomizerMod.Instance.Log("Created list of " + RandomizerMod.Instance.Settings.hintItems.Count + " possible hints.");

            #endregion


            actions = new List<RandomizerAction>();
            int newShinies = 0;

            foreach (KeyValuePair<string, string> kvp in nonShopItems)
            {
                string newItemName = kvp.Value;

                ReqDef oldItem = LogicManager.GetItemDef(kvp.Key);
                ReqDef newItem = LogicManager.GetItemDef(newItemName);

                if (oldItem.replace)
                {
                    actions.Add(new ReplaceObjectWithShiny(oldItem.sceneName, oldItem.objectName, "Randomizer Shiny"));
                    oldItem.objectName = "Randomizer Shiny";
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;
                }
                else if (oldItem.newShiny)
                {
                    string newShinyName = "New Shiny";
                    if (kvp.Key == "Void_Heart" || kvp.Key == "Lurien" || kvp.Key == "Monomon" || kvp.Key == "Herrah") { } // Give these items a name we can safely refer to in miscscenechanges
                    else
                    {
                        newShinyName = "New Shiny " + newShinies++; // Give the other items a name which safely increments for grub/essence rooms
                    }
                    actions.Add(new CreateNewShiny(oldItem.sceneName, oldItem.x, oldItem.y, newShinyName));
                    oldItem.objectName = newShinyName;
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;
                }
                else if (oldItem.type == ItemType.Geo && newItem.type != ItemType.Geo)
                {
                    actions.Add(new AddShinyToChest(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, "Randomizer Chest Shiny"));
                    oldItem.objectName = "Randomizer Chest Shiny";
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;
                }

                string randomizerBoolName = GetAdditiveBoolName(newItemName);
                bool playerdata = false;
                if (string.IsNullOrEmpty(randomizerBoolName))
                {
                    randomizerBoolName = newItem.boolName;
                    playerdata = newItem.type != ItemType.Geo;
                }

                // Dream nail needs a special case
                if (oldItem.boolName == "hasDreamNail")
                {
                    actions.Add(new ChangeBoolTest("RestingGrounds_04", "Binding Shield Activate", "FSM", "Check", randomizerBoolName, playerdata));
                    actions.Add(new ChangeBoolTest("RestingGrounds_04", "Dreamer Plaque Inspect", "Conversation Control", "End", randomizerBoolName, playerdata));
                    actions.Add(new ChangeBoolTest("RestingGrounds_04", "Dreamer Scene 2", "Control", "Init", randomizerBoolName, playerdata));
                    actions.Add(new ChangeBoolTest("RestingGrounds_04", "PreDreamnail", "FSM", "Check", randomizerBoolName, playerdata));
                    actions.Add(new ChangeBoolTest("RestingGrounds_04", "PostDreamnail", "FSM", "Check", randomizerBoolName, playerdata));
                }

                // Good luck to anyone trying to figure out this horrifying switch
                switch (oldItem.type)
                {
                    case ItemType.Charm:
                    case ItemType.Big:
                    case ItemType.Trinket:
                        switch (newItem.type)
                        {
                            case ItemType.Charm:
                            case ItemType.Shop:
                                if (newItem.trinketNum > 0)
                                {
                                    actions.Add(new ChangeShinyIntoTrinket(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.trinketNum, newItem.boolName));
                                    break;
                                }

                                actions.Add(new ChangeShinyIntoCharm(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.boolName));
                                if (!string.IsNullOrEmpty(oldItem.altObjectName))
                                {
                                    actions.Add(new ChangeShinyIntoCharm(oldItem.sceneName, oldItem.altObjectName, oldItem.fsmName, newItem.boolName));
                                }

                                break;
                            case ItemType.Big:
                            case ItemType.Spell:
                                BigItemDef[] newItemsArray = GetBigItemDefArray(newItemName);

                                actions.Add(new ChangeShinyIntoBigItem(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItemsArray, randomizerBoolName, playerdata));
                                if (!string.IsNullOrEmpty(oldItem.altObjectName))
                                {
                                    actions.Add(new ChangeShinyIntoBigItem(oldItem.sceneName, oldItem.altObjectName, oldItem.fsmName, newItemsArray, randomizerBoolName, playerdata));
                                }

                                break;
                            case ItemType.Geo:
                                if (oldItem.inChest)
                                {
                                    actions.Add(new ChangeChestGeo(oldItem.sceneName, oldItem.chestName, oldItem.chestFsmName, newItem.geo));
                                }
                                else
                                {
                                    actions.Add(new ChangeShinyIntoGeo(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.boolName, newItem.geo));

                                    if (!string.IsNullOrEmpty(oldItem.altObjectName))
                                    {
                                        actions.Add(new ChangeShinyIntoGeo(oldItem.sceneName, oldItem.altObjectName, oldItem.fsmName, newItem.boolName, newItem.geo));
                                    }
                                }

                                break;
                            case ItemType.Trinket:
                                actions.Add(new ChangeShinyIntoTrinket(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.trinketNum, newItem.boolName));
                                break;

                            default:
                                throw new Exception("Unimplemented type in randomization: " + oldItem.type);
                        }

                        break;
                    case ItemType.Geo:
                        switch (newItem.type)
                        {
                            case ItemType.Geo:
                                actions.Add(new ChangeChestGeo(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.geo));
                                break;
                            default:
                                throw new Exception("Unimplemented type in randomization: " + oldItem.type);
                        }

                        break;
                    default:
                        throw new Exception("Unimplemented type in randomization: " + oldItem.type);
                }

                if (oldItem.cost != 0)
                {
                    actions.Add(new AddYNDialogueToShiny(
                        oldItem.sceneName,
                        oldItem.objectName,
                        oldItem.fsmName,
                        newItem.nameKey,
                        oldItem.cost,
                        oldItem.costType));
                }
            }

                int shopAdditiveItems = 0;
                List<ChangeShopContents> shopActions = new List<ChangeShopContents>();

                // TODO: Change to use additiveItems rather than hard coded
                // No point rewriting this before making the shop component
                foreach (KeyValuePair<string, List<string>> kvp in shopItems)
                {
                    string shopName = kvp.Key;
                    List<string> newShopItems = kvp.Value;

                    List<ShopItemDef> newShopItemStats = new List<ShopItemDef>();

                    foreach (string item in newShopItems)
                    {
                        ReqDef newItem = LogicManager.GetItemDef(item);

                        if (newItem.type == ItemType.Spell)
                        {
                            switch (newItem.boolName)
                            {
                                case "hasVengefulSpirit":
                                case "hasShadeSoul":
                                    newItem.boolName = "RandomizerMod.ShopFireball" + shopAdditiveItems++;
                                    break;
                                case "hasDesolateDive":
                                case "hasDescendingDark":
                                    newItem.boolName = "RandomizerMod.ShopQuake" + shopAdditiveItems++;
                                    break;
                                case "hasHowlingWraiths":
                                case "hasAbyssShriek":
                                    newItem.boolName = "RandomizerMod.ShopScream" + shopAdditiveItems++;
                                    break;
                                default:
                                    throw new Exception("Unknown spell name: " + newItem.boolName);
                            }
                        }
                        else if (newItem.boolName == "hasDash" || newItem.boolName == "hasShadowDash")
                        {
                            newItem.boolName = "RandomizerMod.ShopDash" + shopAdditiveItems++;
                        }
                        else if (newItem.boolName == nameof(PlayerData.hasDreamNail) || newItem.boolName == nameof(PlayerData.hasDreamGate))
                        {
                            newItem.boolName = "RandomizerMod.ShopDreamNail" + shopAdditiveItems++;
                        }
                        else if (newItem.boolName.EndsWith("QueenFragment") || newItem.boolName.EndsWith("KingFragment") || newItem.boolName.EndsWith("VoidHeart"))
                        {
                            newItem.boolName = "RandomizerMod.ShopKingsoul" + shopAdditiveItems++;
                        }

                        newShopItemStats.Add(new ShopItemDef()
                        {
                            PlayerDataBoolName = newItem.boolName,
                            NameConvo = newItem.nameKey,
                            DescConvo = newItem.shopDescKey,
                            RequiredPlayerDataBool = LogicManager.GetShopDef(shopName).requiredPlayerDataBool,
                            RemovalPlayerDataBool = string.Empty,
                            DungDiscount = LogicManager.GetShopDef(shopName).dungDiscount,
                            NotchCostBool = newItem.notchCost,
                            Cost = 100 + (rand.Next(41) * 10),
                            SpriteName = newItem.shopSpriteKey
                        });
                    }

                    ChangeShopContents existingShopAction = shopActions.Where(action => action.SceneName == LogicManager.GetShopDef(shopName).sceneName && action.ObjectName == LogicManager.GetShopDef(shopName).objectName).FirstOrDefault();

                    if (existingShopAction == null)
                    {
                        shopActions.Add(new ChangeShopContents(LogicManager.GetShopDef(shopName).sceneName, LogicManager.GetShopDef(shopName).objectName, newShopItemStats.ToArray()));
                    }
                    else
                    {
                        existingShopAction.AddItemDefs(newShopItemStats.ToArray());
                    }
                }

                shopActions.ForEach(action => actions.Add(action));
            

            Done = true;
            RandomizerMod.Instance.Log("Randomization done");
        }

        private static void SetupVariables()
        {
            nonShopItems = new Dictionary<string, string>();

            shopItems = new Dictionary<string, List<string>>();
            foreach (string shopName in LogicManager.ShopNames)
            {
                shopItems.Add(shopName, new List<string>());
            }
            ////shopItems.Add("Lemm", new List<string>()); TODO: Custom shop component to handle lemm

            unobtainedLocations = new List<string>();
            foreach (string itemName in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(itemName).type != ItemType.Shop)
                {
                    unobtainedLocations.Add(itemName);
                }
            }

            unobtainedLocations.AddRange(shopItems.Keys);
            unobtainedItems = LogicManager.ItemNames.ToList();
            shopNames = LogicManager.ShopNames.ToList();
            storedItems = new List<string>();
            randomizedItems = new List<string>();
            junkStandby = new List<string>();
            progressionStandby = new List<string>();
            locationStandby = new List<string>();

            //set up difficulty settings
            settingsList = 0;
            if (RandomizerMod.Instance.Settings.ShadeSkips) settingsList |= (LogicManager.progressionBitMask["SHADESKIPS"]);
            if (RandomizerMod.Instance.Settings.AcidSkips) settingsList |= (LogicManager.progressionBitMask["ACIDSKIPS"]);
            if (RandomizerMod.Instance.Settings.SpikeTunnels) settingsList |= (LogicManager.progressionBitMask["SPIKETUNNELS"]);
            if (RandomizerMod.Instance.Settings.MiscSkips) settingsList |= (LogicManager.progressionBitMask["MISCSKIPS"]);
            if (RandomizerMod.Instance.Settings.FireballSkips) settingsList |= (LogicManager.progressionBitMask["FIREBALLSKIPS"]);
            if (RandomizerMod.Instance.Settings.MagSkips) settingsList |= (LogicManager.progressionBitMask["MAGSKIPS"]);
            if (RandomizerMod.Instance.Settings.NoClaw) settingsList |= (LogicManager.progressionBitMask["NOCLAW"]);
            obtainedProgression = settingsList;



            // Don't place claw in no claw mode, obviously
            if (RandomizerMod.Instance.Settings.NoClaw)
            {
                unobtainedItems.Remove("Mantis_Claw");
            }

            foreach (string _itemName in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(_itemName).isFake)
                {
                    unobtainedLocations.Remove(_itemName);
                    unobtainedItems.Remove(_itemName);
                }
            }

            RemoveNonrandomizedItems();


            randomizedItems = unobtainedLocations.Except(LogicManager.ShopNames).ToList();
            Random rand = new Random(RandomizerMod.Instance.Settings.Seed);
            int eggCount = 1;
            foreach (string location in randomizedItems)
            {
                if (LogicManager.GetItemDef(location).longItemTier > RandomizerMod.Instance.Settings.LongItemTier)
                {
                    unobtainedLocations.Remove(location);
                    nonShopItems.Add(location, "Bonus_Arcane_Egg_(" + eggCount + ")");
                    eggCount++;
                }
            }

            if (RandomizerMod.Instance.Settings.PleasureHouse) nonShopItems.Add("Pleasure_House", "Small_Reward_Geo");

            geoItems = unobtainedItems.Where(name => LogicManager.GetItemDef(name).type == ItemType.Geo).ToList();
            randomizedItems = unobtainedLocations.Where(name => !LogicManager.ShopNames.Contains(name) && LogicManager.GetItemDef(name).type != ItemType.Geo).ToList();

            shopMax = unobtainedItems.Count - unobtainedLocations.Count + 5;

            if (shopMax < 5)
            {
                foreach (string shopName in LogicManager.ShopNames) unobtainedLocations.Remove(shopName);
            }
            reachableShops = LogicManager.ShopNames.ToList();
            reachableShops.Remove("Sly_(Key)");

            randomized = false;
            overflow = false;
            validated = false;
            Done = false;
        }

        private static void RemoveNonrandomizedItems()
        {
            if (!RandomizerMod.Instance.Settings.RandomizeDreamers)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Dreamer")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                        if (item.progression) storedItems.Add(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Dreamers left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeSkills)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Skill")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                        if (item.progression) storedItems.Add(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Skills left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeCharms)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Charm")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                        if (item.progression) storedItems.Add(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Charms left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeKeys)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Key")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                        if (item.progression) storedItems.Add(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Keys left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeGeoChests)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Geo")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Geo Chests left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeMaskShards)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Mask")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Mask Shards left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeVesselFragments)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Vessel")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Vessel Fragments left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizeCharmNotches)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Notch")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Charm Notches left in vanilla locations.");
            }
            if (!RandomizerMod.Instance.Settings.RandomizePaleOre)
            {
                foreach (string _itemName in LogicManager.ItemNames)
                {
                    ReqDef item = LogicManager.GetItemDef(_itemName);
                    if (item.pool == "Ore")
                    {
                        unobtainedItems.Remove(_itemName);
                        unobtainedLocations.Remove(_itemName);
                    }
                }
                RandomizerMod.Instance.Log("Pale Ore left in vanilla locations.");
            }
        }

        private static List<string> GetProgressionItems()
        {
            List<string> progression = new List<string>();
            unobtainedLocations.Remove(reachableLocations[0]);
            foreach (string str in unobtainedItems)
            {
                if (LogicManager.GetItemDef(str).progression)
                {
                    long tempItem = LogicManager.progressionBitMask[str];
                    obtainedProgression |= tempItem;
                    foreach (string item in unobtainedLocations)
                    {
                        if (LogicManager.ParseProcessedLogic(item, obtainedProgression))
                        {
                            progression.Add(str);
                            break;
                        }
                    }
                    obtainedProgression &= ~tempItem;
                }
            }
            unobtainedLocations.Add(reachableLocations[0]);

            return progression;
        }

        private static List<string> GetCandidateItems()
        {
            List<string> progression = new List<string>();

            foreach (string str in unobtainedItems)
            {
                if (LogicManager.GetItemDef(str).progression)
                {
                    // Baldur kills and Sprintmaster/Dashmaster/Sharpshadow are never good candidates, so we don't add them
                    if (str == "Mark_of_Pride" || str == "Longnail" || str == "Spore_Shroom" || str == "Glowing_Womb" || str == "Grubberfly's_Elegy" || str == "Weaversong" || str == "Sprintmaster" || str == "Dashmaster" || str == "Sharp_Shadow") { }
                    // Remove redundant items
                    else if (str == "Shopkeeper's_Key" || str == "Void_Heart" || str == "Shade_Soul" || str == "Abyss_Shriek" || str == "Descending_Dark" || str == "Dream_Gate") { }
                    //Place remainder
                    else
                    {
                        progression.Add(str);
                    }
                }
            }

            return progression;
        }

        private static string GetAdditivePrefix(string boolName)
        {
            foreach (string itemSet in LogicManager.AdditiveItemNames)
            {
                if (LogicManager.GetAdditiveItems(itemSet).Contains(boolName))
                {
                    return itemSet;
                }
            }

            return null;
        }

        private static BigItemDef[] GetBigItemDefArray(string boolName)
        {
            string prefix = GetAdditivePrefix(boolName);
            if (prefix != null)
            {
                List<BigItemDef> itemDefs = new List<BigItemDef>();
                foreach (string str in LogicManager.GetAdditiveItems(prefix))
                {
                    ReqDef item = LogicManager.GetItemDef(str);
                    itemDefs.Add(new BigItemDef()
                    {
                        BoolName = item.boolName,
                        SpriteKey = item.bigSpriteKey,
                        TakeKey = item.takeKey,
                        NameKey = item.nameKey,
                        ButtonKey = item.buttonKey,
                        DescOneKey = item.descOneKey,
                        DescTwoKey = item.descTwoKey
                    });
                }

                return itemDefs.ToArray();
            }
            else
            {
                ReqDef item = LogicManager.GetItemDef(boolName);
                return new BigItemDef[]
                {
                    new BigItemDef()
                    {
                        BoolName = item.boolName,
                        SpriteKey = item.bigSpriteKey,
                        TakeKey = item.takeKey,
                        NameKey = item.nameKey,
                        ButtonKey = item.buttonKey,
                        DescOneKey = item.descOneKey,
                        DescTwoKey = item.descTwoKey
                    }
                };
            }
        }

        private static string GetAdditiveBoolName(string boolName)
        {
            if (additiveCounts == null)
            {
                additiveCounts = new Dictionary<string, int>();
                foreach (string str in LogicManager.AdditiveItemNames)
                {
                    additiveCounts.Add(str, 0);
                }
            }

            string prefix = GetAdditivePrefix(boolName);
            if (!string.IsNullOrEmpty(prefix))
            {
                additiveCounts[prefix] = additiveCounts[prefix] + 1;
                return prefix + additiveCounts[prefix];
            }

            return null;
        }

        private static void LogAllPlacements()
        {
            RandomizerMod.Instance.Log("Logging progression item placements:");
            foreach (KeyValuePair<string, List<string>> kvp in shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (LogicManager.GetItemDef(item).progression) LogItemPlacement(item, kvp.Key);
                }
            }
            foreach (KeyValuePair<string, string> kvp in nonShopItems)
            {
                if (LogicManager.GetItemDef(kvp.Value).progression) LogItemPlacement(kvp.Value, kvp.Key);
            }
            RandomizerMod.Instance.Log(".");
            RandomizerMod.Instance.Log("Logging ordinary item placements:");
            foreach (KeyValuePair<string, List<string>> kvp in shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (!LogicManager.GetItemDef(item).progression) LogItemPlacement(item, kvp.Key);
                }
            }
            foreach (KeyValuePair<string, string> kvp in nonShopItems)
            {
                if (!LogicManager.GetItemDef(kvp.Value).progression) LogItemPlacement(kvp.Value, kvp.Key);
            }
        }

        private static void LogItemPlacement(string item, string location)
        {
            RandomizerMod.Instance.Settings.itemPlacements.Add(item, location);
            RandomizerMod.Instance.Log($"Putting item \"{item.Replace('_', ' ')}\" at \"{location.Replace('_', ' ')}\"");
        }
    }
}
