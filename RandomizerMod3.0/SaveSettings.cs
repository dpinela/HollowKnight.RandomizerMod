using System.Linq;
using System.Collections.Generic;
using Modding;
using RandomizerMod.Actions;
using SereCore;
using RandomizerMod.Randomization;
using static RandomizerMod.LogHelper;

using RandomizerLib;
using System;
using RandomizerMod.Components;

namespace RandomizerMod
{
    public class SaveSettings : BaseSettings
    {
        /*
         * UNLISTED BOOLS
         * rescuedSly is used in room randomizer to control when Sly appears in the shop, separately from when the door is unlocked
         */

        private SerializableStringDictionary _itemPlacements = new SerializableStringDictionary();
        private SerializableIntDictionary _orderedLocations = new SerializableIntDictionary();

        public SerializableStringDictionary _transitionPlacements = new SerializableStringDictionary();
        private SerializableIntDictionary _variableCosts = new SerializableIntDictionary();
        private SerializableIntDictionary _shopCosts = new SerializableIntDictionary();
        private SerializableIntDictionary _additiveCounts = new SerializableIntDictionary();
        private SerializableDictionary<int, string> _mwPlayerNames = new SerializableDictionary<int, string>();

        private SerializableBoolDictionary _obtainedItems = new SerializableBoolDictionary();
        private SerializableBoolDictionary _obtainedLocations = new SerializableBoolDictionary();
        private SerializableBoolDictionary _obtainedTransitions = new SerializableBoolDictionary();

        private SerializableBoolDictionary _sentItems = new SerializableBoolDictionary();

        private RandoSettings _randoSettings = new RandoSettings();

        /// <remarks>item, location</remarks>
        public (string, string)[] ItemPlacements => _itemPlacements.Select(pair => (pair.Key, pair.Value)).ToArray();

        public int MaxOrder => _orderedLocations.Count;

        public int NumItemsFound => _obtainedItems.Keys.Intersect(_itemPlacements.Keys).Count();

        public (string, int)[] VariableCosts => _variableCosts.Select(pair => (pair.Key, pair.Value)).ToArray();
        public (string, int)[] ShopCosts => _shopCosts.Select(pair => (pair.Key, pair.Value)).ToArray();

        public bool RandomizeTransitions => RandomizeAreas || RandomizeRooms;

        public bool IsMW => MWNumPlayers > 1;

        public string[] UnconfirmedItems => _sentItems.Where(kvp => !kvp.Value).Select(kvp => kvp.Key).ToArray();

        public bool FreeLantern => !(DarkRooms || RandomizeKeys);
        public SaveSettings()
        {
            AfterDeserialize += () =>
            {
                LanguageStringManager.SetMWNames(_mwPlayerNames);
                RandomizerAction.CreateActions(ItemPlacements, this);
                if (IsMW)
                {
                    try
                    {
                        /*RandomizerMod.Instance.mwConnection.Disconnect();
                        RandomizerMod.Instance.mwConnection = new MultiWorld.ClientConnection();*/
                        RandomizerMod.Instance.mwConnection.Connect();
                        RandomizerMod.Instance.mwConnection.JoinRando(MWRandoId, MWPlayerId);
                    } catch (Exception) {}
                }
            };
        }

        public RandoSettings RandomizerSettings => _randoSettings;

        public int MWNumPlayers
        {
            get => GetInt(1);
            set => SetInt(value);
        }
        public int MWPlayerId
        {
            get => GetInt(0);
            set => SetInt(value);
        }

        public int MWRandoId
        {
            get => GetInt();
            set => SetInt(value);
        }

        public int JijiHintCounter
        {
            get => GetInt(0);
            set => SetInt(value);
        }
        public int QuirrerHintCounter
        {
            get => GetInt(0);
            set => SetInt(value);
        }

        public bool AllBosses
        {
            get => _randoSettings.AllBosses;
            set => _randoSettings.AllBosses = value;
        }

        public bool AllSkills
        {
            get => _randoSettings.AllSkills;
            set => _randoSettings.AllSkills = value;
        }

        public bool AllCharms
        {
            get => _randoSettings.AllCharms;
            set => _randoSettings.AllCharms = value;
        }

