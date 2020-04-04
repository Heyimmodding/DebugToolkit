using BepInEx;
using RoR2;
using BepInEx.Configuration;
using MiniRpcLib;
using LogLevel = DebugToolkit.Log.LogLevel;
using DebugToolkit.Commands;

namespace DebugToolkit
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.wildbook.libminirpc")]
    [BepInPlugin(GUID, modname, modver)]
    public class DebugToolkit : BaseUnityPlugin
    {
        public const string modname = "DebugToolkit", modver = "3.3.0";
        public const string GUID = "com.harbingerofme." + modname;

        internal static ConfigFile Configuration;

        internal static MiniRpcLib.Action.IRpcAction<float> TimeScaleNetwork;

        private void Awake()
        {
            Configuration = base.Config;

            var miniRpc = MiniRpc.CreateInstance(GUID);
            new Log(Logger, miniRpc);

            #region Not Release Message
#if !RELEASE   //Additional references in this block must be fully qualifed as to not use them in Release Builds.
            string gitVersion = "";
            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"{this.GetType().Namespace}.CurrentCommit"))
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                gitVersion= reader.ReadToEnd();
            }
            Log.MessageWarning(
#if DEBUG       
                $"This is a debug build!"
#elif BLEEDING  
                $"This is a Bleeding-Edge build!"
#endif          
                ,Log.Target.Bepinex);
            Log.MessageWarning($"Commit: {gitVersion.Trim()}",Log.Target.Bepinex);
#endif
            #endregion

            Log.Message("Created by Harb, iDeathHD and . Based on RoR2Cheats by Morris1927.", LogLevel.Info, Log.Target.Bepinex);

            PermissionSystem.Init();
            Hooks.InitializeHooks();
            Command_Noclip.InitRPC(miniRpc);
            Command_Teleport.InitRPC(miniRpc);
            TimeScaleNetwork = miniRpc.RegisterAction(Target.Client, (NetworkUser _, float f) => { CurrentRun.HandleTimeScale(f); });
        }

        private void Start()
        {
            var _ = StringFinder.Instance;
            ArgsAutoCompletion.GatherCommandsAndFillStaticArgs();
        }

        private void Update()
        {
            if (Run.instance && Command_Noclip.IsActivated)
            {
                Command_Noclip.Update();
            }
        }

        static void InvokeCMD(NetworkUser user, string commandname, params object[] arguments)
        {
            RoR2.Console.instance.SubmitCmd(user, string.Join(" ", commandname, arguments));
        }


        /// <summary>
        /// Required for automated manifest building.
        /// </summary>
        /// <returns>Returns the TS manifest Version</returns>
        public static string GetModVer()
        {
            return modver;
        }
    }
}