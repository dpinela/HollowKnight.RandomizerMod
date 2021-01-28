using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using RandomizerLib.MultiWorld;

namespace RandomizerLib
{
    [Serializable]
    public class RandoResult
    {
        public int playerId;
        public int players;
        public int randoId;

        public RandoSettings settings;
        public List<string> startItems;
        public Dictionary<MWItem, MWItem> itemPlacements;       // item -> location
        public Dictionary<MWItem, int> locationOrder;           // location -> index
        public Dictionary<string, string> transitionPlacements; // transition -> transition
        public Dictionary<MWItem, int> shopCosts;               // MWItem (in shop) -> shop cost
        public Dictionary<MWItem, int> variableCosts;           // grub/essence location -> randomized grub/essence count
        public List<string> nicknames;
        public string spoiler;

        public RandoResult()
        {
            playerId = 0;
            players = 1;
            settings = new RandoSettings();
            startItems = new List<string>();
            itemPlacements = new Dictionary<MWItem, MWItem>();
            locationOrder = new Dictionary<MWItem, int>();
            transitionPlacements = new Dictionary<string, string>();
            shopCosts = new Dictionary<MWItem, int>();
            variableCosts = new Dictionary<MWItem, int>();
            spoiler = "";
        }
    }
}
