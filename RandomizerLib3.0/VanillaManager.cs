using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerLib
{
    public class VanillaManager
    {
        public HashSet<string> locationsObtained;

        public static HashSet<string> progressionLocations;
        public static Dictionary<string, HashSet<string>> progressionShopItems;
        public static Dictionary<string, string> progressionNonShopItems;
        public static List<(string, string)> ItemPlacements { get; private set; }

		public VanillaManager()
        {
            locationsObtained = new HashSet<string>();
        }

        public static void SetupVanilla(RandoSettings settings)
        {
            ItemPlacements = new List<(string, string)>();
            progressionLocations = new HashSet<string>();
            progressionShopItems = new Dictionary<string, HashSet<string>>();
            progressionNonShopItems = new Dictionary<string, string>();

            //Set up vanillaLocations
            //    Not as cool as all the hashset union stuff :(
            foreach (string item in GetVanillaItems(settings))
            {
                ReqDef itemDef = LogicManager.GetItemDef(item);
                if (itemDef.type == ItemType.Shop && LogicManager.ShopNames.Contains(itemDef.shopName))
                {
                    ItemPlacements.Add((item, itemDef.shopName));

                    //Add shop to locations
                    if (itemDef.progression && !progressionLocations.Contains(itemDef.shopName))
                        progressionLocations.Add(itemDef.shopName);

                    //Add items to the shop items
                    if (itemDef.progression)
                    {
                        if (progressionShopItems.ContainsKey(itemDef.shopName))
                            //Shop's here, but item's not.
                            progressionShopItems[itemDef.shopName].Add(item);
                        else
                            //Shop's not here, so add the shop and the item to it.
                            progressionShopItems.Add(itemDef.shopName, new HashSet<string>() { item });
                    }

                    continue;
                }
                else
                {
                    ItemPlacements.Add((item, item));
                    //Not a shop!
                    if (itemDef.progression)
                        progressionNonShopItems.Add(item, item);
                }

                if (itemDef.progression)
                    progressionLocations.Add(item);
            }
        }

        internal void ResetReachableLocations(ItemManager im, bool doUpdateQueue = true, ProgressionManager _pm = null)
        {
            if (_pm == null) _pm = im.pm;
            locationsObtained = new HashSet<string>();
            if (!doUpdateQueue) return;

            foreach (string location in progressionLocations)
            {
                if (_pm.CanGet(location))
                    UpdateVanillaLocations(im, location, doUpdateQueue);
            }
            if (doUpdateQueue) im.UpdateReachableLocations();
        }

        internal void UpdateVanillaLocations(ItemManager im, string location, bool doUpdateQueue = true, ProgressionManager _pm = null)
        {
            if (_pm == null) _pm = im.pm;
            if (locationsObtained.Contains(location))
            {
                return;
            }

            if (progressionShopItems.ContainsKey(location))
            { // shop in vanilla
                foreach (string shopItem in progressionShopItems[location])
                {
                    _pm.Add(shopItem);
                    if (doUpdateQueue) im.updateQueue.Enqueue(shopItem);
                }
            }
            else
            { // item in vanilla
                _pm.Add(location);
                if (doUpdateQueue) im.updateQueue.Enqueue(location);
            }

            locationsObtained.Add(location);
        }

        // currently unused, worry about this later
        /*public static bool TryGetVanillaTransitionProgression(string transition, out HashSet<string> progression)
        {
            progression = new HashSet<string>(LogicManager.GetLocationsByProgression(new List<string>{ transition }));
            if (progression.Any(l => progressionShopItems.ContainsKey(l)))
            {
                return true;
            }
            progression.IntersectWith(progressionLocations);

            return progression.Any();
        }*/


        public static HashSet<string> GetVanillaItems(RandoSettings settings)
        {
            HashSet<string> unrandoItems = new HashSet<string>();

            if (!settings.RandomizeDreamers) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Dreamer"));
            if (!settings.RandomizeSkills) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Skill"));
            if (!settings.RandomizeCharms) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Charm"));
            if (!settings.RandomizeKeys) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Key"));
            if (!settings.RandomizeMaskShards) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Mask"));
            if (!settings.RandomizeVesselFragments) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Vessel"));
            if (!settings.RandomizePaleOre) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Ore"));
            if (!settings.RandomizeCharmNotches) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Notch"));
            if (!settings.RandomizeGeoChests) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Geo"));
            if (!settings.RandomizeRancidEggs) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Egg"));
            if (!settings.RandomizeRelics) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Relic"));
            if (!settings.RandomizeMaps) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Map"));
            if (!settings.RandomizeStags) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Stag"));
            if (!settings.RandomizeRocks) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Rock"));
            if (!settings.RandomizeSoulTotems) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Soul"));
            if (!settings.RandomizePalaceTotems) unrandoItems.UnionWith(LogicManager.GetItemsByPool("PalaceSoul"));
            if (!settings.RandomizeLoreTablets) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Lore"));
            // intercept maps and stags in randomizer action since the vanilla placement is much preferable to shinies
            // no reason to include grubs or essence. Logic for vanilla placements is handled directly in the progression manager

            return unrandoItems;
        }

        public static HashSet<string> GetVanillaProgression(RandoSettings settings)
        {
            HashSet<string> unrandoItems = new HashSet<string>();

            if (!settings.RandomizeDreamers) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Dreamer"));
            if (!settings.RandomizeSkills) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Skill"));
            if (!settings.RandomizeCharms) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Charm"));
            if (!settings.RandomizeKeys) unrandoItems.UnionWith(LogicManager.GetItemsByPool("Key"));
            // no reason to search other pools, because only this class of items can be progression in their vanilla locations
            // used for managing transition randomizer

            unrandoItems.IntersectWith(LogicManager.ProgressionItems);

            return unrandoItems;
        }
    }
}
