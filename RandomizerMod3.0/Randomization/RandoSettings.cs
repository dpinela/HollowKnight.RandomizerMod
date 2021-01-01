using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod.Randomization
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

        internal bool GetRandomizeByPool(string pool)
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
    }
}
