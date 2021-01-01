using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerMod.Randomization.MultiWorld;

namespace RandomizerMod.Randomization.MultiWorld
{
    public class MWVanillaManager
    {
        public HashSet<MWItem> locationsObtained;

        public HashSet<MWItem> progressionLocations;
        public Dictionary<MWItem, HashSet<MWItem>> progressionShopItems;
        public Dictionary<MWItem, MWItem> progressionNonShopItems;
        public List<(MWItem, MWItem)> ItemPlacements { get; private set; }

        private int players;
        private List<RandoSettings> settings;

		public MWVanillaManager(int players, List<RandoSettings> settings)
        {
            this.players = players;
            this.settings = settings;

            locationsObtained = new HashSet<MWItem>();
            ItemPlacements = new List<(MWItem, MWItem)>();
            progressionLocations = new HashSet<MWItem>();
            progressionShopItems = new Dictionary<MWItem, HashSet<MWItem>>();
            progressionNonShopItems = new Dictionary<MWItem, MWItem>();

            //Set up vanillaLocations
            //    Not as cool as all the hashset union stuff :(
            foreach (MWItem item in GetVanillaItems())
            {
                ReqDef itemDef = LogicManager.GetItemDef(item.item);
                if (itemDef.type == ItemType.Shop && LogicManager.ShopNames.Contains(itemDef.shopName))
                {
                    MWItem mwShop = new MWItem(item.playerId, itemDef.shopName);

                    ItemPlacements.Add((item, mwShop));

                    //Add shop to locations
                    if (itemDef.progression && !progressionLocations.Contains(mwShop))
                        progressionLocations.Add(mwShop);

                    //Add items to the shop items
                    if (itemDef.progression)
                    {
                        if (progressionShopItems.ContainsKey(mwShop))
                            //Shop's here, but item's not.
                            progressionShopItems[mwShop].Add(item);
                        else
                            //Shop's not here, so add the shop and the item to it.
                            progressionShopItems.Add(mwShop, new HashSet<MWItem>() { item });
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

        internal void ResetReachableLocations(MWItemManager im, bool doUpdateQueue = true, MWProgressionManager _pm = null)
        {
            if (_pm == null) _pm = im.pm;
            locationsObtained = new HashSet<MWItem>();
            if (!doUpdateQueue) return;

            foreach (MWItem location in progressionLocations)
            {
                if (_pm.CanGet(location))
                    UpdateVanillaLocations(im, location, doUpdateQueue);
            }
            if (doUpdateQueue) im.UpdateReachableLocations();
        }

        internal void UpdateVanillaLocations(MWItemManager im, MWItem location, bool doUpdateQueue = true, MWProgressionManager _pm = null)
        {
            if (_pm == null) _pm = im.pm;
            if (locationsObtained.Contains(location))
            {
                return;
            }

            if (progressionShopItems.ContainsKey(location))
            { // shop in vanilla
                foreach (MWItem shopItem in progressionShopItems[location])
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

        public bool TryGetVanillaTransitionProgression(MWItem transition, out HashSet<MWItem> progression)
        {
            progression = new HashSet<MWItem>(LogicManager.GetLocationsByProgression(new List<string>{ transition.item }).Select(loc => new MWItem(transition.playerId, loc)));
            if (progression.Any(l => progressionShopItems.ContainsKey(l)))
            {
                return true;
            }
            progression.IntersectWith(progressionLocations);

            return progression.Any();
        }

        private HashSet<MWItem> GetVanillaItems()
        {
            HashSet<MWItem> unrandoItems = new HashSet<MWItem>();
            for (int i = 0; i < players; i++)
            {
                foreach (string item in VanillaManager.GetVanillaItems(settings[i]))
                {
                    unrandoItems.Add(new MWItem(i, item));
                }
            }

            return unrandoItems;
        }

        /*public HashSet<MWItem> GetVanillaProgression()
        {
            HashSet<MWItem> unrandoItems = new HashSet<MWItem>();
            for (int i = 0; i < players; i++)
            {
                foreach (string item in VanillaManager.GetVanillaProgression(settings[i]))
                {
                    unrandoItems.Add(new MWItem(i, item));
                }
            }

            return unrandoItems;
        }*/
    }
}