        public bool CharmNotch
        {
            get => _randoSettings.CharmNotch;
            set => _randoSettings.CharmNotch = value;
        }

        public bool Grubfather
        {
            get => _randoSettings.Grubfather;
            set => _randoSettings.Grubfather = value;
        }
        public bool Jiji
        {
            get => _randoSettings.Jiji;
            set => _randoSettings.Jiji = value;
        }
        public bool Quirrel
        {
            get => _randoSettings.Quirrel;
            set => _randoSettings.Quirrel = value;
        }
        public bool ItemDepthHints
        {
            get => _randoSettings.ItemDepthHints;
            set => _randoSettings.ItemDepthHints = value;
        }

        public bool EarlyGeo
        {
            get => _randoSettings.EarlyGeo;
            set => _randoSettings.EarlyGeo = value;
        }

        public bool LeverSkips
        {
            get => _randoSettings.LeverSkips;
            set => _randoSettings.LeverSkips = value;
        }

        public bool ExtraPlatforms
        {
            get => _randoSettings.ExtraPlatforms;
            set => _randoSettings.ExtraPlatforms = value;
        }

        public bool Randomizer
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeAreas
        {
            get => _randoSettings.RandomizeAreas;
            set => _randoSettings.RandomizeAreas = value;
        }
        public bool RandomizeRooms
        {
            get => _randoSettings.RandomizeRooms;
            set => _randoSettings.RandomizeRooms = value;
        }
        public bool ConnectAreas
        {
            get => _randoSettings.ConnectAreas;
            set => _randoSettings.ConnectAreas = value;
        }
        public bool SlyCharm
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeDreamers
        {
            get => _randoSettings.RandomizeDreamers;
            set => _randoSettings.RandomizeDreamers = value;
        }
        public bool RandomizeSkills
        {
            get => _randoSettings.RandomizeSkills;
            set => _randoSettings.RandomizeSkills = value;
        }
        public bool RandomizeCharms
        {
            get => _randoSettings.RandomizeCharms;
            set => _randoSettings.RandomizeCharms = value;
        }
        public bool RandomizeKeys
        {
            get => _randoSettings.RandomizeKeys;
            set => _randoSettings.RandomizeKeys = value;
        }
        public bool RandomizeGeoChests
        {
            get => _randoSettings.RandomizeGeoChests;
            set => _randoSettings.RandomizeGeoChests = value;
        }
        public bool RandomizeMaskShards
        {
            get => _randoSettings.RandomizeMaskShards;
            set => _randoSettings.RandomizeMaskShards = value;
        }
        public bool RandomizeVesselFragments
        {
            get => _randoSettings.RandomizeVesselFragments;
            set => _randoSettings.RandomizeVesselFragments = value;
        }
        public bool RandomizeCharmNotches
        {
            get => _randoSettings.RandomizeCharmNotches;
            set => _randoSettings.RandomizeCharmNotches = value;
        }
        public bool RandomizePaleOre
        {
            get => _randoSettings.RandomizePaleOre;
            set => _randoSettings.RandomizePaleOre = value;
        }
        public bool RandomizeRancidEggs
        {
            get => _randoSettings.RandomizeRancidEggs;
            set => _randoSettings.RandomizeRancidEggs = value;
        }
        public bool RandomizeRelics
        {
            get => _randoSettings.RandomizeRelics;
            set => _randoSettings.RandomizeRelics = value;
        }

        public bool RandomizeMaps
        {
            get => _randoSettings.RandomizeMaps;
            set => _randoSettings.RandomizeMaps = value;
        }

        public bool RandomizeStags
        {
            get => _randoSettings.RandomizeStags;
            set => _randoSettings.RandomizeStags = value;
        }

        public bool RandomizeGrubs
        {
            get => _randoSettings.RandomizeGrubs;
            set => _randoSettings.RandomizeGrubs = value;
        }

        public bool RandomizeWhisperingRoots
        {
            get => _randoSettings.RandomizeWhisperingRoots;
            set => _randoSettings.RandomizeWhisperingRoots = value;
        }
        
        public bool RandomizeRocks
        {
            get => _randoSettings.RandomizeRocks;
            set => _randoSettings.RandomizeRocks = value;
        }
        
