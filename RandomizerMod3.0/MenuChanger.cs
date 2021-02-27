using System;
using RandomizerMod.Extensions;
using SereCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static RandomizerMod.LogHelper;
using Object = UnityEngine.Object;
using Random = System.Random;
using RandomizerMod.Randomization;

using RandomizerLib;
using System.Net;

using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions.Messages;

namespace RandomizerMod
{
    internal static class MenuChanger
    {
        private static readonly Color TRUE_COLOR = Color.Lerp(Color.white, Color.yellow, 0.5f);
        private static readonly Color FALSE_COLOR = Color.grey;
        private static readonly Color LOCKED_TRUE_COLOR = Color.Lerp(Color.grey, Color.yellow, 0.5f);
        private static readonly Color LOCKED_FALSE_COLOR = Color.Lerp(Color.grey, Color.black, 0.5f);

        public static void EditUI()
        {
            // Reset settings
            RandomizerMod.Instance.Settings = new SaveSettings();

            // Fetch data from vanilla screen
            MenuScreen playScreen = Ref.UI.playModeMenuScreen;

            playScreen.title.gameObject.transform.localPosition = new Vector3(0, 520.56f);

            Object.Destroy(playScreen.topFleur.gameObject);

            MenuButton classic = (MenuButton)playScreen.defaultHighlight;
            MenuButton steel = (MenuButton)classic.FindSelectableOnDown();
            MenuButton back = (MenuButton)steel.FindSelectableOnDown();

            GameObject parent = steel.transform.parent.gameObject;

            Object.Destroy(parent.GetComponent<VerticalLayoutGroup>());

            // Create new buttons
            MenuButton startRandoBtn = classic.Clone("StartRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, 0), "Start Game", "Randomizer", RandomizerMod.GetSprite("UI.logo"));
            
            /*
            MenuButton startNormalBtn = classic.Clone("StartNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, -200), "Start Game", "Non-Randomizer");
            MenuButton startSteelRandoBtn = steel.Clone("StartSteelRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Randomizer", RandomizerMod.GetSprite("UI.logo2"));
            MenuButton startSteelNormalBtn = steel.Clone("StartSteelNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Non-Randomizer");
                
            startNormalBtn.transform.localScale = 
                startSteelNormalBtn.transform.localScale =
                    startSteelRandoBtn.transform.localScale = */
            startRandoBtn.transform.localScale = new Vector2(0.75f, 0.75f);
            Object.Destroy(startRandoBtn.GetComponent<StartGameEventTrigger>());
            MenuButton backBtn = back.Clone("Back", MenuButton.MenuButtonType.Proceed, new Vector2(0, -100), "Back");

            //RandoMenuItem<string> gameTypeBtn = new RandoMenuItem<string>(back, new Vector2(0, 600), "Game Type", "Normal", "Steel Soul");

            RandoMenuItem<string> presetPoolsBtn = new RandoMenuItem<string>(back, new Vector2(900, 1120), "Preset", "Mini Super Junk Pit", "Basic", "Completionist", "Junk Pit", "Super Junk Pit", "Mini Super Geo Pit", "Super Geo Pit", "Mini Super Totem Pit", "Super Totem Pit", "EVERYTHING", "Vanilla", "Custom");
            RandoMenuItem<bool> RandoDreamersBtn = new RandoMenuItem<bool>(back, new Vector2(700, 1040), "Dreamers", true, false);
            RandoMenuItem<bool> RandoSkillsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 1040), "Skills", true, false);
            RandoMenuItem<bool> RandoCharmsBtn = new RandoMenuItem<bool>(back, new Vector2(700, 960), "Charms", true, false);
            RandoMenuItem<bool> RandoKeysBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 960), "Keys", true, false);
            RandoMenuItem<bool> RandoGeoChestsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 880), "Geo Chests", true, false);
            RandoMenuItem<bool> RandoMaskBtn = new RandoMenuItem<bool>(back, new Vector2(700, 800), "Mask Shards", true, false);
            RandoMenuItem<bool> RandoVesselBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 800), "Vessel Fragments", true, false);
            RandoMenuItem<bool> RandoOreBtn = new RandoMenuItem<bool>(back, new Vector2(700, 720), "Pale Ore", true, false);
            RandoMenuItem<bool> RandoNotchBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 720), "Charm Notches", true, false);
            RandoMenuItem<bool> RandoEggBtn = new RandoMenuItem<bool>(back, new Vector2(700, 640), "Rancid Eggs", true, false);
            RandoMenuItem<bool> RandoRelicsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 640), "Relics", true, false);
            RandoMenuItem<bool> RandoMapBtn = new RandoMenuItem<bool>(back, new Vector2(700, 560), "Maps", false, true);
            RandoMenuItem<bool> RandoStagBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 560), "Stags", true, false);
            RandoMenuItem<bool> RandoGrubBtn = new RandoMenuItem<bool>(back, new Vector2(700, 480), "Grubs", false, true);
            RandoMenuItem<bool> RandoRootsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 480), "Whispering Roots", false, true);
            RandoMenuItem<bool> RandoGeoRocksBtn = new RandoMenuItem<bool>(back, new Vector2(700, 400), "Geo Rocks", false, true);
            RandoMenuItem<bool> RandoCocoonsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 400), "Lifeblood Cocoons", false, true);
            RandoMenuItem<bool> RandoSoulTotemsBtn = new RandoMenuItem<bool>(back, new Vector2(700, 320), "Soul Totems", false, true);
            RandoMenuItem<bool> RandoPalaceTotemsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 320), "Palace Totems", false, true);
            //RandoMenuItem<bool> RandoLoreTabletsBtn = new RandoMenuItem<bool>(back, new Vector2(1100, 320), "Lore Tablets", false, true);
            RandoMenuItem<bool> DuplicateBtn = new RandoMenuItem<bool>(back, new Vector2(900, 240), "Duplicate Major Items", true, false);

            RandoMenuItem<bool> RandoStartItemsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 80), "Randomize Start Items", false, true);
            RandoMenuItem<string> RandoStartLocationsModeBtn = new RandoMenuItem<string>(back, new Vector2(900, 0), "Start Location Setting", "Select", "Random");
            RandoMenuItem<string> StartLocationsListBtn = new RandoMenuItem<string>(back, new Vector2(900, -80), "Start Location", LogicManager.StartLocations);

            RandoMenuItem<string> presetSkipsBtn = new RandoMenuItem<string>(back, new Vector2(-900, 1040), "Preset", "Easy", "Medium", "Hard", "Custom");
            RandoMenuItem<bool> mildSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 960), "Mild Skips", false, true);
            RandoMenuItem<bool> shadeSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 880), "Shade Skips", false, true);
            RandoMenuItem<bool> fireballSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 800), "Fireball Skips", false, true);
            RandoMenuItem<bool> acidSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 720), "Acid Skips", false, true);
            RandoMenuItem<bool> spikeTunnelsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 640), "Spike Tunnels", false, true);
            RandoMenuItem<bool> darkRoomsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 560), "Dark Rooms", false, true);
            RandoMenuItem<bool> spicySkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 480), "Spicy Skips", false, true);

            RandoMenuItem<bool> charmNotchBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 300), "Salubra Notches", true, false);
            RandoMenuItem<bool> grubfatherBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 220), "Fast Grubfather", true, false);
            RandoMenuItem<bool> EarlyGeoBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 140), "Early Geo", true, false);
            RandoMenuItem<bool> softlockBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 60), "Extra Platforms", true, false);
            RandoMenuItem<bool> leverBtn = new RandoMenuItem<bool>(back, new Vector2(-900, -20), "1.2.2.1 Levers", true, false);
            RandoMenuItem<bool> jijiBtn = new RandoMenuItem<bool>(back, new Vector2(-900, -100), "Jiji Hints", false, true);

            RandoMenuItem<string> modeBtn = new RandoMenuItem<string>(back, new Vector2(0, 1040), "Mode", "Item Randomizer", "Area Randomizer", "Connected-Area Room Randomizer", "Room Randomizer");
            RandoMenuItem<string> cursedBtn = new RandoMenuItem<string>(back, new Vector2(0, 960), "Cursed", "no", "noo", "noooo", "noooooooo", "noooooooooooooooo", "Oh yeah");
            RandoMenuItem<string> multiworldBtn = new RandoMenuItem<string>(back, new Vector2(0, 800), "Multiworld", "No", "Yes");
            RandoMenuItem<bool> multiworldReadyBtn = new RandoMenuItem<bool>(back, new Vector2(0, 600), "Ready", false, true);
            GameObject readyPlayers = CreateLabel(back, new Vector2(-0, 540), "");
            readyPlayers.transform.localScale = new Vector3(0.5f, 0.5f);
            multiworldReadyBtn.Button.gameObject.SetActive(false);
            RandoMenuItem<bool> RandoSpoilerBtn = new RandoMenuItem<bool>(back, new Vector2(0, 0), "Create Spoiler Log", true, false);

            // Used to show how many players readied up "Ready (1)"
            void UpdateReady(int num, string players)
            {
                if (multiworldReadyBtn.CurrentSelection)
                {
                    multiworldReadyBtn.SetName($"Ready ({num})");
                    readyPlayers.transform.Find("Text").GetComponent<Text>().text = players;

                    if (num == -1)
                    {
                        startRandoBtn.gameObject.SetActive(false);
                    }
                }
            }

            // NGL i don't know what this does i just copied it and put it in a function
            InputField createTextEntry()
            {
                // Create seed entry field
                GameObject gameObject = back.Clone("entry", MenuButton.MenuButtonType.Activate, new Vector2(0, 1130)).gameObject;
                Object.DestroyImmediate(gameObject.GetComponent<MenuButton>());
                Object.DestroyImmediate(gameObject.GetComponent<EventTrigger>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<AutoLocalizeTextUI>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<FixVerticalAlign>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<ContentSizeFitter>());

                RectTransform rect = gameObject.transform.Find("Text").GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(337, 63.2f);

                InputField input = gameObject.AddComponent<InputField>();
                input.textComponent = gameObject.transform.Find("Text").GetComponent<Text>();

                input.colors = new ColorBlock
                {
                    highlightedColor = Color.yellow,
                    pressedColor = Color.red,
                    disabledColor = Color.black,
                    normalColor = Color.white,
                    colorMultiplier = 2f
                };

                return input;
            }

            InputField seedInput = createTextEntry();

            RandomizerMod.Instance.Settings.Seed = new Random().Next(999999999);
            seedInput.text = RandomizerMod.Instance.Settings.Seed.ToString();

            seedInput.transform.localPosition = new Vector3(0, 1240);
            seedInput.caretColor = Color.white;
            seedInput.contentType = InputField.ContentType.IntegerNumber;
            seedInput.onEndEdit.AddListener(ParseSeedInput);
            seedInput.navigation = Navigation.defaultNavigation;
            seedInput.caretWidth = 8;
            seedInput.characterLimit = 9;

            InputField urlInput = createTextEntry();
            urlInput.transform.localPosition = new Vector3(0, 860);
            urlInput.text = RandomizerMod.Instance.MWSettings.URL;
            urlInput.textComponent.fontSize = urlInput.textComponent.fontSize - 5;

            urlInput.caretColor = Color.white;
            urlInput.contentType = InputField.ContentType.Standard;
            urlInput.navigation = Navigation.defaultNavigation;
            urlInput.caretWidth = 8;
            urlInput.characterLimit = 0;

            InputField nicknameInput = createTextEntry();
            nicknameInput.transform.localPosition = new Vector3(0, 720);
            nicknameInput.text = RandomizerMod.Instance.MWSettings.UserName;
            nicknameInput.textComponent.fontSize = nicknameInput.textComponent.fontSize - 5;

            nicknameInput.caretColor = Color.white;
            nicknameInput.contentType = InputField.ContentType.Standard;
            nicknameInput.onEndEdit.AddListener(ChangeNickname);
            nicknameInput.navigation = Navigation.defaultNavigation;
            nicknameInput.caretWidth = 8;
            nicknameInput.characterLimit = 15;

            nicknameInput.gameObject.SetActive(false);

            InputField roomInput = createTextEntry();
            roomInput.transform.localPosition = new Vector3(0, 660);
            roomInput.text = "";
            roomInput.textComponent.fontSize = roomInput.textComponent.fontSize - 5;

            roomInput.caretColor = Color.white;
            roomInput.contentType = InputField.ContentType.Standard;
            roomInput.navigation = Navigation.defaultNavigation;
            roomInput.caretWidth = 8;
            roomInput.characterLimit = 15;

            roomInput.gameObject.SetActive(false);

            // Create some labels
            CreateLabel(back, new Vector2(-900, 1130), "Required Skips");
            CreateLabel(back, new Vector2(-900, 380), "Quality of Life");
            CreateLabel(back, new Vector2(900, 1210), "Randomization");
            CreateLabel(back, new Vector2(900, 160), "Open Mode");
            CreateLabel(back, new Vector2(0, 200), "Use of Benchwarp mod may be required");
            CreateLabel(back, new Vector2(0, 1300), "Seed:");
            GameObject urlLabel = CreateLabel(back, new Vector2(-200, 865), "URL:");
            urlLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            GameObject nicknameLabel = CreateLabel(back, new Vector2(-300, 725), "Nickname:");
            nicknameLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            nicknameLabel.SetActive(false);
            GameObject roomLabel = CreateLabel(back, new Vector2(-300, 665), "Room:");
            roomLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            roomLabel.SetActive(false);

            // We don't need these old buttons anymore
            Object.Destroy(classic.gameObject);
            Object.Destroy(steel.gameObject);
            Object.Destroy(parent.FindGameObjectInChildren("GGButton"));
            Object.Destroy(back.gameObject);

            // Gotta put something here, we destroyed the old default
            playScreen.defaultHighlight = startRandoBtn;

            // Apply navigation info (up, right, down, left)
            //startNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            startRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            //startSteelNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            //startSteelRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, gameTypeBtn.Button, presetSkipsBtn.Button);
            modeBtn.Button.SetNavigation(backBtn, modeBtn.Button, startRandoBtn, modeBtn.Button);
            //gameTypeBtn.Button.SetNavigation(startRandoBtn, presetPoolsBtn.Button, startRandoBtn, presetSkipsBtn.Button);
            backBtn.SetNavigation(startRandoBtn, backBtn, modeBtn.Button, backBtn);
            RandoSpoilerBtn.Button.SetNavigation(RandoRelicsBtn.Button, RandoSpoilerBtn.Button, presetPoolsBtn.Button, startRandoBtn);

            presetSkipsBtn.Button.SetNavigation(leverBtn.Button, startRandoBtn, shadeSkipsBtn.Button, presetSkipsBtn.Button);
            mildSkipsBtn.Button.SetNavigation(presetSkipsBtn.Button, startRandoBtn, mildSkipsBtn.Button, mildSkipsBtn.Button);
            shadeSkipsBtn.Button.SetNavigation(mildSkipsBtn.Button, startRandoBtn, fireballSkipsBtn.Button, shadeSkipsBtn.Button);
            fireballSkipsBtn.Button.SetNavigation(shadeSkipsBtn.Button, startRandoBtn, acidSkipsBtn.Button, fireballSkipsBtn.Button);
            acidSkipsBtn.Button.SetNavigation(fireballSkipsBtn.Button, startRandoBtn, spikeTunnelsBtn.Button, acidSkipsBtn.Button);
            spikeTunnelsBtn.Button.SetNavigation(acidSkipsBtn.Button, startRandoBtn, darkRoomsBtn.Button, spikeTunnelsBtn.Button);
            darkRoomsBtn.Button.SetNavigation(spikeTunnelsBtn.Button, startRandoBtn, spicySkipsBtn.Button, darkRoomsBtn.Button);
            spicySkipsBtn.Button.SetNavigation(darkRoomsBtn.Button, startRandoBtn, charmNotchBtn.Button, spicySkipsBtn.Button);

            charmNotchBtn.Button.SetNavigation(spicySkipsBtn.Button, startRandoBtn, grubfatherBtn.Button, charmNotchBtn.Button);
            grubfatherBtn.Button.SetNavigation(charmNotchBtn.Button, startRandoBtn, EarlyGeoBtn.Button, grubfatherBtn.Button);
            EarlyGeoBtn.Button.SetNavigation(grubfatherBtn.Button, startRandoBtn, softlockBtn.Button, EarlyGeoBtn.Button);
            softlockBtn.Button.SetNavigation(EarlyGeoBtn.Button, startRandoBtn, leverBtn.Button, softlockBtn.Button);
            leverBtn.Button.SetNavigation(softlockBtn.Button, startRandoBtn, jijiBtn.Button, leverBtn.Button);
            jijiBtn.Button.SetNavigation(leverBtn.Button, startRandoBtn, presetSkipsBtn.Button, jijiBtn.Button);

            presetPoolsBtn.Button.SetNavigation(RandoSpoilerBtn.Button, presetPoolsBtn.Button, RandoDreamersBtn.Button, startRandoBtn);
            RandoDreamersBtn.Button.SetNavigation(presetPoolsBtn.Button, RandoSkillsBtn.Button, RandoCharmsBtn.Button, startRandoBtn);
            RandoSkillsBtn.Button.SetNavigation(presetPoolsBtn.Button, RandoSkillsBtn.Button, RandoKeysBtn.Button, RandoDreamersBtn.Button);
            RandoCharmsBtn.Button.SetNavigation(RandoDreamersBtn.Button, RandoKeysBtn.Button, RandoGeoChestsBtn.Button, startRandoBtn);
            RandoKeysBtn.Button.SetNavigation(RandoSkillsBtn.Button, RandoKeysBtn.Button, RandoGeoChestsBtn.Button, RandoCharmsBtn.Button);
            RandoGeoChestsBtn.Button.SetNavigation(RandoCharmsBtn.Button, RandoGeoChestsBtn.Button, RandoMaskBtn.Button, startRandoBtn);
            RandoMaskBtn.Button.SetNavigation(RandoGeoChestsBtn.Button, RandoVesselBtn.Button, RandoOreBtn.Button, startRandoBtn);
            RandoVesselBtn.Button.SetNavigation(RandoGeoChestsBtn.Button, RandoVesselBtn.Button, RandoNotchBtn.Button, RandoMaskBtn.Button);
            RandoOreBtn.Button.SetNavigation(RandoMaskBtn.Button, RandoNotchBtn.Button, RandoEggBtn.Button, startRandoBtn);
            RandoNotchBtn.Button.SetNavigation(RandoVesselBtn.Button, RandoNotchBtn.Button, RandoRelicsBtn.Button, RandoOreBtn.Button);
            RandoEggBtn.Button.SetNavigation(RandoOreBtn.Button, RandoRelicsBtn.Button, RandoMapBtn.Button, startRandoBtn);
            RandoRelicsBtn.Button.SetNavigation(RandoNotchBtn.Button, RandoRelicsBtn.Button, RandoStagBtn.Button, RandoEggBtn.Button);
            RandoMapBtn.Button.SetNavigation(RandoEggBtn.Button, RandoStagBtn.Button, RandoGrubBtn.Button, startRandoBtn);
            RandoStagBtn.Button.SetNavigation(RandoRelicsBtn.Button, RandoStagBtn.Button, RandoRootsBtn.Button, RandoMapBtn.Button);
            RandoGrubBtn.Button.SetNavigation(RandoMapBtn.Button, RandoRootsBtn.Button, RandoGeoRocksBtn.Button, startRandoBtn);
            RandoRootsBtn.Button.SetNavigation(RandoStagBtn.Button, RandoRootsBtn.Button, RandoCocoonsBtn.Button, RandoGrubBtn.Button);
            RandoGeoRocksBtn.Button.SetNavigation(RandoGrubBtn.Button, RandoCocoonsBtn.Button, RandoSoulTotemsBtn.Button, startRandoBtn);
            RandoCocoonsBtn.Button.SetNavigation(RandoRootsBtn.Button, RandoCocoonsBtn.Button, RandoPalaceTotemsBtn.Button, RandoGeoRocksBtn.Button);
            RandoSoulTotemsBtn.Button.SetNavigation(RandoGeoRocksBtn.Button, RandoPalaceTotemsBtn.Button, DuplicateBtn.Button, startRandoBtn);
            RandoPalaceTotemsBtn.Button.SetNavigation(RandoGeoRocksBtn.Button, RandoPalaceTotemsBtn.Button, DuplicateBtn.Button, RandoSoulTotemsBtn.Button);
            DuplicateBtn.Button.SetNavigation(RandoSoulTotemsBtn.Button, DuplicateBtn.Button, RandoStartItemsBtn.Button, startRandoBtn);

            RandoStartItemsBtn.Button.SetNavigation(DuplicateBtn.Button, RandoStartItemsBtn.Button, RandoStartLocationsModeBtn.Button, startRandoBtn);
            RandoStartLocationsModeBtn.Button.SetNavigation(RandoStartItemsBtn.Button, RandoStartLocationsModeBtn.Button, StartLocationsListBtn.Button, startRandoBtn);
            StartLocationsListBtn.Button.SetNavigation(RandoStartLocationsModeBtn.Button, RandoStartLocationsModeBtn.Button, StartLocationsListBtn.Button, startRandoBtn);

            void CopySettings(bool rando)
            {
                RandomizerMod.Instance.Settings.CharmNotch = charmNotchBtn.CurrentSelection;
                RandomizerMod.Instance.Settings.Grubfather = grubfatherBtn.CurrentSelection;
                RandomizerMod.Instance.Settings.EarlyGeo = EarlyGeoBtn.CurrentSelection;


                if (rando)
                {
                    RandomizerMod.Instance.Settings.Jiji = jijiBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.Quirrel = false;
                    RandomizerMod.Instance.Settings.ItemDepthHints = false;
                    RandomizerMod.Instance.Settings.LeverSkips = leverBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.ExtraPlatforms = softlockBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.RandomizeDreamers = RandoDreamersBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeSkills = RandoSkillsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeCharms = RandoCharmsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeKeys = RandoKeysBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeGeoChests = RandoGeoChestsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeMaskShards = RandoMaskBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeVesselFragments = RandoVesselBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizePaleOre = RandoOreBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeCharmNotches = RandoNotchBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRancidEggs = RandoEggBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRelics = RandoRelicsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeMaps = RandoMapBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeStags = RandoStagBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeGrubs = RandoGrubBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeLifebloodCocoons = RandoCocoonsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeWhisperingRoots = RandoRootsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRocks = RandoGeoRocksBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeSoulTotems = RandoSoulTotemsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizePalaceTotems = RandoPalaceTotemsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.DuplicateMajorItems = DuplicateBtn.CurrentSelection;

                    ParseSeedInput(seedInput.textComponent.text);
                    RandomizerMod.Instance.Settings.CreateSpoilerLog = RandoSpoilerBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.Cursed = cursedBtn.CurrentSelection.StartsWith("O");

                    RandomizerMod.Instance.Settings.Randomizer = rando;
                    RandomizerMod.Instance.Settings.RandomizeAreas = modeBtn.CurrentSelection == "Area Randomizer";
                    RandomizerMod.Instance.Settings.RandomizeRooms = modeBtn.CurrentSelection.EndsWith("Room Randomizer");
                    RandomizerMod.Instance.Settings.ConnectAreas = modeBtn.CurrentSelection.StartsWith("Connected-Area");

                    RandomizerMod.Instance.Settings.MildSkips = mildSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.ShadeSkips = shadeSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.FireballSkips = fireballSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.AcidSkips = acidSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.SpikeTunnels = spikeTunnelsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.DarkRooms = darkRoomsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.SpicySkips = spicySkipsBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.RandomizeStartItems = RandoStartItemsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeStartLocation = RandoStartLocationsModeBtn.CurrentSelection == "Random";
                    RandomizerMod.Instance.Settings.StartName = StartLocationsListBtn.GetColor() == Color.red ? "King's Pass" : StartLocationsListBtn.CurrentSelection;
                }
            }

            void UpdateSkipsButtons(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Easy":
                        SetShadeSkips(false);
                        mildSkipsBtn.SetSelection(false);
                        acidSkipsBtn.SetSelection(false);
                        spikeTunnelsBtn.SetSelection(false);
                        spicySkipsBtn.SetSelection(false);
                        fireballSkipsBtn.SetSelection(false);
                        darkRoomsBtn.SetSelection(false);
                        break;
                    case "Medium":
                        SetShadeSkips(true);
                        mildSkipsBtn.SetSelection(true);
                        acidSkipsBtn.SetSelection(false);
                        spikeTunnelsBtn.SetSelection(false);
                        spicySkipsBtn.SetSelection(false);
                        fireballSkipsBtn.SetSelection(false);
                        darkRoomsBtn.SetSelection(false);
                        break;
                    case "Hard":
                        SetShadeSkips(true);
                        mildSkipsBtn.SetSelection(true);
                        acidSkipsBtn.SetSelection(true);
                        spikeTunnelsBtn.SetSelection(true);
                        spicySkipsBtn.SetSelection(true);
                        fireballSkipsBtn.SetSelection(true);
                        darkRoomsBtn.SetSelection(true);
                        break;
                    case "Custom":
                        item.SetSelection("Easy");
                        goto case "Easy";

                    default:
                        LogWarn("Unknown value in preset button: " + item.CurrentSelection);
                        break;
                }
            }
            void UpdatePoolPreset(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Mini Super Junk Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Basic":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(false);
                        RandoMaskBtn.SetSelection(false);
                        RandoVesselBtn.SetSelection(false);
                        RandoOreBtn.SetSelection(false);
                        RandoNotchBtn.SetSelection(false);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(false);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Completionist":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(false);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Junk Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(false);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Super Junk Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Mini Super Geo Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(true);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Super Geo Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(true);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Mini Super Totem Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceTotemsBtn.SetSelection(true);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Super Totem Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceTotemsBtn.SetSelection(true);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "EVERYTHING":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(true);
                        RandoCocoonsBtn.SetSelection(true);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceTotemsBtn.SetSelection(true);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Vanilla":
                        RandoDreamersBtn.SetSelection(false);
                        RandoSkillsBtn.SetSelection(false);
                        RandoCharmsBtn.SetSelection(false);
                        RandoKeysBtn.SetSelection(false);
                        RandoGeoChestsBtn.SetSelection(false);
                        RandoMaskBtn.SetSelection(false);
                        RandoVesselBtn.SetSelection(false);
                        RandoOreBtn.SetSelection(false);
                        RandoNotchBtn.SetSelection(false);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(false);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceTotemsBtn.SetSelection(false);
                        //RandoLoreTabletsBtn.SetSelection(false);
                        break;
                    case "Custom":
                        item.SetSelection("Mini Super Junk Pit");
                        goto case "Mini Super Junk Pit";
                }
            }

            void UpdateStartLocationColor()
            {
                if (RandoStartLocationsModeBtn.CurrentSelection == "Random")
                {
                    StartLocationsListBtn.SetSelection("King's Pass");
                    StartLocationsListBtn.Lock();
                    StartLocationsListBtn.SetColor(LOCKED_FALSE_COLOR);
                    return;
                }
                else StartLocationsListBtn.Unlock();

                // cf. TestStartLocation in PreRandomizer. Note that color is checked in StartGame to determine if a selected start was valid
                if (LogicManager.GetStartLocation(StartLocationsListBtn.CurrentSelection) is StartDef startDef)
                {
                    if (RandoStartItemsBtn.CurrentSelection)
                    {
                        StartLocationsListBtn.SetColor(Color.white);
                    }
                    else if (modeBtn.CurrentSelection.EndsWith("Room Randomizer"))
                    {
                        if (startDef.roomSafe)
                        {
                            StartLocationsListBtn.SetColor(Color.white);
                        }
                        else StartLocationsListBtn.SetColor(Color.red);
                    }
                    else if (modeBtn.CurrentSelection == "Area Randomizer")
                    {
                        if (startDef.areaSafe)
                        {
                            StartLocationsListBtn.SetColor(Color.white);
                        }
                        else StartLocationsListBtn.SetColor(Color.red);
                    }
                    else if (startDef.itemSafe)
                    {
                        StartLocationsListBtn.SetColor(Color.white);
                    }
                    else StartLocationsListBtn.SetColor(Color.red);
                }
            }

            void HandleProgressionLock()
            {
                if (RandoStartItemsBtn.CurrentSelection)
                {
                    RandoDreamersBtn.SetSelection(true);
                    RandoSkillsBtn.SetSelection(true);
                    RandoCharmsBtn.SetSelection(true);
                    RandoKeysBtn.SetSelection(true);
                    RandoDreamersBtn.Lock();
                    RandoSkillsBtn.Lock();
                    RandoCharmsBtn.Lock();
                    RandoKeysBtn.Lock();
                }
                else if (DuplicateBtn.CurrentSelection)
                {
                    RandoDreamersBtn.SetSelection(true);
                    RandoSkillsBtn.SetSelection(true);
                    RandoCharmsBtn.SetSelection(true);
                    RandoKeysBtn.SetSelection(true);
                    RandoDreamersBtn.Lock();
                    RandoSkillsBtn.Lock();
                    RandoCharmsBtn.Lock();
                    RandoKeysBtn.Lock();
                }
                else
                {
                    RandoDreamersBtn.Unlock();
                    RandoSkillsBtn.Unlock();
                    RandoCharmsBtn.Unlock();
                    RandoKeysBtn.Unlock();
                }
            }
            HandleProgressionLock(); // call it because duplicates are on by default

            void LockAll()
            {
                seedInput.enabled = false;
                nicknameInput.enabled = false;
                roomInput.enabled = false;

                presetPoolsBtn.Lock();
                RandoDreamersBtn.Lock();
                RandoSkillsBtn.Lock();
                RandoCharmsBtn.Lock();
                RandoKeysBtn.Lock();
                RandoGeoChestsBtn.Lock();
                RandoMaskBtn.Lock();
                RandoVesselBtn.Lock();
                RandoOreBtn.Lock();
                RandoNotchBtn.Lock();
                RandoEggBtn.Lock();
                RandoRelicsBtn.Lock();
                RandoMapBtn.Lock();
                RandoStagBtn.Lock();
                RandoGrubBtn.Lock();
                RandoRootsBtn.Lock();
                RandoGeoRocksBtn.Lock();
                RandoCocoonsBtn.Lock();
                RandoSoulTotemsBtn.Lock();
                RandoPalaceTotemsBtn.Lock();
                //RandoLoreTabletsBtn.Lock();
                DuplicateBtn.Lock();

                RandoStartItemsBtn.Lock();
                RandoStartLocationsModeBtn.Lock();
                StartLocationsListBtn.Lock();

                presetSkipsBtn.Lock();
                mildSkipsBtn.Lock();
                shadeSkipsBtn.Lock();
                fireballSkipsBtn.Lock();
                acidSkipsBtn.Lock();
                spikeTunnelsBtn.Lock();
                darkRoomsBtn.Lock();
                spicySkipsBtn.Lock();

                charmNotchBtn.Lock();
                grubfatherBtn.Lock();
                EarlyGeoBtn.Lock();
                softlockBtn.Lock();
                leverBtn.Lock();
                jijiBtn.Lock();

                modeBtn.Lock();
                cursedBtn.Lock();
                RandoSpoilerBtn.Lock();
            }

            void UnlockAll()
            {
                seedInput.enabled = true;
                nicknameInput.enabled = true;
                roomInput.enabled = true;

                presetPoolsBtn.Unlock();
                RandoDreamersBtn.Unlock();
                RandoSkillsBtn.Unlock();
                RandoCharmsBtn.Unlock();
                RandoKeysBtn.Unlock();
                RandoGeoChestsBtn.Unlock();
                RandoMaskBtn.Unlock();
                RandoVesselBtn.Unlock();
                RandoOreBtn.Unlock();
                RandoNotchBtn.Unlock();
                RandoEggBtn.Unlock();
                RandoRelicsBtn.Unlock();
                RandoMapBtn.Unlock();
                RandoStagBtn.Unlock();
                RandoGrubBtn.Unlock();
                RandoRootsBtn.Unlock();
                RandoGeoRocksBtn.Unlock();
                RandoCocoonsBtn.Unlock();
                RandoSoulTotemsBtn.Unlock();
                RandoPalaceTotemsBtn.Unlock();
                //RandoLoreTabletsBtn.Unlock();
                DuplicateBtn.Unlock();

                RandoStartItemsBtn.Unlock();
                RandoStartLocationsModeBtn.Unlock();
                StartLocationsListBtn.Unlock();

                presetSkipsBtn.Unlock();
                mildSkipsBtn.Unlock();
                shadeSkipsBtn.Unlock();
                fireballSkipsBtn.Unlock();
                acidSkipsBtn.Unlock();
                spikeTunnelsBtn.Unlock();
                darkRoomsBtn.Unlock();
                spicySkipsBtn.Unlock();

                charmNotchBtn.Unlock();
                grubfatherBtn.Unlock();
                EarlyGeoBtn.Unlock();
                softlockBtn.Unlock();
                leverBtn.Unlock();
                jijiBtn.Unlock();

                modeBtn.Unlock();
                cursedBtn.Unlock();
                RandoSpoilerBtn.Unlock();

                HandleProgressionLock();
                UpdateStartLocationColor();
            }

            void SetShadeSkips(bool enabled)
            {
                if (enabled)
                {
                    //gameTypeBtn.SetSelection("Normal");
                    //SwitchGameType(false);
                }

                shadeSkipsBtn.SetSelection(enabled);
            }

            void SkipsSettingChanged(RandoMenuItem<bool> item)
            {
                presetSkipsBtn.SetSelection("Custom");

            }

            void PoolSettingChanged(RandoMenuItem<bool> item)
            {
                presetPoolsBtn.SetSelection("Custom");
            }

            void MWChanged(RandoMenuItem<string> item)
            {
                if (item.CurrentSelection == "Yes")
                {
                    try
                    {
                        RandomizerMod.Instance.MWSettings.URL = urlInput.text;
                        Log($"Trying to connect to {urlInput.text}");
                        /*RandomizerMod.Instance.mwConnection.Disconnect();
                        RandomizerMod.Instance.mwConnection = new MultiWorld.ClientConnection();*/
                        RandomizerMod.Instance.mwConnection.Connect();
                        RandomizerMod.Instance.mwConnection.NumReadyReceived += UpdateReady;
                        item.SetSelection("Yes");
                    }
                    catch
                    {
                        Log("Failed to connect!");
                        item.SetSelection("No");
                        return;
                    }

                    startRandoBtn.transform.localPosition = startRandoBtn.transform.localPosition - new Vector3(0, 210);

                    urlLabel.SetActive(false);
                    urlInput.gameObject.SetActive(false);

                    nicknameInput.gameObject.SetActive(true);
                    nicknameLabel.SetActive(true);

                    roomInput.gameObject.SetActive(true);
                    roomLabel.SetActive(true);

                    multiworldReadyBtn.Button.gameObject.SetActive(true);
                    multiworldReadyBtn.SetSelection(false);
                    multiworldReadyBtn.SetName("Ready");

                    readyPlayers.transform.Find("Text").GetComponent<Text>().text = "";

                    startRandoBtn.gameObject.SetActive(false);
                }
                else
                {
                    startRandoBtn.transform.localPosition = startRandoBtn.transform.localPosition + new Vector3(0, 210);

                    urlLabel.SetActive(true);
                    urlInput.gameObject.SetActive(true);

                    nicknameInput.gameObject.SetActive(false);
                    nicknameLabel.SetActive(false);

                    roomInput.gameObject.SetActive(false);
                    roomLabel.SetActive(false);
                    
                    multiworldReadyBtn.SetSelection(false);
                    multiworldReadyBtn.Button.gameObject.SetActive(false);
                    multiworldReadyBtn.SetName("Ready");

                    readyPlayers.transform.Find("Text").GetComponent<Text>().text = "";

                    startRandoBtn.gameObject.SetActive(true);
                    UnlockAll();

                    RandomizerMod.Instance.mwConnection.Disconnect();
                }
            }

            void MWReadyChanged(RandoMenuItem<bool> item)
            {
                if (item.CurrentSelection)
                {
                    LockAll();
                    CopySettings(true);
                    RandomizerMod.Instance.mwConnection.ReadyUp(roomInput.text);
                    startRandoBtn.gameObject.SetActive(true);
                }
                else
                {
                    UnlockAll();
                    RandomizerMod.Instance.mwConnection.Unready();
                    startRandoBtn.gameObject.SetActive(false);
                    multiworldReadyBtn.SetName("Ready");
                    readyPlayers.transform.Find("Text").GetComponent<Text>().text = "";
                }
            }

            modeBtn.Changed += s => HandleProgressionLock();

            presetSkipsBtn.Changed += UpdateSkipsButtons;
            presetPoolsBtn.Changed += UpdatePoolPreset;

            mildSkipsBtn.Changed += SkipsSettingChanged;
            shadeSkipsBtn.Changed += SkipsSettingChanged;
            shadeSkipsBtn.Changed += SaveShadeVal;
            acidSkipsBtn.Changed += SkipsSettingChanged;
            spikeTunnelsBtn.Changed += SkipsSettingChanged;
            spicySkipsBtn.Changed += SkipsSettingChanged;
            fireballSkipsBtn.Changed += SkipsSettingChanged;
            darkRoomsBtn.Changed += SkipsSettingChanged;

            RandoDreamersBtn.Changed += PoolSettingChanged;
            RandoSkillsBtn.Changed += PoolSettingChanged;
            RandoCharmsBtn.Changed += PoolSettingChanged;
            RandoKeysBtn.Changed += PoolSettingChanged;
            RandoGeoChestsBtn.Changed += PoolSettingChanged;
            RandoGeoRocksBtn.Changed += PoolSettingChanged;
            RandoSoulTotemsBtn.Changed += PoolSettingChanged;
            RandoPalaceTotemsBtn.Changed += PoolSettingChanged;
            RandoMaskBtn.Changed += PoolSettingChanged;
            RandoVesselBtn.Changed += PoolSettingChanged;
            RandoOreBtn.Changed += PoolSettingChanged;
            RandoNotchBtn.Changed += PoolSettingChanged;
            RandoEggBtn.Changed += PoolSettingChanged;
            RandoRelicsBtn.Changed += PoolSettingChanged;
            RandoStagBtn.Changed += PoolSettingChanged;
            RandoMapBtn.Changed += PoolSettingChanged;
            RandoGrubBtn.Changed += PoolSettingChanged;
            RandoRootsBtn.Changed += PoolSettingChanged;
            RandoCocoonsBtn.Changed += PoolSettingChanged;
            DuplicateBtn.Changed += s => HandleProgressionLock();

            multiworldBtn.Changed += MWChanged;
            multiworldReadyBtn.Changed += MWReadyChanged;

            RandoStartItemsBtn.Changed += (RandoMenuItem<bool> Item) => UpdateStartLocationColor();
            RandoStartItemsBtn.Changed += s => HandleProgressionLock();
            RandoStartLocationsModeBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();
            StartLocationsListBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();
            modeBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();

            // Setup game type button changes
            void SaveShadeVal(RandoMenuItem<bool> item)
            {
                SetShadeSkips(shadeSkipsBtn.CurrentSelection);
            }

            /*void SwitchGameType(bool steelMode)
            {
                if (!steelMode)
                {
                    // Normal mode
                    startRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startNormalBtn.transform.localPosition = new Vector2(0, -200);
                    startSteelRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelNormalBtn.transform.localPosition = new Vector2(10000, 10000);

                    //backBtn.SetNavigation(startNormalBtn, startNormalBtn, modeBtn.Button, startRandoBtn);
                   //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startRandoBtn, allSkillsBtn.Button);
                }
                else
                {
                    // Steel Soul mode
                    startRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startNormalBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startSteelNormalBtn.transform.localPosition = new Vector2(0, -200);

                    SetShadeSkips(false);

                    //backBtn.SetNavigation(startSteelNormalBtn, startSteelNormalBtn, modeBtn.Button, startSteelRandoBtn);
                    //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startSteelNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startSteelRandoBtn, allSkillsBtn.Button);
                }
            }

            gameTypeBtn.Button.AddEvent(EventTriggerType.Submit,
                garbage => SwitchGameType(gameTypeBtn.CurrentSelection != "Normal"));
                */

            // Setup start game button events
            void StartGame(bool rando)
            {
                if (multiworldBtn.CurrentSelection == "Yes")
                {
                    RandomizerMod.Instance.mwConnection.Start();
                }
                else
                {
                    CopySettings(rando);
                    RandomizerMod.Instance.StartNewGame();
                }
            }

            //startNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            startRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
            //startSteelNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            //startSteelRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
        }

        private static void ParseSeedInput(string input)
        {
            if (int.TryParse(input, out int newSeed))
            {
                RandomizerMod.Instance.Settings.RandomizerSettings.Seed = newSeed;
            }
            else
            {
                LogWarn($"Seed input \"{input}\" could not be parsed to an integer");
            }
        }
        private static void ChangeNickname(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            RandomizerMod.Instance.MWSettings.UserName = input;
        }

        private static GameObject CreateLabel(MenuButton baseObj, Vector2 position, string text)
        {
            GameObject label = baseObj.Clone(text + "Label", MenuButton.MenuButtonType.Activate, position, text)
                .gameObject;
            Object.Destroy(label.GetComponent<EventTrigger>());
            Object.Destroy(label.GetComponent<MenuButton>());
            return label;
        }

        private class RandoMenuItem<T> where T : IEquatable<T>
        {
            public delegate void RandoMenuItemChanged(RandoMenuItem<T> item);

            private readonly FixVerticalAlign _align;
            private readonly T[] _selections;
            private readonly Text _text;
            private int _currentSelection;
            private bool _locked = false;

            public RandoMenuItem(MenuButton baseObj, Vector2 position, string name, params T[] values)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                if (baseObj == null)
                {
                    throw new ArgumentNullException(nameof(baseObj));
                }

                if (values == null || values.Length == 0)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                _selections = values;
                Name = name;

                Button = baseObj.Clone(name + "Button", MenuButton.MenuButtonType.Activate, position, string.Empty);

                _text = Button.transform.Find("Text").GetComponent<Text>();
                _text.fontSize = 36;
                _align = Button.gameObject.GetComponentInChildren<FixVerticalAlign>(true);

                Button.ClearEvents();
                Button.AddEvent(EventTriggerType.Submit, GotoNext);

                RefreshText();
            }

            public T CurrentSelection => _selections[_currentSelection];

            public MenuButton Button { get; }

            public string Name { get; set; }

            public event RandoMenuItemChanged Changed
            {
                add => ChangedInternal += value;
                remove => ChangedInternal -= value;
            }

            public void SetName(string n)
            {
                Name = n;
                RefreshText(false);
            }

            private event RandoMenuItemChanged ChangedInternal;

            public void SetSelection(T obj)
            {
                if (_locked) return;

                for (int i = 0; i < _selections.Length; i++)
                {
                    if (_selections[i].Equals(obj))
                    {
                        _currentSelection = i;
                        break;
                    }
                }

                RefreshText(false);
            }

            private void GotoNext(BaseEventData data = null)
            {
                if (_locked) return;

                _currentSelection++;
                if (_currentSelection >= _selections.Length)
                {
                    _currentSelection = 0;
                }

                RefreshText();
            }

            private void RefreshText(bool invokeEvent = true)
            {
                if (typeof(T) == typeof(bool))
                {
                    _text.text = Name;
                }
                else
                {
                    _text.text = Name + ": " + _selections[_currentSelection];
                }

                _align.AlignText();
                SetColor();

                if (invokeEvent)
                {
                    ChangedInternal?.Invoke(this);
                }
            }

            internal void SetColor(Color? c = null)
            {
                if (c is Color forceColor)
                {
                    _text.color = forceColor;
                    return;
                }

                if (!(_selections[_currentSelection] is bool value))
                {
                    if (_locked)
                    {
                        _text.color = LOCKED_FALSE_COLOR;
                    }
                    else
                    {
                        _text.color = Color.white;
                    }
                    return;
                }

                if (!_locked && value)
                {
                    _text.color = TRUE_COLOR;
                }
                else if (!_locked && !value)
                {
                    _text.color = FALSE_COLOR;
                }
                else if (_locked && value)
                {
                    _text.color = LOCKED_TRUE_COLOR;
                }
                else if (_locked && value)
                {
                    _text.color = LOCKED_FALSE_COLOR;
                }
                else
                {
                    _text.color = Color.red;
                }
            }
            
            internal Color GetColor()
            {
                return _text.color;
            }

            internal void Lock()
            {
                _locked = true;
                SetColor();
            }

            internal void Unlock()
            {
                _locked = false;
                SetColor();
            }
        }
    }
}
