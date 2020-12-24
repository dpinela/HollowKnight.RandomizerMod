using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RandomizerMod.LogHelper;
using static RandomizerMod.Randomization.Randomizer;

namespace RandomizerMod.Randomization
{
    internal static class PreRandomizer
    {
        public static void RandomizeNonShopCosts(Random rand, SaveSettings settings)
        {
            foreach (string item in LogicManager.ItemNames)
            {
                ReqDef def = LogicManager.GetItemDef(item);
                if (!settings.GetRandomizeByPool(def.pool))
                {
                    settings.AddNewCost(item, def.cost);
                    continue; //Skip cost rando if this item's pool is vanilla
                }

                if (def.costType == Actions.AddYNDialogueToShiny.CostType.Essence) //essence cost
                {
                    int cost = 1 + rand.Next(MAX_ESSENCE_COST);

                    def.cost = cost;
                    LogicManager.EditItemDef(item, def); // really shouldn't be editing this, bad idea
                    settings.AddNewCost(item, cost);
                    continue;
                }

                if (def.costType == Actions.AddYNDialogueToShiny.CostType.Grub) //grub cost
                {
                    int cost = 1 + rand.Next(MAX_GRUB_COST);

                    def.cost = cost;
                    LogicManager.EditItemDef(item, def); // yeah, I'm probably not fixing it though
                    settings.AddNewCost(item, cost);
                    continue;
                }
            }
        }

        public static (List<string>, List<string>) RandomizeStartingItems(Random rand, SaveSettings settings)
        {
            List<string> startItems = new List<string>();
            List<string> startProgression = new List<string>();
            if (!settings.RandomizeStartItems) return (startItems, startProgression);

            List<string> pool1 = new List<string> { "Mantis_Claw", "Monarch_Wings" };
            List<string> pool2 = new List<string> { "Mantis_Claw", "Monarch_Wings", "Mothwing_Cloak", "Crystal_Heart" };
            List<string> pool3 = new List<string> { "Shade_Cloak", "Isma's_Tear", "Vengeful_Spirit", "Howling_Wraiths", "Desolate_Dive", "Cyclone_Slash", "Great_Slash", "Dash_Slash", "Dream_Nail" };
            List<string> pool4 = new List<string> { "City_Crest", "Lumafly_Lantern", "Tram_Pass", "Simple_Key-Sly", "Shopkeeper's_Key", "Elegant_Key", "Love_Key", "King's_Brand" };

            startItems.Add(pool1[rand.Next(pool1.Count)]);

            pool2.Remove(startItems[0]);
            startItems.Add(pool2[rand.Next(pool2.Count)]);


            for (int i = rand.Next(4); i > 0; i--)
            {
                startItems.Add(pool3[rand.Next(pool3.Count)]);
                pool3.Remove(startItems.Last());
            }

            for (int i = rand.Next(7 - startItems.Count); i > 0; i--) // no more than 4 tier3 or tier4 items
            {
                startItems.Add(pool4[rand.Next(pool4.Count)]);
                pool4.Remove(startItems.Last());
            }

            for (int i = rand.Next(2) + 1; i > 0; i--)
            {
                List<string> charms = LogicManager.ItemNames.Where(_item => LogicManager.GetItemDef(_item).action == GiveItemActions.GiveAction.Charm).Except(startItems).ToList();
                startItems.Add(charms[rand.Next(charms.Count)]);
            }

            if (startProgression == null) startProgression = new List<string>();

            foreach (string item in startItems)
            {
                if (LogicManager.GetItemDef(item).progression) startProgression.Add(item);
            }
            return (startItems, startProgression);
        }

        public static string RandomizeStartingLocation(Random rand, SaveSettings settings, List<String> startProgression)
        {
            string StartName = null;
            if (settings.RandomizeStartLocation)
            {
                List<string> startLocations = LogicManager.StartLocations.Where(start => TestStartLocation(settings, start)).Except(new string[] { "King's Pass" }).ToList();
                StartName = startLocations[rand.Next(startLocations.Count)];
            }
            else if (!LogicManager.StartLocations.Contains(settings.StartName))
            {
                StartName = "King's Pass";
            }
            else StartName = settings.StartName;

            Log("Setting start location as " + StartName);

            StartDef def = LogicManager.GetStartLocation(StartName);

            if (startProgression == null)
            {
                startProgression = new List<string>();
            }
            if (!settings.RandomizeRooms)
            {
                startProgression.Add(def.waypoint);
            }
            if (settings.RandomizeAreas && !string.IsNullOrEmpty(def.areaTransition))
            {
                startProgression.Add(def.areaTransition);
            }
            if (settings.RandomizeRooms)
            {
                startProgression.Add(def.roomTransition);
            }

            return StartName;
        }
        private static bool TestStartLocation(SaveSettings settings, string start)
        {
            // could potentially add logic checks here in the future
            StartDef startDef = LogicManager.GetStartLocation(start);
            if (settings.RandomizeStartItems)
            {
                return true;
            }
            if (settings.RandomizeRooms)
            {
                if (startDef.roomSafe)
                {
                    return true;
                }
                else return false;
            }
            if (settings.RandomizeAreas)
            {
                if (startDef.areaSafe)
                {
                    return true;
                }
                else return false;
            }
            if (startDef.itemSafe) return true;
            return false;
        }
    }
}
