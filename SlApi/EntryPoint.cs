using HarmonyLib;

using PluginAPI.Core.Attributes;

using SlApi.Configs;
using SlApi.Events;

using System;

namespace SlApi
{
    public class EntryPoint
    {
        private static Harmony _harmony = new Harmony("slapi.entrypoint");

        public static EntryPoint Instance { get; private set; }

        public static event Action OnReloaded;
        public static event Action OnLoaded;
        public static event Action OnUnloading;

        [PluginEntryPoint("SlApi", "2.0.1", "A custom API for SCP: Secret Laboratory servers.", "azyworks")]
        public void Load()
        {
            CosturaUtility.Initialize();

            Instance = this;

            CustomConfigManager.Reload();
            EventHandlers.RegisterBase();

            _harmony.PatchAll();

            OnLoaded?.Invoke();

            Logger.Info("Loaded Sl Api.");
        }

        [PluginReload]
        public void Reload()
        {
            CustomConfigManager.Reload();

            OnReloaded?.Invoke();

            Logger.Info("Reloaded.");
        }

        [PluginUnload]
        public void Unload()
        {
            OnUnloading?.Invoke();

            _harmony.UnpatchAll();

            EventHandlers.UnregisterBase();
        }
    }
}