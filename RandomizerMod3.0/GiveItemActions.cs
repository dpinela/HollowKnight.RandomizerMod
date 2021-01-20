using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerMod.Randomization;
using static RandomizerMod.RandoLogger;
using static RandomizerMod.LogHelper;
using UnityEngine;
using MultiWorldProtocol.Messaging.Definitions.Messages;

using RandomizerLib;
using RandomizerMod.MultiWorld;
using RandomizerLib.MultiWorld;

namespace RandomizerMod
{
    // WORK IN PROGRESS
    public static class GiveItemActions
    {
        // Ugly, but need this to be here for BingoUI compatibility
        public enum GiveAction
        {
            Bool = 0,
            Int,
            Charm,
            EquippedCharm,
            Additive,
            SpawnGeo,
            AddGeo,

            Map,
            Grub,
            Essence,
            Stag,
            DirtmouthStag,

            MaskShard,
            VesselFragment,
            WanderersJournal,
            HallownestSeal,
            KingsIdol,
            ArcaneEgg,

            Dreamer,
            Kingsoul,
            Grimmchild,

            SettingsBool,
            None,
            AddSoul,

            Lifeblood
        }

        // Oh god this is terrible
        public static GiveAction convertGiveAction(RandomizerLib.GiveAction giveAction)
        {
            // bad bad ugly stinky
            return (GiveAction) (int) giveAction;
        }

        // Wrapper to allow me to change stuff without fricking up BingoUI
        public static void GiveItemWrapper(RandomizerLib.GiveAction action, string rawItem, string location, string from = null, int geo = 0, bool remote = false)
        {
            // With multiworld we may end up receiving the same item twice due to network issues etc. so we want giving items to be idempotent (i.e. same cloak twice doesn't give shade)
            if (RandomizerMod.Instance.Settings.CheckItemFound(rawItem)) return;

            (int player, string item) = LogicManager.ExtractPlayerID(rawItem);

            // In tracker log, we only want to show MW(X)_ when picking up items in multiworld (otherwise everything is MW(1))
            if (RandomizerMod.Instance.Settings.IsMW)
            {
                string loc = location;
                if (remote) loc = $"{from}-{location}";
                string it = item;
                if (player != RandomizerMod.Instance.Settings.MWPlayerId)
                {
                    it = $"{LanguageStringManager.GetMWPlayerName(player)}-{item}";
                }
                LogItemToTracker(it, loc);
            }
            else
            {
                LogItemToTracker(item, location);
            }

            if (!remote && RandomizerMod.Instance.Settings.IsMW && player >= 0 && player != RandomizerMod.Instance.Settings.MWPlayerId)
            {
                // Not our item, send it to MW instead
                RandomizerMod.Instance.Settings.MarkItemFound(rawItem);
                RandomizerMod.Instance.Settings.MarkLocationFound(location);
                RandomizerMod.Instance.Settings.AddSentItem(rawItem);
                RandomizerMod.Instance.mwConnection.SendItem(location, item, player);
                return;
            }

            // Remove duplicate _(1)
            item = LogicManager.RemovePrefixSuffix(item);

            // If we received this from MW, display a relic message so the player knows they got an item
            if (remote) ShowRelic(item, from);

            GiveItem(convertGiveAction(action), item, location, geo);

            // Mark the item/location as picked up only once we have actually given the item (there was a bug where sometimes items would not be given but flagged as given?)
            RandomizerMod.Instance.Settings.MarkItemFound(rawItem);
            if (!remote) RandomizerMod.Instance.Settings.MarkLocationFound(location);
            UpdateHelperLog();
        }

        public static void GiveItemMW(string item, string location, string from)
        {
            ReqDef def = LogicManager.GetItemDef(item);
            item = new MWItem(RandomizerMod.Instance.Settings.MWPlayerId, item).ToString();

            // Geo spawning is normally handleded in the shiny, so just add geo instead
            if (def.action == RandomizerLib.GiveAction.SpawnGeo)
                def.action = RandomizerLib.GiveAction.AddGeo;
            GiveItemWrapper(def.action, item, location, from, remote: true);
        }

