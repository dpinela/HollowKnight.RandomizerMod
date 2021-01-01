using System;
using System.Collections.Generic;
using static RandomizerLib.PreRandomizer;
using static RandomizerLib.Logging.LogHelper;

namespace RandomizerLib.MultiWorld
{
    public class MWRandomizer
    {
        private List<RandoSettings> settings;
        private Random rand;
        private int players;

        private MWItemManager im;
        private List<Dictionary<string, string>> transitionPlacements;

        private List<Dictionary<string, int>> modifiedCosts;
        private List<List<string>> startProgression;
        private List<List<string>> startItems;

        public MWRandomizer(List<RandoSettings> settings)
        {
            this.settings = settings;
            rand = new Random(settings[0].Seed);
            players = settings.Count;
        }

        public MWRandomizer(RandoSettings settings, int players)
        {
            this.settings = new List<RandoSettings>();
            for (int i = 0; i < players; i++)
            {
                this.settings.Add(settings);
            }
            rand = new Random(settings.Seed);
            this.players = players;
        }

        public void RandomizeMW()
        {
            transitionPlacements = new List<Dictionary<string, string>>();

            modifiedCosts = new List<Dictionary<string, int>>();
            startProgression = new List<List<string>>();
            startItems = new List<List<string>>();

            bool randoSuccess = false;
            while (!randoSuccess)
            {
                modifiedCosts.Clear();
                startProgression.Clear();
                startItems.Clear();

                try
                {
                    for (int i = 0; i < players; i++)
                    {
                        modifiedCosts.Add(RandomizeNonShopCosts(rand, settings[i]));
                        (List<string> playerStartItems, List<string> playerStartProgression) = RandomizeStartingItems(rand, settings[i]);

                        startItems.Add(playerStartItems);
                        startProgression.Add(playerStartProgression);

                        string playerStartName = RandomizeStartingLocation(rand, settings[i], playerStartProgression);

                        if (settings[i].RandomizeTransitions)
                        {
                            Log("Starting transition randomization for player " + i);
                            transitionPlacements.Add(TransitionRandomizer.RandomizeTransitions(settings[i], rand, playerStartName, playerStartItems, playerStartProgression).transitionPlacements);
                        }
                    }
                    MWRandomizeItems();

                    randoSuccess = true;
                }
                catch (RandomizationError) { }
            }

            //PostRandomizationTasks(ims[playerId], tms[playerId], "", startItems[playerId], modifiedCosts[playerId]);
            //RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);
        }

        private void MWRandomizeItems()
        {
            im = new MWItemManager(players, transitionPlacements, rand, settings, startItems, startProgression, modifiedCosts);

            while (im.anyItems && im.anyLocations)
            {
                im.PlaceItem(im.NextItem(), im.NextLocation());
            }

            if (im.anyLocations) throw new RandomizationError();

            while (im.anyItems)
            {
                im.PlaceItem(im.NextItem(), new List<MWItem>(im.shopItems.Keys)[rand.Next(im.shopItems.Keys.Count)]);
            }

            Log(im.nonShopItems);
            Log(im.shopItems);
        }
    }
}
