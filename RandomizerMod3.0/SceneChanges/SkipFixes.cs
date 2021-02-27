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

namespace RandomizerMod.SceneChanges
{
    internal static partial class SceneEditor
    {
        public static void FixMiscSkips(Scene newScene)
        {
            switch (newScene.name)
            {
                // Make Cliffs shade skip feasible
                case SceneNames.Fungus1_28:
                    GameObject cliffsCrawlid = Object.Instantiate(GameObject.Find("Crawler"));
                    cliffsCrawlid.SetActive(true);
                    cliffsCrawlid.transform.position = new Vector2(74f, 31f);
                    if (RandomizerMod.Instance.Settings.ShadeSkips && RandomizerMod.Instance.Settings.SpicySkips && PlayerData.instance.hasDoubleJump && !PlayerData.instance.hasWalljump)
                    {
                        foreach (GameObject g in GameManager.FindObjectsOfType<GameObject>())
                        {
                            if (g.transform.GetPositionX() < 75 && g.transform.GetPositionX() > 70 && g.transform.GetPositionY() < 54 && g.transform.GetPositionY() > 33)
                            {
                                Object.Destroy(g);
                            }
                        }
                    }
                    break;

                // Make descending dark spikes pogoable
                case SceneNames.Mines_35:
                    foreach (NonBouncer nonBounce in Object.FindObjectsOfType<NonBouncer>())
                    {
                        if (nonBounce.gameObject.name.StartsWith("Spike Collider"))
                        {
                            nonBounce.active = false;
                            nonBounce.gameObject.AddComponent<RandomizerTinkEffect>();
                        }
                    }
                    break;

                // Wings-only pogo to Soul Sanctum
                case SceneNames.Ruins1_05:
                    GameObject chandelier = GameObject.Find("ruind_dressing_light_02 (10)");
                    chandelier.transform.SetPositionX(chandelier.transform.position.x - 2);
                    chandelier.GetComponent<NonBouncer>().active = false;
                    break;

                // Sign pogo to get past right-side city without items
                case SceneNames.Ruins2_04:
                    {
                        GameObject plat = ObjectCache.SmallPlatform;
                        plat.SetActive(true);
                        plat.transform.position = new Vector2(18f, 10f);
                    }
                    break;

                // Respawn jars in Collector's room to allow wings only access
                case SceneNames.Ruins2_11:
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (1)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (2)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (3)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (4)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (5)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (6)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (7)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (8)",
                        activated = false,
                        semiPersistent = false
                    });
                    break;
            }
        }

        public static void FixLeverSkips(Scene newScene)
        {
            void RemoveRangeCheck(string obj, string fsm)
            {
                if (GameObject.Find(obj).LocateMyFSM(fsm).GetState("Range") is FsmState range)
                {
                    range.RemoveActionsOfType<BoolTest>();
                }
                if (GameObject.Find(obj).LocateMyFSM(fsm).GetState("Check If Nail") is FsmState checkNail)
                {
                    checkNail.RemoveActionsOfType<BoolTest>();
                }
            }

            void RemoveCURSEDRangeCheck(string objName, string fsm)
            {
                foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.Contains(objName))
                    {
                        go.LocateMyFSM(fsm).GetState("Range").RemoveActionsOfType<BoolTest>();
                        go.LocateMyFSM(fsm).GetState("Check If Nail").RemoveActionsOfType<BoolTest>();
                    }
                }
            }
            switch (newScene.name)
            {
                case SceneNames.Crossroads_03:
                    // Toll Gate Switch - toll switch
                    break;
                case SceneNames.Fungus2_18:
                    RemoveRangeCheck("Mantis Lever", "Switch Control"); // no actual lever skip, but the hitbox was dramatically reduced anyways for no good reason
                    break;
                case SceneNames.Fungus3_44:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_05:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_05b:
                    //EXTREMELY CURSED LEVER
                    RemoveCURSEDRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_05 + "c":
                    RemoveRangeCheck("Ruins Lever 2", "Switch Control");
                    RemoveRangeCheck("Ruins Lever 3", "Switch Control");
                    break;
                case SceneNames.Ruins1_17:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_23:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    RemoveRangeCheck("Ruins Lever (1)", "Switch Control");
                    break;
                case SceneNames.Ruins1_25:
                    //EXTREMELY CURSED LEVER
                    RemoveCURSEDRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_27:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_30:
                    //EXTREMELY CURSED LEVER
                    RemoveCURSEDRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Ruins1_32:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    RemoveRangeCheck("Ruins Lever (1)", "Switch Control");
                    break;
                case SceneNames.Ruins2_01:
                    //EXTREMELY CURSED LEVER
                    RemoveCURSEDRangeCheck("Ruins Lever", "Switch Control");
                    break;
                case SceneNames.Waterways_09:
                    RemoveRangeCheck("Ruins Lever", "Switch Control");
                    break;
            }
        }
    }
}
