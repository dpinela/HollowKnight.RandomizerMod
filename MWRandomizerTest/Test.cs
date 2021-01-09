using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RandomizerLib;
using RandomizerLib.MultiWorld;
using Modding;

namespace MWRandomizerTest
{
    class Test
    {
        static void Main(string[] args)
        {
            LogicManager.ParseXML();

            RandoSettings settings = new RandoSettings();
            settings.Seed = 886399437;
            settings.RandomizeRooms = true;
            settings.Cursed = false;
            settings.CreateSpoilerLog = true;
            
            settings.RandomizeStartLocation = true;
            settings.RandomizeStartItems = false;

            settings.MildSkips = false;
            settings.ShadeSkips = false;
            settings.FireballSkips = false;
            settings.AcidSkips = false;
            settings.SpikeTunnels = false;
            settings.DarkRooms = false;
            settings.SpicySkips = false;

            settings.RandomizeDreamers = true;
            settings.RandomizeSkills = true;
            settings.RandomizeCharms = true;
            settings.RandomizeKeys = true;
            settings.RandomizeGeoChests = true;
            settings.RandomizeMaskShards = true;
            settings.RandomizeVesselFragments = true;
            settings.RandomizePaleOre = true;
            settings.RandomizeCharmNotches = true;
            settings.RandomizeRancidEggs = true;
            settings.RandomizeRelics = true;
            settings.RandomizeStags = true;
            settings.RandomizeMaps = true;
            settings.RandomizeGrubs = true;
            settings.RandomizeWhisperingRoots = true;
            settings.RandomizeRocks = true;
            settings.RandomizeSoulTotems = true;
            settings.RandomizePalaceTotems = true;
            settings.RandomizeLifebloodCocoons = true;
            settings.DuplicateMajorItems = true;

            settings.Grubfather = true;
            settings.CharmNotch = true;
            settings.EarlyGeo = true;
            settings.ExtraPlatforms = true;
            settings.LeverSkips = true;
            settings.Jiji = false;

            MWRandomizer rando = new MWRandomizer(settings, 2);

            List<RandoResult> results = rando.RandomizeMW();
            Console.WriteLine(JsonConvert.SerializeObject(results[0]));

            Console.Read();
        }
    }
}
