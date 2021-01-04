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

            /*RandoSettings settings = new RandoSettings();
            //settings.RandomizeRooms = true;
            settings.RandomizeCharmNotches = true;
            settings.RandomizeCharms = true;
            settings.RandomizeDreamers = true;
            settings.RandomizeGeoChests = true;
            settings.RandomizeKeys = true;
            settings.RandomizeMaskShards = true;
            settings.RandomizePaleOre = true;
            settings.RandomizeRancidEggs = true;
            settings.RandomizeRelics = true;
            settings.RandomizeSkills = true;
            settings.RandomizeStags = true;
            settings.RandomizeVesselFragments = true;*/
            RandoSettings settings = JsonConvert.DeserializeObject<RandoSettings>("{\"AllBosses\":false,\"AllSkills\":false,\"AllCharms\":false,\"CharmNotch\":true,\"Grubfather\":true,\"Jiji\":false,\"Quirrel\":false,\"ItemDepthHints\":false,\"EarlyGeo\":true,\"LeverSkips\":true,\"ExtraPlatforms\":true,\"RandomizeAreas\":false,\"RandomizeRooms\":true,\"ConnectAreas\":false,\"RandomizeDreamers\":true,\"RandomizeSkills\":true,\"RandomizeCharms\":true,\"RandomizeKeys\":true,\"RandomizeGeoChests\":true,\"RandomizeMaskShards\":true,\"RandomizeVesselFragments\":true,\"RandomizeCharmNotches\":true,\"RandomizePaleOre\":true,\"RandomizeRancidEggs\":true,\"RandomizeRelics\":true,\"RandomizeMaps\":true,\"RandomizeStags\":true,\"RandomizeGrubs\":true,\"RandomizeWhisperingRoots\":true,\"RandomizeRocks\":true,\"RandomizeSoulTotems\":true,\"RandomizePalaceTotems\":true,\"RandomizeLoreTablets\":false,\"RandomizeLifebloodCocoons\":true,\"StartName\":\"King's Pass\",\"DuplicateMajorItems\":true,\"CreateSpoilerLog\":true,\"Cursed\":false,\"RandomizeStartItems\":false,\"RandomizeStartLocation\":true,\"ShadeSkips\":false,\"AcidSkips\":false,\"SpikeTunnels\":false,\"MildSkips\":false,\"SpicySkips\":false,\"FireballSkips\":false,\"DarkRooms\":false,\"Seed\":978317640,\"RandomizeTransitions\":true}");

            MWRandomizer rando = new MWRandomizer(settings, 2);

            List<RandoResult> results = rando.RandomizeMW();
            JsonConvert.DeserializeObject<RandoResult>(JsonConvert.SerializeObject(results[0]));

            Console.Read();
        }
    }
}
