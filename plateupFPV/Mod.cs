using KitchenLib.Event;
using KitchenLib;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KitchenLib.Preferences;
using Kitchen;
using StreamerPack;

namespace KitchenFirstPersonView
{
    public class Mod : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "QuackAndCheese.PlateUp.FirstPersonView";
        public const string MOD_NAME = "First Person View";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "QuackAndCheese";
        public const string MOD_GAMEVERSION = ">=1.1.4";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        #region Preferences
        public const string FOV_ID = "fov";
        public const string SENSITIVITY_ID = "sensitivity";
        public const string FPV_ENABLED_ID = "firstPersonCamera";
        #endregion

        public static Dictionary<string, int> DefaultValuesDict;
        internal static PreferenceManager PrefManager;
        internal static PreferenceFloat SensitivityPreference = new PreferenceFloat(SENSITIVITY_ID, 5.0f);
        internal static PreferenceInt FOVPreference = new PreferenceInt(FOV_ID, 90);

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        public static AssetBundle Bundle;

        public Mod() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // TODO: Uncomment the following if you have an asset bundle.
            // TODO: Also, make sure to set EnableAssetBundleDeploy to 'true' in your ModName.csproj

            LogInfo("Attempting to load asset bundle...");
            Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).First();
            LogInfo("Done loading asset bundle.");

            PrefManager = new PreferenceManager(MOD_GUID);

            PrefManager.RegisterPreference(SensitivityPreference);
            PrefManager.RegisterPreference(FOVPreference);


            ModsPreferencesMenu<PauseMenuAction>.RegisterMenu("First Person View", typeof(FirstPersonViewMenu<PauseMenuAction>), typeof(PauseMenuAction));

            Events.PreferenceMenu_PauseMenu_CreateSubmenusEvent += (s, args) => {
                args.Menus.Add(typeof(FirstPersonViewMenu<PauseMenuAction>), new FirstPersonViewMenu<PauseMenuAction>(args.Container, args.Module_list));
            };
        }

        private void CreatePreferencesNew()
        {
            /*DefaultValuesDict = new Dictionary<string, int>()
            {
                { FOV_ID, 90 },
                { SENSITIVITY_ID, 5 },
                { FPV_ENABLED_ID, 0 }
            };

            PrefManager = new PreferenceManager(MOD_GUID);

            PrefManager
                .AddLabel("First Person View")
                .AddSpacer()
                .AddOption<int>
                (
                    FPV_ENABLED_ID, 
                    0,
                    new int[] { 0, 1 },
                    new string[] { "Disabled", "Enabled" }
                );*/
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