        public bool RandomizeSoulTotems
        {
            get => _randoSettings.RandomizeSoulTotems;
            set => _randoSettings.RandomizeSoulTotems = value;
        }
        
        public bool RandomizePalaceTotems
        {
            get => _randoSettings.RandomizePalaceTotems;
            set => _randoSettings.RandomizePalaceTotems = value;
        }
        
        public bool RandomizeLoreTablets
        {
            get => _randoSettings.RandomizeLoreTablets;
            set => _randoSettings.RandomizeLoreTablets = value;
        }

        public bool RandomizeLifebloodCocoons
        {
            get => _randoSettings.RandomizeLifebloodCocoons;
            set => _randoSettings.RandomizeLifebloodCocoons = value;
        }

        public bool DuplicateMajorItems
        {
            get => _randoSettings.DuplicateMajorItems;
            set => _randoSettings.DuplicateMajorItems = value;
        }

        internal bool GetRandomizeByPool(string pool)
        {
            return _randoSettings.GetRandomizeByPool(pool);
        }


        public bool CreateSpoilerLog
        {
            get => _randoSettings.CreateSpoilerLog;
            set => _randoSettings.CreateSpoilerLog = value;
        }

        public bool Cursed
        {
            get => _randoSettings.Cursed;
            set => _randoSettings.Cursed = value;
        }

        public bool RandomizeStartItems
        {
            get => _randoSettings.RandomizeStartItems;
            set => _randoSettings.RandomizeStartItems = value;
        }

        public bool RandomizeStartLocation
        {
            get => _randoSettings.RandomizeStartLocation;
            set => _randoSettings.RandomizeStartLocation = value;
        }

        // The following settings names are referenced in Benchwarp. Please do not change!
        public string StartName
        {
            get => _randoSettings.StartName;
            set => _randoSettings.StartName = value;
        }

        public string StartSceneName
        {
            get => GetString("Tutorial_01");
            set => SetString(value);
        }

        public string StartRespawnMarkerName
        {
            get => GetString("Randomizer Respawn Marker");
            set => SetString(value);
        }

        public int StartRespawnType
        {
            get => GetInt(0);
            set => SetInt(value);
        }

        public int StartMapZone
        {
            get => GetInt((int)GlobalEnums.MapZone.KINGS_PASS);
            set => SetInt(value);
        }
        // End Benchwarp block.

        public bool ShadeSkips
        {
            get => _randoSettings.ShadeSkips;
            set => _randoSettings.ShadeSkips = value;
        }

        public bool AcidSkips
        {
            get => _randoSettings.AcidSkips;
            set => _randoSettings.AcidSkips = value;
        }

        public bool SpikeTunnels
        {
            get => _randoSettings.SpikeTunnels;
            set => _randoSettings.SpikeTunnels = value;
        }

        public bool MildSkips
        {
            get => _randoSettings.MildSkips;
            set => _randoSettings.MildSkips = value;
        }

        public bool SpicySkips
        {
            get => _randoSettings.SpicySkips;
            set => _randoSettings.SpicySkips = value;
        }

        public bool FireballSkips
        {
            get => _randoSettings.FireballSkips;
            set => _randoSettings.FireballSkips = value;
        }

        public bool DarkRooms
        {
            get => _randoSettings.DarkRooms;
            set => _randoSettings.DarkRooms = value;
        }

        public int Seed
        {
            get => _randoSettings.Seed;
            set => _randoSettings.Seed = value;
        }
        public int GeoSeed
        {
            get => _randoSettings.GeoSeed;
            set => _randoSettings.GeoSeed = value;
        }

        public void ResetPlacements()
        {
            _itemPlacements = new SerializableStringDictionary();
            _orderedLocations = new SerializableIntDictionary();
            _transitionPlacements = new SerializableStringDictionary();
            _variableCosts = new SerializableIntDictionary();
            _shopCosts = new SerializableIntDictionary();
            _additiveCounts = new SerializableIntDictionary();

            _obtainedItems = new SerializableBoolDictionary();
            _obtainedLocations = new SerializableBoolDictionary();
            _obtainedTransitions = new SerializableBoolDictionary();
        }

        public void AddItemPlacement(string item, string location)
        {
            _itemPlacements[item] = location;
        }

