﻿using BepInEx;
using BepInEx.Configuration;
using EntityStates.Commando;
using EntityStates.Huntress;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RoR2Cheats
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.harbingerofme.RoR2Cheats", "RoR2Cheats", "2.4.0")]
    public class Cheats : BaseUnityPlugin
    {

        private static ConfigEntry<float> SprintFovMultiplierConfig;
        private static ConfigEntry<float> FovConfig;
        // private static ConfigEntry<float> fovConfig { get; set; }

        public static float SprintFoVMultiplier { get { return SprintFovMultiplierConfig.Value; } set { SprintFovMultiplierConfig.Value = value; } }
        public static float FieldOfVision { get { return FovConfig.Value; } set { FovConfig.Value = value; } }

        public static bool noEnemies = false;
        public static ulong seed =0;
        public static readonly Cheats instance;

        public void Awake()
        {
            Logger.LogMessage("Harb's and Paddy's Version. Original by Morris1927.");
            SprintFovMultiplierConfig = Config.AddSetting(
                "0 FOV",
                "sprint Fov Multiplier",
                1.3f,
                new ConfigDescription(
                "What FOV gets multiplied by while sprinting",
                new AcceptableValueRange<float>(1f, 2f)
                )
            );
            FovConfig = Config.AddSetting(
                "0 FOV",
                "Base FOV",
                60f,
                "Your base Field of vision"
            );

            Hooks.InitializeHooks();
            NetworkHandler.RegisterNetworkHandlerAttributes();
        }

        //[ConCommand(commandName = "getBodyMatch", flags = ConVarFlags.None, helpText = "Match a body prefab")]
        //private static void CCGetBodyMatch(ConCommandArgs args)
        //{
            
        //    Debug.Log(Character.Instance.GetBodyName(args[0]).ToString());
        //}

        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = "Godmode")]
        private static void CCGodModeToggle(ConCommandArgs _)
        {
            var godToggleMethod = typeof(CharacterMaster).GetMethodCached("ToggleGod");
            bool hasNotYetRun = true;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                godToggleMethod.Invoke(playerInstance.master, null);
                if (hasNotYetRun)
                {
                    Debug.Log($"God mode {(playerInstance.master.GetBody().healthComponent.godMode ? "enabled" : "disabled")}.");
                    hasNotYetRun = false;
                }
            }
        }

        [ConCommand(commandName = "time_scale", flags = ConVarFlags.Engine | ConVarFlags.ExecuteOnServer, helpText = "Time scale")]
        private static void CCTimeScale(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(Time.timeScale);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float scale))
            {
                Time.timeScale = scale;
                Debug.Log("Time scale set to " + scale);
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: time_scale 0.5");
            }

            NetworkWriter networkWriter = new NetworkWriter();
            networkWriter.StartMessage(101);
            networkWriter.Write((double)Time.timeScale);

            networkWriter.FinishMessage();
            NetworkServer.SendWriterToReady(null, networkWriter, QosChannelIndex.time.intVal);
        }

        [NetworkMessageHandler(msgType = 101, client = true, server = false)]
        private static void HandleTimeScale(NetworkMessage netMsg)
        {
            NetworkReader reader = netMsg.reader;
            Time.timeScale = (float)reader.ReadDouble();
        }

        [ConCommand(commandName = "list_items", flags = ConVarFlags.None, helpText = "List all item names and their IDs")]
        private static void CCListItems(ConCommandArgs _)
        {
            StringBuilder text = new StringBuilder();
            foreach (ItemIndex item in ItemCatalog.allItems)
            {
                int index = (int)item;
                string line = string.Format("{0} = {1}", index, item);
                text.AppendLine(line);
            }
            Debug.Log(text.ToString());
        }

        [ConCommand(commandName = "list_equips", flags = ConVarFlags.None, helpText = "List all equipment items and their IDs")]
        private static void CCListEquipments(ConCommandArgs _)
        {
            StringBuilder text = new StringBuilder();
            foreach (EquipmentIndex item in EquipmentCatalog.allEquipment)
            {
                int index = (int)item;
                string line = string.Format("{0} = {1}", index, item);
                text.AppendLine(line);
            }
            Debug.Log(text.ToString());
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.None, helpText = "Give item directly in the player's inventory. give_item <id> <amount> <playerid>")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                return;
            }

            if (args.Count<2 || !TextSerialization.TryParseInvariant(args[1], out int itemCount))
            {
                itemCount = 1;
            }

            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 3)
            {
                NetworkUser player = GetNetUserFromString(args[2]);
                inventory = (player == null) ? inventory : player.master.inventory;
            }

            inventory.GiveItem((ItemIndex)Enum.Parse(typeof(ItemIndex), args[0], true), itemCount);
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Give equipment directly to a player's inventory.")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                return;
            }

            Inventory inventory = args.sender.master.inventory;
            if (args.Count >= 2)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                inventory = (player == null) ? inventory : player.master.inventory;
            }

            inventory.SetEquipmentIndex((EquipmentIndex)Enum.Parse(typeof(EquipmentIndex), args[0], true));

        }

        [ConCommand(commandName = "give_money", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives money")]
        private static void CCGiveMoney(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out uint result))
            {
                return;
            }

            if (args.Count <2 || args[1].ToLower() != "all")
            {
                CharacterMaster master = args.sender.master;
                if (args.Count >= 2)
                {
                    NetworkUser player = GetNetUserFromString(args[1]);
                    if (player != null)
                    {
                        master = player.master;
                    }
                }
                master.GiveMoney(result);
            }
            else
            {
                TeamManager.instance.GiveTeamMoney(args.sender.master.teamIndex, result);
            }
        }

        [Obsolete("Fix this in issue #14. Use team_set_level")]
        [ConCommand(commandName = "give_exp", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives experience")]
        private static void CCGiveExperience(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                return;
            }

            if (TeamManager.instance && uint.TryParse(args[0], out uint result))
            {
                TeamManager.instance.GiveTeamExperience(args.sender.master.teamIndex,result);
            }
        }

        [ConCommand(commandName = "next_round", flags = ConVarFlags.ExecuteOnServer, helpText = "Start next round. Additional args for specific scene.")]
        private static void CCNextRound(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Run.instance.AdvanceStage(Run.instance.nextStageScene);
                return;
            }

            string stageString = args[0];

            List<string> array = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                array.Add(SceneUtility.GetScenePathByBuildIndex(i).Replace("Assets/RoR2/Scenes/", "").Replace(".unity", ""));
            }

            if (array.Contains(stageString))
            {
                Run.instance.AdvanceStage(SceneCatalog.GetSceneDefFromSceneName(stageString));
                return;
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: next_round golemplains   --- Here is a list of available scenes");
                Debug.Log(string.Join("\n", array));
            }
        }

        [ConCommand(commandName = "seed", flags = ConVarFlags.ExecuteOnServer, helpText = "Set seed.")]
        private static void CCUseSeed(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                string s = "Current Seed is ";
                if (PreGameController.instance)
                {
                    s += PreGameController.instance.runSeed;
                }
                else 
                {
                    s += (seed == 0) ? "random" : seed.ToString();
                }
                Debug.Log(s);
                return;
            }
            args.CheckArgumentCount(1);
            if (!TextSerialization.TryParseInvariant(args[0], out ulong result))
            {
                throw new ConCommandException("Specified seed is not a parsable uint64.");
            }

            if (PreGameController.instance)
            {
                PreGameController.instance.runSeed = (result == 0) ? RoR2Application.rng.nextUlong  : result ;
            }
            seed = result;
        }

        [ConCommand(commandName = "fixed_time", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets fixed time - Affects monster difficulty")]
        private static void CCSetTime(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Debug.Log(Run.instance.fixedTime);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float setTime))
            {
                Run.instance.fixedTime = setTime;
                ResetEnemyTeamLevel();
                Debug.Log("Fixed_time set to " + setTime);
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: fixed_time 600");
            }

        }

        [Obsolete("Fix this in issue #14. Use run_set_stages_cleared")]
        [ConCommand(commandName = "stage_clear_count", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets stage clear count - Affects monster difficulty")]
        private static void CCSetClearCount(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(Run.instance.stageClearCount);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out int setClearCount))
            {
                Run.instance.stageClearCount = setClearCount;
                ResetEnemyTeamLevel();
                Debug.Log("Stage_clear_count set to " + setClearCount);
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: stage_clear_count 5");
            }

        }

        [ConCommand(commandName = "no_enemies", flags = ConVarFlags.ExecuteOnServer, helpText = "Stops enemies from spawning")]
        private static void CCNoEnemies(ConCommandArgs args)
        {
            if (args.Count > 0 && TextSerialization.TryParseInvariant(args[0], out int desired))
            {
                if (desired == 0)
                {
                    noEnemies = false;
                }
                else
                {
                    noEnemies = true;
                }
            }
            else
            {
                noEnemies = !noEnemies;
            }
            Debug.Log("No_enemies set to " + noEnemies);
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn as a new character. Type body_list for a full list of characters")]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (args.Count == 0)
            {

                return;
            }


            string character = Character.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Debug.LogFormat(MagicVars.SPAWN_ERROR, character);
                return;
            }

            CharacterMaster master = args.sender.master;
            if (args.Count > 1)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                if(player != null)
                {
                    master = player.master;
                }
            }

            if (!master.alive)
            {
                Debug.Log(MagicVars.PLAYER_DEADRESPAWN);
                return;
            }

            GameObject newBody = BodyCatalog.FindBodyPrefab(character);

            if (newBody == null)
            {
                List<string> array = new List<string>();
                foreach (var item in BodyCatalog.allBodyPrefabs)
                {
                    array.Add(item.name);
                }
                string list = string.Join("\n", array);
                Debug.LogFormat(MagicVars.SPAWN_ERROR + "   --- \n{1}", character, list);
                return;
            }
            master.bodyPrefab = newBody;
            Debug.Log(args.sender.userName + " is spawning as " + character);

            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
        }

        private static NetworkUser GetNetUserFromString(string playerString)
        {
            if (playerString != "")
            {
                if (int.TryParse(playerString, out int result))
                {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                    {

                        return NetworkUser.readOnlyInstancesList[result];
                    }
                    Debug.Log(MagicVars.PLAYER_NOTFOUND);
                    return null;
                }
                else
                {
                    foreach (NetworkUser n in NetworkUser.readOnlyInstancesList)
                    {
                        if (n.userName.Equals(playerString, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return n;
                        }
                    }
                    Debug.Log(MagicVars.PLAYER_NOTFOUND);
                    return null;
                }
            }

            return null;
        }

        [ConCommand(commandName = "player_list", flags = ConVarFlags.ExecuteOnServer, helpText = "Shows list of players with their ID")]
        private static void CCPlayerList(ConCommandArgs _)
        {
            NetworkUser n;
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                n = NetworkUser.readOnlyInstancesList[i];
                Debug.Log(i + ": " + n.userName);
            }
        }

        [ConCommand(commandName = "fov_sprint_multiplier", flags = ConVarFlags.Engine, helpText = "Set your sprint FOV multiplier")]
        private static void CCSetSprintFOVMulti(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(SprintFoVMultiplier);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float sprintFov))
            {
                SprintFoVMultiplier = sprintFov;
                Debug.Log("Set Sprint FOV Multiplier to " + SprintFoVMultiplier);
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: sprint_fov_multiplier 1");
            }
        }

        [ConCommand(commandName = "fov", flags = ConVarFlags.Engine, helpText = "Set your FOV")]
        private static void CCSetFov(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(FieldOfVision);
                return;
            }

            if (TextSerialization.TryParseInvariant(args[0], out float fovTemp))
            {
                FieldOfVision = fovTemp;
                DodgeState.dodgeFOV = FieldOfVision - 10f;
                BackflipState.dodgeFOV = FieldOfVision - 10f;
                Debug.Log("Set FOV to " + FieldOfVision);

                List<CameraRigController> instancesList = (List<CameraRigController>)typeof(CameraRigController).GetField("instancesList", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                foreach (CameraRigController c in instancesList)
                {
                    c.baseFov = FieldOfVision;
                }
            }
            else
            {
                Debug.Log("Incorrect arguments. Try: fov 60");
            }
        }

        [ConCommand(commandName = "kill_all", flags = ConVarFlags.ExecuteOnServer, helpText = "Kill all members of a team. Default is monster.")]
        private static void CCKillAll(ConCommandArgs args)
        {

            TeamIndex team;
            if (args.Count == 0)
            {
                team = TeamIndex.Monster;
            }
            else
            {
                team = args.GetArgEnum<TeamIndex>(0);
            }
            int count = 0;
            foreach (CharacterMaster cm in FindObjectsOfType<CharacterMaster>())
            {
                if (cm.teamIndex == team)
                {
                    CharacterBody cb = cm.GetBody();
                    if (cb)
                    {
                        if (cb.healthComponent)
                        {
                            cb.healthComponent.Suicide(null);
                            count++;
                        }
                    }

                }

            }
            Debug.Log("Killed " + count + " of team " + team + ".");
        }

        [ConCommand(commandName = "spawn_ai", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawn an AI")]
        private static void CCSpawnAI(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);

            string character = Character.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Debug.LogFormat(MagicVars.SPAWN_ERROR, character);
                return;
            }
            var prefab = MasterCatalog.FindMasterPrefab(Character.Instance.GetMasterName(args[0]));
            var body = BodyCatalog.FindBodyPrefab(character);


            var bodyGameObject = Instantiate<GameObject>(prefab, args.sender.master.GetBody().transform.position, Quaternion.identity);
            CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
            NetworkServer.Spawn(bodyGameObject);
            master.SpawnBody(body, args.sender.master.GetBody().transform.position, Quaternion.identity);

            if (args.Count>1 && Enum.TryParse<EliteIndex>(args[1], true, out EliteIndex eliteIndex))
            {
                if ((int)eliteIndex > (int)EliteIndex.None && (int)eliteIndex < (int)EliteIndex.Count)
                {
                    master.inventory.SetEquipmentIndex(EliteCatalog.GetEliteDef(eliteIndex).eliteEquipmentIndex);
                }
            }

            if (args.Count > 2 && Enum.TryParse<TeamIndex>(args[2], true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    master.teamIndex = teamIndex;
                }
            }

            if (args.Count > 3 && bool.TryParse(args[3], out bool braindead))
            {
                if (braindead)
                {
                    Destroy(master.GetComponent<BaseAI>());
                }
            }
            Debug.Log(MagicVars.SPAWN_ATTEMPT + character);
        }

        [ConCommand(commandName = "spawn_body", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns a CharacterBody")]
        private static void CCSpawnBody(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);

            string character = Character.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Debug.LogFormat(MagicVars.SPAWN_ERROR, character);
                return;
            }

            GameObject body = BodyCatalog.FindBodyPrefab(character);

            GameObject gameObject = Instantiate<GameObject>(body, args.sender.master.GetBody().transform.position, Quaternion.identity);

            NetworkServer.Spawn(gameObject);
            Debug.Log(MagicVars.SPAWN_ATTEMPT + character);
        }

        private static void ResetEnemyTeamLevel()
        {
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }

        [ConCommand(commandName = "true_kill", flags = ConVarFlags.ExecuteOnServer, helpText = "Truly kill a player, ignoring revival effects")]
        private static void CCTrueKill(ConCommandArgs args)
        {
            CharacterMaster master = args.sender.master;
            if (args.Count > 0)
            {
                NetworkUser player = GetNetUserFromString(args[0]);
                if(player != null)
                {
                    master = player.master;
                }
            }

            master.TrueKill();
        }

        [ConCommand(commandName = "add_portal", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a blue, gold, or celestial portal")]
        private static void CCAddPortal(ConCommandArgs args)
        {
            if (TeleporterInteraction.instance)
            {
                switch (args[0])
                {
                    case "blue":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
                        break;
                    case "gold":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
                        break;
                    case "celestial":
                        TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
                        break;
                    default: Debug.Log("Accepeter parameters are blue, gold, celestial");
                        break;

                }
            }
        }

        [Obsolete("Use add_portal instead")]
        [ConCommand(commandName = "add_blue", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a blue portal on completion")]
        private static void CCAddBlueOrb(ConCommandArgs _)
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
            }
        }

        [Obsolete("Use add_portal instead")]
        [ConCommand(commandName = "add_gold", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a gold portal on completion")]
        private static void CCAddGoldOrb(ConCommandArgs _)
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
            }
        }

        [Obsolete("Use add_portal instead")]
        [ConCommand(commandName = "add_celestial", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleporter will attempt to spawn a celestial portal on completion")]
        private static void CCAddCelestialOrb(ConCommandArgs _)
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
            }
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = "Change team to Neutral, Player or Monster (0, 1, 2)")]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);

            CharacterMaster master = args.sender.master;
            if (args.Count > 1)
            {
                NetworkUser player = GetNetUserFromString(args[1]);
                if (player != null)
                {
                    master = player.master;
                }
            }

            if (Enum.TryParse(args[0], true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    if (master.GetBody())
                    {
                        master.GetBody().teamComponent.teamIndex = teamIndex;
                        master.teamIndex = teamIndex;
                        Debug.Log("Changed to team " + teamIndex);
                    }
                }
            }
        }

        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawn a player")]
        private static void RespawnPlayer(ConCommandArgs args)
        {
            CharacterMaster master = args.sender.master;
            if (args.Count > 0)
            {
                NetworkUser player = GetNetUserFromString(args[0]);
                if (player != null)
                {
                    master = player.master;
                }
            }

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            master.Respawn(spawnPoint.position, spawnPoint.rotation, false);
        }
    }
}
