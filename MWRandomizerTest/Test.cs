using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RandomizerLib;
using RandomizerLib.MultiWorld;

namespace MWRandomizerTest
{
    class Test
    {
        static void Main(string[] args)
        {
            LogicManager.ParseXML();

            RandoSettings settings = new RandoSettings();
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
            settings.RandomizeVesselFragments = true;

            MWRandomizer rando = new MWRandomizer(settings, 2);

            List<RandoResult> results = rando.RandomizeMW();
            Console.WriteLine(JsonConvert.SerializeObject(results[0]));
            Console.WriteLine(JsonConvert.SerializeObject(results[1]));

            Console.Read();
        }
    }
}
