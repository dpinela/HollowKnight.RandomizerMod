using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerLib
{
    [Serializable]
    public class RandoSettings
    {
        // Restriction rules (not used?)

        public bool AllBosses = false;
        public bool AllSkills = false;
        public bool AllCharms = false;

        // Quality of life changes

        public bool CharmNotch = false;
        public bool Grubfather = false;
        public bool Jiji = false;
        public bool Quirrel = false; // Unused?
        public bool ItemDepthHints = false; // Unused?
        public bool EarlyGeo = false;
        public bool LeverSkips = false;
        public bool ExtraPlatforms = false;

        // Randomizer type

        public bool RandomizeAreas = false;
        public bool RandomizeRooms = false;
        public bool RandomizeTransitions => RandomizeAreas || RandomizeRooms;
        public bool ConnectAreas = false;

        // Randomized item types

        public bool RandomizeDreamers = false;
        public bool RandomizeSkills = false;
        public bool RandomizeCharms = false;
        public bool RandomizeKeys = false;
        public bool RandomizeGeoChests = false;
        public bool RandomizeMaskShards = false;
        public bool RandomizeVesselFragments = false;
        public bool RandomizeCharmNotches = false;
        public bool RandomizePaleOre = false;
        public bool RandomizeRancidEggs = false;
        public bool RandomizeRelics = false;
        public bool RandomizeMaps = false;
        public bool RandomizeStags = false;
        public bool RandomizeGrubs = false;
        public bool RandomizeWhisperingRoots = false;
        public bool RandomizeRocks = false;
        public bool RandomizeSoulTotems = false;
        public bool RandomizePalaceTotems = false;
        public bool RandomizeLoreTablets = false;
        public bool RandomizeLifebloodCocoons = false;

        // Extra rando settings

        public string StartName = "King's Pass";
        public bool DuplicateMajorItems = false;
        public bool CreateSpoilerLog = false;
        public bool Cursed = false;
        public bool RandomizeStartItems = false;
        public bool RandomizeStartLocation = false;

        // Logic rules (allowed skips)

        public bool ShadeSkips = false;
        public bool AcidSkips = false;
        public bool SpikeTunnels = false;
        public bool MildSkips = false;
        public bool SpicySkips = false;
        public bool FireballSkips = false;
        public bool DarkRooms = false;

        // Seed

        public int Seed = -1;
        public int GeoSeed = -1;
        public bool GetRandomizeByPool(string pool)
        {
            switch (pool)
            {
                case "Dreamer":
                    return RandomizeDreamers;
                case "Skill":
                    return RandomizeSkills;
                case "Charm":
                    return RandomizeCharms;
                case "Key":
                    return RandomizeKeys;
                case "Mask":
                    return RandomizeMaskShards;
                case "Vessel":
                    return RandomizeVesselFragments;
                case "Ore":
                    return RandomizePaleOre;
                case "Notch":
                    return RandomizeCharmNotches;
                case "Geo":
                    return RandomizeGeoChests;
                case "Egg":
                    return RandomizeRancidEggs;
                case "Relic":
                    return RandomizeRelics;
                case "Map":
                    return RandomizeMaps;
                case "Stag":
                    return RandomizeStags;
                case "Grub":
                    return RandomizeGrubs;
                case "Root":
                    return RandomizeWhisperingRoots;
                case "Rock":
                    return RandomizeRocks;
                case "Soul":
                    return RandomizeSoulTotems;
                case "PalaceSoul":
                    return RandomizePalaceTotems;
                case "Lore":
                    return RandomizeLoreTablets;
                case "Lifeblood":
                    return RandomizeLifebloodCocoons;
                default:
                    return false;
            }
        }

        public RandoSettings Clone()
        {
            RandoSettings result = new RandoSettings();
            result.AllBosses = AllBosses;
            result.AllSkills = AllSkills;
            result.AllCharms = AllCharms;
            result.CharmNotch = CharmNotch;
            result.Grubfather = Grubfather;
            result.Jiji = Jiji;
            result.Quirrel = Quirrel; // Unused?
            result.ItemDepthHints = ItemDepthHints; // Unused?
            result.EarlyGeo = EarlyGeo;
            result.LeverSkips = LeverSkips;
            result.ExtraPlatforms = ExtraPlatforms;
            result.RandomizeAreas = RandomizeAreas;
            result.RandomizeRooms = RandomizeRooms;
            result.ConnectAreas = ConnectAreas;
            result.RandomizeDreamers = RandomizeDreamers;
            result.RandomizeSkills = RandomizeSkills;
            result.RandomizeCharms = RandomizeCharms;
            result.RandomizeKeys = RandomizeKeys;
            result.RandomizeGeoChests = RandomizeGeoChests;
            result.RandomizeMaskShards = RandomizeMaskShards;
            result.RandomizeVesselFragments = RandomizeVesselFragments;
            result.RandomizeCharmNotches = RandomizeCharmNotches;
            result.RandomizePaleOre = RandomizePaleOre;
            result.RandomizeRancidEggs = RandomizeRancidEggs;
            result.RandomizeRelics = RandomizeRelics;
            result.RandomizeMaps = RandomizeMaps;
            result.RandomizeStags = RandomizeStags;
            result.RandomizeGrubs = RandomizeGrubs;
            result.RandomizeWhisperingRoots = RandomizeWhisperingRoots;
            result.RandomizeRocks = RandomizeRocks;
            result.RandomizeSoulTotems = RandomizeSoulTotems;
            result.RandomizePalaceTotems = RandomizePalaceTotems;
            result.RandomizeLoreTablets = RandomizeLoreTablets;
            result.RandomizeLifebloodCocoons = RandomizeLifebloodCocoons;
            result.StartName = StartName;
            result.DuplicateMajorItems = DuplicateMajorItems;
            result.CreateSpoilerLog = CreateSpoilerLog;
            result.Cursed = Cursed;
            result.RandomizeStartItems = RandomizeStartItems;
            result.RandomizeStartLocation = RandomizeStartLocation;
            result.ShadeSkips = ShadeSkips;
            result.AcidSkips = AcidSkips;
            result.SpikeTunnels = SpikeTunnels;
            result.MildSkips = MildSkips;
            result.SpicySkips = SpicySkips;
            result.FireballSkips = FireballSkips;
            result.DarkRooms = DarkRooms;
            result.Seed = Seed;
            result.GeoSeed = GeoSeed;

            return result;
        }

        public int GetSettingsSeed()
        {
            int settingsSeed = 0;
            if (RandomizeDreamers) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeSkills) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeCharms) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeKeys) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeGeoChests) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeMaskShards) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeVesselFragments) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeCharmNotches) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizePaleOre) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeRancidEggs) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeRelics) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeMaps) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeStags) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeGrubs) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeWhisperingRoots) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeRocks) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeSoulTotems) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizePalaceTotems) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeLoreTablets) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (RandomizeLifebloodCocoons) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            /*if (RandomizeGrimmkinFlames) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;*/
            if (DuplicateMajorItems) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (ShadeSkips) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (AcidSkips) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (SpikeTunnels) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (MildSkips) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (SpicySkips) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (FireballSkips) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;
            if (DarkRooms) settingsSeed += 1;
            settingsSeed = settingsSeed << 1;

            return settingsSeed;
        }
    }
}
