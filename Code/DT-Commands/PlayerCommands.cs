﻿using RoR2;
using System;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class PlayerCommands
    {
        [ConCommand(commandName = "god", flags = ConVarFlags.ExecuteOnServer, helpText = "Become invincible. " + Lang.GOD_ARGS)]
        private static void CCGodModeToggle(ConCommandArgs args)
        {
            bool hasNotYetRun = true;
            foreach (var playerInstance in PlayerCharacterMasterController.instances)
            {
                playerInstance.master.ToggleGod();
                if (hasNotYetRun)
                {
                    Log.MessageNetworked($"God mode {(playerInstance.master.GetBody().healthComponent.godMode ? "enabled" : "disabled")}.", args);
                    hasNotYetRun = false;
                }
            }
        }

        [ConCommand(commandName = "noclip", flags = ConVarFlags.ExecuteOnServer, helpText = "Allow flying and going through objects. Sprinting will double the speed. " + Lang.NOCLIP_ARGS)]
        private static void CCNoclip(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (Run.instance)
            {
                NoclipNet.Invoke(args.sender); // callback
            }
            else
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "teleport_on_cursor", flags = ConVarFlags.ExecuteOnServer, helpText = "Teleport you to where your cursor is currently aiming at. " + Lang.CURSORTELEPORT_ARGS)]
        private static void CCCursorTeleport(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.MessageWarning(Lang.DS_NOTAVAILABLE);
                return;
            }
            if (Run.instance && args.senderBody)
            {
                TeleportNet.Invoke(args.sender); // callback
            }
            else
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "spawn_as", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawn the specified player using the specified body prefab. " + Lang.SPAWNAS_ARGS)]
        [AutoCompletion(typeof(BodyCatalog), "bodyPrefabBodyComponents", "baseNameToken")]
        private static void CCSpawnAs(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.SPAWNAS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            string character = StringFinder.Instance.GetBodyName(args[0]);
            if (character == null)
            {
                Log.MessageNetworked(Lang.SPAWN_ERROR + args[0], args, LogLevel.MessageClientOnly);
                Log.MessageNetworked("Please use list_body to print CharacterBodies", args, LogLevel.MessageClientOnly);
                return;
            }
            GameObject newBody = BodyCatalog.FindBodyPrefab(character);

            if (args.sender == null && args.Count < 2)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            master.bodyPrefab = newBody;
            Log.MessageNetworked(args.sender.userName + " is spawning as " + character, args);

            if (!master.GetBody())
            {
                Log.MessageNetworked(Lang.PLAYER_DEADRESPAWN, args, LogLevel.MessageClientOnly);
                return;
            }

            RoR2.ConVar.BoolConVar stage1pod = Stage.stage1PodConVar;
            bool oldVal = stage1pod.value;
            stage1pod.SetBool(false);
            master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
            stage1pod.SetBool(oldVal);
        }



        [ConCommand(commandName = "respawn", flags = ConVarFlags.ExecuteOnServer, helpText = "Respawns the specified player. " + Lang.RESPAWN_ARGS)]
        [AutoCompletion(typeof(NetworkUser), "instancesList", "userName", true)]
        //[AutoCompletion(typeof(NetworkUser), "instancesList", "_id/value", true)] // ideathhd : breaks the whole console for me
        private static void RespawnPlayer(ConCommandArgs args)
        {
            if (args.sender == null && args.Count < 1)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }
            CharacterMaster master = args.sender?.master;
            if (args.Count > 0)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Transform spawnPoint = Stage.instance.GetPlayerSpawnTransform();
            master.Respawn(spawnPoint.position, spawnPoint.rotation);
            Log.MessageNetworked(string.Format(Lang.SPAWN_ATTEMPT_1, master.name), args);
        }

        [ConCommand(commandName = "change_team", flags = ConVarFlags.ExecuteOnServer, helpText = "Change the specified player to the specified team. " + Lang.CHANGETEAM_ARGS)]
        private static void CCChangeTeam(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.CHANGETEAM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null && args.Count < 2)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }

            CharacterMaster master = args.sender?.master;
            if (args.Count > 1)
            {
                NetworkUser player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player != null)
                {
                    master = player.master;
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            if (Enum.TryParse(StringFinder.GetEnumFromPartial<TeamIndex>(args[0]).ToString(), true, out TeamIndex teamIndex))
            {
                if ((int)teamIndex >= (int)TeamIndex.None && (int)teamIndex < (int)TeamIndex.Count)
                {
                    if (master.GetBody())
                    {
                        master.GetBody().teamComponent.teamIndex = teamIndex;
                        master.teamIndex = teamIndex;
                        Log.MessageNetworked("Changed to team " + teamIndex, args);
                        return;
                    }
                }
            }
            //Note the `return` on succesful evaluation.
            Log.MessageNetworked("Invalid team. Please use 0,'neutral',1,'player',2, or 'monster'", args, LogLevel.MessageClientOnly);

        }

        [ConCommand(commandName = "loadout_set_skin_variant", flags = ConVarFlags.None, helpText = "Change your loadout's skin,  " + Lang.LOADOUTSKIN_ARGS)]
        public static void CCLoadoutSetSkinVariant(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                return;
            }

            if (args.Count != 2)
            {
                Log.Message(Lang.LOADOUTSKIN_ARGS, LogLevel.MessageClientOnly);
                return;
            }

            BodyIndex argBodyIndex = BodyIndex.None;
            bool bodyIsSelf = false;

            if (args.Count > 0)
            {
                if (args.GetArgString(0).ToUpperInvariant() == "SELF")
                {
                    bodyIsSelf = true;
                    if (args.sender == null)
                    {
                        Log.Message("Can't choose self if not in-game!", LogLevel.Error);
                        return;
                    }
                    if (args.senderBody)
                    {
                        argBodyIndex = args.senderBody.bodyIndex;
                    }
                    else
                    {
                        if (args.senderMaster && args.senderMaster.bodyPrefab)
                        {
                            argBodyIndex = args.senderMaster.bodyPrefab.GetComponent<CharacterBody>().bodyIndex;
                        }
                        else
                        {
                            argBodyIndex = args.sender.bodyIndexPreference;
                        }
                    }
                }
                else
                {
                    string requestedBodyName = StringFinder.Instance.GetBodyName(args[0]);
                    if (requestedBodyName != null)
                    {
                        argBodyIndex = BodyCatalog.FindBodyIndex(requestedBodyName);
                    }
                }
            }

            int requestedSkinIndexChange = args.GetArgInt(1);

            Loadout loadout = new Loadout();
            UserProfile userProfile = args.GetSenderLocalUser().userProfile;
            userProfile.loadout.Copy(loadout);
            loadout.bodyLoadoutManager.SetSkinIndex(argBodyIndex, (uint)requestedSkinIndexChange);
            userProfile.SetLoadout(loadout);
            if (args.senderMaster)
            {
                args.senderMaster.SetLoadoutServer(loadout);
            }
            if (args.senderBody)
            {
                args.senderBody.SetLoadoutServer(loadout);
                if (args.senderBody.modelLocator && args.senderBody.modelLocator.modelTransform)
                {
                    var modelSkinController = args.senderBody.modelLocator.modelTransform.GetComponent<ModelSkinController>();
                    if (modelSkinController)
                    {
                        modelSkinController.ApplySkin(requestedSkinIndexChange);
                    }
                }
            }

            if (bodyIsSelf && !args.senderBody)
            {
                Log.MessageNetworked(Lang.PLAYER_SKINCHANGERESPAWN, args, LogLevel.MessageClientOnly);
            }
        }



        internal static bool UpdateCurrentPlayerBody(out NetworkUser networkUser, out CharacterBody characterBody)
        {
            networkUser = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            characterBody = null;

            if (networkUser)
            {
                var master = networkUser.master;

                if (master && master.GetBody())
                {
                    characterBody = master.GetBody();
                    return true;
                }
            }

            return false;
        }
    }
}