        public static void ShowRelic(string item, string from)
        {
            string[] itemSet = LogicManager.AdditiveItemSets.FirstOrDefault(set => set.Contains(item));

            if (itemSet != null)
            {
                int current = RandomizerMod.Instance.Settings.GetAdditiveCount(item);
                int max = LogicManager.GetMaxAdditiveLevel(item);
                int next = Math.Min(current + 1, max);
                item = itemSet[next - 1];
            }
            RelicMsg.ShowRelicItem(item, from);
        }

        // TODO: clean up the above? the reason it's all messed up is since I thought GiveAction would make more sense as part of RandomizerLib (LogicManager specifically)
        // However, it's only used in like one place in RandomizerLib. Since we don't want RandomizerLib to depend on RandomizerMod, maybe we could just treat actions
        // as an int over there and convert for usage in RandomizerMod. Either way, I'm leaving this ugly hack in

        // This method is hooked via reflection from BingoUI, and it uses the type "GiveItemActions.GiveAction" for the first parameter
        public static void GiveItem(GiveAction action, string item, string location, int geo = 0)
        {
            item = LogicManager.RemovePrefixSuffix(item); // just to be safe
            switch (action)
            {
                default:
                case GiveAction.Bool:
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).boolName, true);
                    break;

                case GiveAction.Int:
                    {
                        string intName = LogicManager.GetItemDef(item).intName;
                    }
                    PlayerData.instance.IncrementInt(LogicManager.GetItemDef(item).intName);
                    break;

