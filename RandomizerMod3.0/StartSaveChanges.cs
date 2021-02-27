using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using RandomizerMod.Components;
using RandomizerMod.FsmStateActions;
using SereCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;
using static RandomizerMod.LogHelper;
using System.Collections;
using RandomizerMod.Randomization;
using System;
using Object = UnityEngine.Object;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using static RandomizerMod.GiveItemActions;

using RandomizerLib;

namespace RandomizerMod
{
    public static class StartSaveChanges
    {
        public const string RESPAWN_MARKER_NAME = "Randomizer Respawn Marker";
        public const string RESPAWN_TAG = "RespawnPoint";
        private static StartDef start => LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName);

        private static void CreateRespawnMarker()
        {
            GameObject respawnMarker = ObjectCache.RespawnMarker;
            respawnMarker.transform.SetPosition2D(start.x, start.y);
            respawnMarker.transform.SetPositionZ(7.4f);
            respawnMarker.name = RESPAWN_MARKER_NAME;
            respawnMarker.tag = RESPAWN_TAG;
            respawnMarker.SetActive(true);
        }

        // Merge or move this switch block into scene changes
        public static void StartSceneChanges(Scene newScene)
        {
            if (newScene.name != start.sceneName) return;

            CreateRespawnMarker();

            switch (newScene.name)
            {
                case SceneNames.Ruins1_27:
                    PlayerData.instance.hornetFountainEncounter = true;
                    Object.Destroy(GameObject.Find("Fountain Inspect"));
                    break;

                case SceneNames.Abyss_06_Core:
                    PlayerData.instance.abyssGateOpened = true;
                    break;
            }
        }

        public static void StartDataChanges()
        {
            PlayerData.instance.Reset();

            PlayerData.instance.hasCharm = true;

            /*
            if (RandomizerMod.Instance.Settings.FreeLantern)
            {
                PlayerData.instance.hasLantern = true;
            }
            */

            if (RandomizerMod.Instance.Settings.EarlyGeo)
            {
                // added version checking to the early geo randomization
                int geoSeed = RandomizerMod.Instance.Settings.GeoSeed;
                unchecked
                {
                    geoSeed = geoSeed * 17 + 31 * RandomizerMod.Instance.MakeAssemblyHash();
                }

                Random rand = new Random(geoSeed);
                int startgeo = rand.Next(300, 600);
                PlayerData.instance.AddGeo(startgeo);
            }

            Ref.PD.unchainedHollowKnight = true;
            Ref.PD.encounteredMimicSpider = true;
            Ref.PD.infectedKnightEncountered = true;
            Ref.PD.mageLordEncountered = true;
            Ref.PD.mageLordEncountered_2 = true;

            List<string> startItems = RandomizerMod.Instance.Settings.ItemPlacements.Where(pair => pair.Item2.StartsWith("Equip")).Select(pair => pair.Item1).ToList();
            foreach (string item in startItems)
            {
                RandomizerLib.GiveAction action = LogicManager.GetItemDef(item).action;
                if (action == RandomizerLib.GiveAction.Charm) action = RandomizerLib.GiveAction.EquippedCharm;
                else if (action == RandomizerLib.GiveAction.SpawnGeo) action = RandomizerLib.GiveAction.AddGeo;

                GiveItemWrapper(action, item, "Equipped");
            }

            for (int i = 1; i < 5; i++)
            {
                if (PlayerData.instance.charmSlotsFilled > PlayerData.instance.charmSlots)
                {
                    PlayerData.instance.charmSlots++;
                    PlayerData.instance.SetBool("salubraNotch" + i, true);
                }
                if (PlayerData.instance.charmSlotsFilled <= PlayerData.instance.charmSlots)
                {
                    PlayerData.instance.overcharmed = false;
                    break;
                }
            }

            PlayerData.instance.respawnScene = start.sceneName;
            PlayerData.instance.respawnMarkerName = RESPAWN_MARKER_NAME;
            PlayerData.instance.respawnType = 0;
            PlayerData.instance.mapZone = start.zone;
        }
    }
}