        public void AddOrderedLocation(string location, int order)
        {
            _orderedLocations[location] = order;
        }

        public int GetLocationOrder(string location)
        {
            return _orderedLocations[location];
        }

        public string GetNthLocation(int n)
        {
            return _orderedLocations.FirstOrDefault(kvp => kvp.Value == n).Key;
        }

        public string[] GetNthLocationItems(int n)
        {
            string location = GetNthLocation(n);
            return ItemPlacements.Where(pair => pair.Item2 == location).Select(pair => pair.Item1).ToArray();
        }
        
        public void AddTransitionPlacement(string entrance, string exit)
        {
            _transitionPlacements[entrance] = exit;
        }

        public void AddNewCost(string item, int cost)
        {
            _variableCosts[item] = cost;
        }

        public void AddShopCost(string item, int cost)
        {
            _shopCosts[item] = cost;
        }

        public int GetShopCost(string item)
        {
            return _shopCosts[item];
        }


        public void MarkItemFound(string item)
        {
            _obtainedItems[item] = true;
        }

        public bool CheckItemFound(string item)
        {
            if (!_obtainedItems.TryGetValue(item, out bool found)) return false;
            return found;
        }

        public string[] GetItemsFound()
        {
            return _obtainedItems.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
        }

        public int GetNumLocations()
        {
            return _orderedLocations.Count + _shopCosts.Count - 5;
        }

        public HashSet<string> GetPlacedItems()
        {
            return new HashSet<string>(ItemPlacements.Select(pair => pair.Item1));
        }

        public string GetItemLocation(string item)
        {
            return _itemPlacements[item];
        }

        public void MarkLocationFound(string location)
        {
            _obtainedLocations[location] = true;
        }

        public bool CheckLocationFound(string location)
        {
            if (!_obtainedLocations.TryGetValue(location, out bool found)) return false;
            return found;
        }

        public string[] GetLocationsFound()
        {
            return _obtainedLocations.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
        }

        public void MarkTransitionFound(string transition)
        {
            _obtainedTransitions[transition] = true;
        }

        public bool CheckTransitionFound(string transition)
        {
            if (!_obtainedTransitions.TryGetValue(transition, out bool found)) return false;
            return found;
        }

        public string[] GetTransitionsFound()
        {
            return _obtainedTransitions.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
        }

        public int GetAdditiveCount(string item)
        {
            string[] additiveSet = LogicManager.AdditiveItemSets.FirstOrDefault(set => set.Contains(item));
            if (additiveSet is null) return 0;
            if (!_additiveCounts.TryGetValue(additiveSet[0], out int count))
            {
                _additiveCounts.Add(additiveSet[0], 0);
                count = 0;
            }
            return count;
        }

        public void IncrementAdditiveCount(string item)
        {
            string[] additiveSet = LogicManager.AdditiveItemSets.FirstOrDefault(set => set.Contains(item));
            if (additiveSet is null) return;
            if (!_additiveCounts.ContainsKey(additiveSet[0]))
            {
                _additiveCounts.Add(additiveSet[0], 0);
            }
            _additiveCounts[additiveSet[0]]++;
        }

        public string GetCurrentAdditiveItem(string item)
        {
            string[] itemSet = LogicManager.AdditiveItemSets.FirstOrDefault(set => set.Contains(item));

            if (itemSet != null)
            {
                int current = GetAdditiveCount(item);
                int max = LogicManager.GetMaxAdditiveLevel(item);
                int next = Math.Min(current + 1, max);
                item = itemSet[next - 1];
            }

            return item;
        }

        internal void SetMWNames(List<string> nicknames)
        {
            for (int i = 0; i < nicknames.Count; i++)
            {
                _mwPlayerNames[i] = nicknames[i];
            }
        }

        public void AddSentItem(string item)
        {
            _sentItems[item] = false;
        }

        public void MarkItemConfirmed(string item)
        {
            _sentItems[item] = true;
        }

        public string GetMWPlayerName(int playerId)
        {
            string name = "Player " + (playerId + 1);
            if (_mwPlayerNames != null && _mwPlayerNames.ContainsKey(playerId))
            {
                name = _mwPlayerNames[playerId];
            }
            return name;
        }
    }
}