                case GiveAction.Charm:
                    PlayerData.instance.hasCharm = true;
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).boolName, true);
                    PlayerData.instance.charmsOwned++;
                    break;

                case GiveAction.EquippedCharm:
                    PlayerData.instance.hasCharm = true;
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).boolName, true);
                    PlayerData.instance.charmsOwned++;
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).equipBoolName, true);
                    PlayerData.instance.EquipCharm(LogicManager.GetItemDef(item).charmNum);

                    PlayerData.instance.CalculateNotchesUsed();
                    if (PlayerData.instance.charmSlotsFilled > PlayerData.instance.charmSlots)
                    {
                        PlayerData.instance.overcharmed = true;
                    }
                    break;

                case GiveAction.Additive:
                    string[] additiveItems = LogicManager.GetAdditiveItems(LogicManager.AdditiveItemNames.First(s => LogicManager.GetAdditiveItems(s).Contains(item)));
                    int additiveCount = RandomizerMod.Instance.Settings.GetAdditiveCount(item);
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(additiveItems[Math.Min(additiveCount, additiveItems.Length - 1)]).boolName, true);
                    break;

                case GiveAction.AddGeo:
                    if (geo > 0) HeroController.instance.AddGeo(geo);
                    else
                    {
                        HeroController.instance.AddGeo(LogicManager.GetItemDef(item).geo);
                    }
                    
                    break;

                // Disabled because it's more convenient to do this from the fsm. Use GiveAction.None for geo spawns.
                case GiveAction.SpawnGeo:
                    RandomizerMod.Instance.LogError("Tried to spawn geo from GiveItem.");
                    throw new NotImplementedException();

                case GiveAction.AddSoul:
                    HeroController.instance.AddMPCharge(200);
                    break;

                case GiveAction.Map:
                    PlayerData.instance.hasMap = true;
                    PlayerData.instance.openedMapperShop = true;
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).boolName, true);
                    break;

                case GiveAction.Stag:
                    PlayerData.instance.SetBool(LogicManager.GetItemDef(item).boolName, true);
                    PlayerData.instance.stationsOpened++;
                    break;

                case GiveAction.DirtmouthStag:
                    PlayerData.instance.SetBool(nameof(PlayerData.openedTown), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.openedTownBuilding), true);
                    break;

                case GiveAction.Grub:
                    PlayerData.instance.grubsCollected++;
                    int clipIndex = new System.Random().Next(2);
                    AudioSource.PlayClipAtPoint(ObjectCache.GrubCry[clipIndex],
                        new Vector3(
                            Camera.main.transform.position.x - 2,
                            Camera.main.transform.position.y,
                            Camera.main.transform.position.z + 2
                        ));
                    AudioSource.PlayClipAtPoint(ObjectCache.GrubCry[clipIndex],
                        new Vector3(
                            Camera.main.transform.position.x + 2,
                            Camera.main.transform.position.y,
                            Camera.main.transform.position.z + 2
                        ));
                    break;

                case GiveAction.Essence:
                    PlayerData.instance.IntAdd(nameof(PlayerData.dreamOrbs), LogicManager.GetItemDef(item).geo);
                    EventRegister.SendEvent("DREAM ORB COLLECT");
                    break;

                case GiveAction.MaskShard:
                    PlayerData.instance.heartPieceCollected = true;
                    if (PlayerData.instance.heartPieces < 3)
                    {
                        PlayerData.instance.heartPieces++;
                    }
                    else
                    {
                        HeroController.instance.AddToMaxHealth(1);
                        PlayMakerFSM.BroadcastEvent("MAX HP UP");
                        PlayMakerFSM.BroadcastEvent("HERO HEALED FULL");
                        if (PlayerData.instance.maxHealthBase < PlayerData.instance.maxHealthCap)
                        {
                            PlayerData.instance.heartPieces = 0;
                        }
                    }
                    break;

                case GiveAction.VesselFragment:
                    PlayerData.instance.vesselFragmentCollected = true;
                    if (PlayerData.instance.vesselFragments < 2)
                    {
                        GameManager.instance.IncrementPlayerDataInt("vesselFragments");
                    }
                    else
                    {
                        HeroController.instance.AddToMaxMPReserve(33);
                        PlayMakerFSM.BroadcastEvent("NEW SOUL ORB");
                        if (PlayerData.instance.MPReserveMax < PlayerData.instance.MPReserveCap)
                        {
                            PlayerData.instance.vesselFragments = 0;
                        }
                    }
                    break;

                case GiveAction.WanderersJournal:
                    PlayerData.instance.foundTrinket1 = true;
                    PlayerData.instance.trinket1++;
                    break;

                case GiveAction.HallownestSeal:
                    PlayerData.instance.foundTrinket2 = true;
                    PlayerData.instance.trinket2++;
                    break;

                case GiveAction.KingsIdol:
                    PlayerData.instance.foundTrinket3 = true;
                    PlayerData.instance.trinket3++;
                    break;

                case GiveAction.ArcaneEgg:
                    PlayerData.instance.foundTrinket4 = true;
                    PlayerData.instance.trinket4++;
                    break;

                case GiveAction.Dreamer:
                    switch (item)
                    {
                        case "Lurien":
                            if (PlayerData.instance.lurienDefeated) break;
                            PlayerData.instance.lurienDefeated = true;
                            PlayerData.instance.maskBrokenLurien = true;
                            break;
                        case "Monomon":
                            if (PlayerData.instance.monomonDefeated) break;
                            PlayerData.instance.monomonDefeated = true;
                            PlayerData.instance.maskBrokenMonomon = true;
                            break;
                        case "Herrah":
                            if (PlayerData.instance.hegemolDefeated) break;
                            PlayerData.instance.hegemolDefeated = true;
                            PlayerData.instance.maskBrokenHegemol = true;
                            break;
                    }
                    if (PlayerData.instance.guardiansDefeated == 0)
                    {
                        PlayerData.instance.hornetFountainEncounter = true;
                        PlayerData.instance.marmOutside = true;
                        PlayerData.instance.crossroadsInfected = true;
                    }
                    if (PlayerData.instance.guardiansDefeated == 2)
                    {
                        PlayerData.instance.dungDefenderSleeping = true;
                        PlayerData.instance.brettaState++;
                        PlayerData.instance.mrMushroomState++;
                        PlayerData.instance.corniferAtHome = true;
                        PlayerData.instance.metIselda = true;
                        PlayerData.instance.corn_cityLeft = true;
                        PlayerData.instance.corn_abyssLeft = true;
                        PlayerData.instance.corn_cliffsLeft = true;
                        PlayerData.instance.corn_crossroadsLeft = true;
                        PlayerData.instance.corn_deepnestLeft = true;
                        PlayerData.instance.corn_fogCanyonLeft = true;
                        PlayerData.instance.corn_fungalWastesLeft = true;
                        PlayerData.instance.corn_greenpathLeft = true;
                        PlayerData.instance.corn_minesLeft = true;
                        PlayerData.instance.corn_outskirtsLeft = true;
                        PlayerData.instance.corn_royalGardensLeft = true;
                        PlayerData.instance.corn_waterwaysLeft = true;
                    }
                    if (PlayerData.instance.guardiansDefeated < 3)
                    {
                        PlayerData.instance.guardiansDefeated++;
                    }
                    break;

                case GiveAction.Kingsoul:
                    if (PlayerData.instance.royalCharmState < 4)
                    {
                        PlayerData.instance.royalCharmState++;
                    }
                    switch (PlayerData.instance.royalCharmState)
                    {
                        case 1:
                            PlayerData.instance.gotCharm_36 = true;
                            PlayerData.instance.charmsOwned++;
                            break;
                        case 2:
                            PlayerData.instance.royalCharmState++;
                            break;
                        case 4:
                            PlayerData.instance.gotShadeCharm = true;
                            PlayerData.instance.charmCost_36 = 0;
                            PlayerData.instance.equippedCharm_36 = true;
                            if (!PlayerData.instance.equippedCharms.Contains(36)) PlayerData.instance.equippedCharms.Add(36);
                            break;
                    }
                    break;

                case GiveAction.Grimmchild:
                    PlayerData.instance.SetBool(nameof(PlayerData.instance.gotCharm_40), true);
                    // Skip first two collection quests
                    PlayerData.instance.SetBool(nameof(PlayerData.nightmareLanternAppeared), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.nightmareLanternLit), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.troupeInTown), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.divineInTown), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.metGrimm), true);
                    PlayerData.instance.SetInt(nameof(PlayerData.flamesRequired), 3);
                    PlayerData.instance.SetInt(nameof(PlayerData.flamesCollected), 3);
                    PlayerData.instance.SetBool(nameof(PlayerData.killedFlameBearerSmall), true);
                    PlayerData.instance.SetBool(nameof(PlayerData.killedFlameBearerMed), true);
                    PlayerData.instance.SetInt(nameof(PlayerData.killsFlameBearerSmall), 3);
                    PlayerData.instance.SetInt(nameof(PlayerData.killsFlameBearerMed), 3);
                    PlayerData.instance.SetInt(nameof(PlayerData.grimmChildLevel), 2);

                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Mines_10",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins1_28",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Fungus1_10",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Tutorial_01",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "RestingGrounds_06",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Deepnest_East_03",
                        id = "Flamebearer Spawn",
                        activated = true,
                        semiPersistent = false
                    });
                    break;

                case GiveAction.SettingsBool:
                    RandomizerMod.Instance.Settings.SetBool(true, LogicManager.GetItemDef(item).boolName);
                    break;

                case GiveAction.None:
                    break;
                
                case GiveAction.Lifeblood:
                    var n = LogicManager.GetItemDef(item).lifeblood;
                    for (int i = 0; i < n; i++)
                    {
                        EventRegister.SendEvent("ADD BLUE HEALTH");
                    }
                    break;
            }

            // additive, kingsoul, bool type items can all have additive counts
            if (LogicManager.AdditiveItemSets.Any(set => set.Contains(item)))
            {
                RandomizerMod.Instance.Settings.IncrementAdditiveCount(item);

                // Give bonus 300 geo here so it works with MW items
                int max = LogicManager.GetMaxAdditiveLevel(item);
                if (RandomizerMod.Instance.Settings.GetAdditiveCount(item) > max)
                {
                    HeroController.instance.AddGeo(300);
                }
            }
        }
    }
}
