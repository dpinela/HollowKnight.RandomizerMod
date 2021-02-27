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
using RandomizerMod.SceneChanges;
using RandomizerMod.Randomization;

using RandomizerLib;

namespace RandomizerMod.SceneChanges
{
    internal static partial class SceneEditor
    {
        /*
         * Better organization someday...
         */
        public static void MiscQoLChanges(Scene newScene)
        {
            string sceneName = newScene.name;

            // Make baldurs always able to spit rollers and reduce hp
            if (sceneName == SceneNames.Crossroads_11_alt || sceneName == SceneNames.Crossroads_ShamanTemple ||
                sceneName == SceneNames.Fungus1_28)
            {
                foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                {
                    if (obj.name.Contains("Blocker"))
                    {
                        HealthManager hm = obj.GetComponent<HealthManager>();
                        if (hm != null)
                        {
                            hm.hp = 5;
                        }
                        PlayMakerFSM fsm = FSMUtility.LocateFSM(obj, "Blocker Control");
                        if (fsm != null)
                        {
                            fsm.GetState("Can Roller?").RemoveActionsOfType<IntCompare>();
                        }
                    }
                }
            }

            switch (sceneName)
            {
                // Lemm sell all
                /*
                case SceneNames.Ruins1_05b when RandomizerMod.Instance.Settings.Lemm:
                    PlayMakerFSM lemm = FSMUtility.LocateFSM(GameObject.Find("Relic Dealer"), "npc_control");
                    lemm.GetState("Convo End").AddAction(new RandomizerSellRelics());
                    break;
                */

                // Grubfather rewards are given out all at once
                case SceneNames.Crossroads_38 when RandomizerMod.Instance.Settings.Grubfather:
                    PlayMakerFSM grubDaddy = FSMUtility.LocateFSM(GameObject.Find("Grub King"), "King Control");
                    grubDaddy.GetState("Final Reward?").RemoveTransitionsTo("Recover");
                    grubDaddy.GetState("Final Reward?").AddTransition("FINISHED", "Recheck");
                    grubDaddy.GetState("Recheck").RemoveTransitionsTo("Gift Anim");
                    grubDaddy.GetState("Recheck").AddTransition("FINISHED", "Activate Reward");

                    int geoTotal = 0;
                    grubDaddy.GetState("All Given").AddAction(new RandomizerAddGeo(grubDaddy.gameObject, 0, true));
                    grubDaddy.GetState("Recheck").AddFirstAction(new RandomizerExecuteLambda(() =>
                        grubDaddy.GetState("All Given").GetActionsOfType<RandomizerAddGeo>()[0].SetGeo(geoTotal)));

                    foreach (PlayMakerFSM grubFsm in grubDaddy.gameObject.GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (grubFsm.FsmName == "grub_reward_geo")
                        {
                            FsmState grubGeoState = grubFsm.GetState("Remaining?");
                            int geo = grubGeoState.GetActionsOfType<IntCompare>()[0].integer1.Value;

                            grubGeoState.RemoveActionsOfType<FsmStateAction>();
                            grubGeoState.AddAction(new RandomizerExecuteLambda(() => geoTotal += geo));
                            grubGeoState.AddTransition("FINISHED", "End");
                        }
                    }
                    break;

                // Great Hopper Easter Egg, I guess
                case SceneNames.Deepnest_East_16:
                    GameObject hopper1 = newScene.FindGameObject("Giant Hopper");
                    GameObject hopper2 = newScene.FindGameObject("Giant Hopper (1)");

                    for (int i = 0; i < 10; i++)
                    {
                        GameObject newHopper1 = Object.Instantiate(hopper1, hopper1.transform.parent);
                        GameObject newHopper2 = Object.Instantiate(hopper2, hopper2.transform.parent);

                        HealthManager hopper1HM = newHopper1.GetComponent<HealthManager>();
                        hopper1HM.SetGeoSmall(0);
                        hopper1HM.SetGeoMedium(0);
                        hopper1HM.SetGeoLarge(0);

                        HealthManager hopper2HM = newHopper2.GetComponent<HealthManager>();
                        hopper2HM.SetGeoSmall(0);
                        hopper2HM.SetGeoMedium(0);
                        hopper2HM.SetGeoLarge(0);

                        Vector3 hopper1Pos = newHopper1.transform.localPosition;
                        hopper1Pos = new Vector3(
                            hopper1Pos.x + i,
                            hopper1Pos.y,
                            hopper1Pos.z);
                        newHopper1.transform.localPosition = hopper1Pos;

                        Vector3 hopper2Pos = newHopper2.transform.localPosition;
                        hopper2Pos = new Vector3(
                            hopper2Pos.x + i - 4,
                            hopper2Pos.y,
                            hopper2Pos.z);
                        newHopper2.transform.localPosition = hopper2Pos;
                    }
                    break;

                // Skip dreamer text before Dream Nail
                case SceneNames.RestingGrounds_04:
                    FsmState dreamerPlaqueInspect = FSMUtility
                        .LocateFSM(GameObject.Find("Dreamer Plaque Inspect"), "Conversation Control")
                        .GetState("Hero Anim");
                    dreamerPlaqueInspect.RemoveActionsOfType<ActivateGameObject>();
                    dreamerPlaqueInspect.RemoveTransitionsTo("Fade Up");
                    dreamerPlaqueInspect.AddTransition("FINISHED", "Map Msg?");

                    PlayMakerFSM dreamerScene2 = FSMUtility.LocateFSM(GameObject.Find("Dreamer Scene 2"), "Control");
                    dreamerScene2.GetState("Take Control").RemoveTransitionsTo("Blast");
                    dreamerScene2.GetState("Take Control").AddTransition("FINISHED", "Fade Out");
                    dreamerScene2.GetState("Fade Out").RemoveTransitionsTo("Dial Wait");
                    dreamerScene2.GetState("Fade Out").AddTransition("FINISHED", "Set Compass Point");
                    break;
            }
        }

        public static void ApplyHintChanges(Scene newScene)
        {
            switch (newScene.name)
            {
                // King Fragment hint
                case SceneNames.Abyss_05:
                    {
                        string item = RandomizerMod.Instance.Settings.ItemPlacements.FirstOrDefault(pair => pair.Item2 == "King_Fragment").Item1;
                        string itemName = LanguageStringManager.GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
                        LanguageStringManager.SetString(
                            "Lore Tablets",
                            "DUSK_KNIGHT_CORPSE",
                            "A corpse in white armour. You can clearly see the "
                                + itemName + " it's holding, " +
                                "but for some reason you get the feeling you're going to have to go" +
                                " through an unnecessarily long gauntlet of spikes and sawblades just to pick it up."
                                );
                    }
                    break;

                // Colosseum hints
                case SceneNames.Room_Colosseum_01:
                    {
                        string item = RandomizerMod.Instance.Settings.ItemPlacements.FirstOrDefault(pair => pair.Item2 == "Charm_Notch-Colosseum").Item1;
                        string itemName = LanguageStringManager.GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
                        LanguageStringManager.SetString("Prompts", "TRIAL_BOARD_BRONZE", "Trial of the Warrior. Fight for " + itemName + ".\n" + "Place a mark and begin the Trial?");
                    }
                    {
                        string item = RandomizerMod.Instance.Settings.ItemPlacements.FirstOrDefault(pair => pair.Item2 == "Pale_Ore-Colosseum").Item1;
                        string itemName = LanguageStringManager.GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
                        LanguageStringManager.SetString("Prompts", "TRIAL_BOARD_SILVER", "Trial of the Conqueror. Fight for " + itemName + ".\n" + "Place a mark and begin the Trial?");
                    }

                    break;

                // Grey Mourner hint
                case SceneNames.Room_Mansion:
                    {
                        string item = RandomizerMod.Instance.Settings.ItemPlacements.FirstOrDefault(pair => pair.Item2 == "Mask_Shard-Grey_Mourner").Item1;
                        string itemName = LanguageStringManager.GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
                        LanguageStringManager.SetString(
                            "Prompts", 
                            "XUN_OFFER", 
                            "Accept the Gift, even knowing you'll only get a lousy " + itemName + "?"
                            );
                    }
                    
                    break;

                // Enable Jiji hints when the player does not have a shade
                case SceneNames.Room_Ouiji:
                    if (PlayerData.instance.shadeScene != "None")
                    {
                        PlayMakerFSM jijiFsm = GameObject.Find("Jiji NPC").LocateMyFSM("Conversation Control");
                        FsmState HasShade = jijiFsm.GetState("Has Shade?");
                        HasShade.RemoveTransitionsTo("Check Location");
                        HasShade.AddTransition("YES", "Offer");
                    }
                    else if (RandomizerMod.Instance.Settings.Jiji)
                    {
                        PlayerData.instance.SetString("shadeMapZone", "HIVE");
                        PlayMakerFSM jijiFsm = GameObject.Find("Jiji NPC").LocateMyFSM("Conversation Control");
                        FsmState BoxUp = jijiFsm.GetState("Box Up");
                        BoxUp.ClearTransitions();
                        BoxUp.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        BoxUp.AddTransition("FINISHED", "Offer");
                        FsmState SendText = jijiFsm.GetState("Send Text");
                        SendText.RemoveTransitionsTo("Yes");
                        SendText.AddTransition("YES", "Check Location");
                        FsmState CheckLocation = jijiFsm.GetState("Check Location");
                        CheckLocation.AddFirstAction(BoxUp.GetActionsOfType<SendEventByName>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<PlayerDataIntAdd>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<SendEventByName>()[0]);
                    }
                    {
                        GameObject Jinn = ObjectCache.Jinn;
                        Jinn.SetActive(true);
                        Jinn.transform.position = GameObject.Find("Jiji NPC").transform.position + new Vector3(-10f, 0, 0);
                        FsmState transaction = Jinn.LocateMyFSM("Conversation Control").GetState("Transaction");
                        transaction.RemoveActionsOfType<RandomInt>();
                        transaction.RemoveActionsOfType<CallMethodProper>();
                        transaction.AddFirstAction(new RandomizerExecuteLambda(() => HeroController.instance.AddGeo(450)));
                    }

                    break;

                // Tuk only sells eggs when you have no eggs in your inventory, to balance around hints and/or eggs
                case SceneNames.Waterways_03:
                    GameObject.Find("Tuk NPC").LocateMyFSM("Conversation Control").GetState("Convo Choice").GetActionOfType<IntCompare>().integer2 = 1;
                    break;
            }
        }
    }
}
