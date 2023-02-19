using AzyWorks.Configuration;
using AzyWorks.Configuration.Converters.Yaml;

using SlApi.Commands;
using SlApi.CustomEvents;
using SlApi.Features.CustomLoadouts;
using SlApi.Features.CustomTesla;
using SlApi.Features.Fixes;
using SlApi.Features.Overwatch;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.AdminVoiceStates;
using SlApi.Features.PlayerStates.FreezeStates;
using SlApi.Features.PlayerStates.InvisibleStates;
using SlApi.Features.PlayerStates.ResizeStates;
using SlApi.Features.PlayerStates.RocketStates;
using SlApi.Features.PlayerStates.SpectateStates;
using SlApi.Features.RainbowWarhead;
using SlApi.Features.RandomEvents;
using SlApi.Features.RandomEvents.Events;
using SlApi.Features.RemoteKeycard;
using SlApi.Features.RespawnTimer;
using SlApi.Features.Scp1162;
using SlApi.Features.Spawnpoints;
using SlApi.Features.Voice;
using SlApi.Features.Voice.AdminVoice;
using SlApi.Features.Voice.Custom;
using SlApi.Patches.Feature;
using SlApi.Voice;

using System;
using System.Collections.Generic;
using System.IO;

namespace SlApi.Configs
{
    public static class CustomConfigManager
    {
        private static Dictionary<string, ConfigHandler> _handlers = new Dictionary<string, ConfigHandler>();

        public static string TopPath { get => $"{PluginAPI.Helpers.Paths.Configs}/SlApi"; }
        public static string VoicePath { get => $"{TopPath}/voice"; }

        public static Dictionary<ConfigType, Type[]> HandlerBuilders = new Dictionary<ConfigType, Type[]>()
        {
            [ConfigType.Main] = new Type[] { typeof(Logger) },

            [ConfigType.Features] = new Type[] { typeof(SpawnpointManager), typeof(RespawnManager_Update_Patch), 
                typeof(PersistentOverwatch), typeof(RemoteCard), typeof(Scp096RageManager_UpdateRage), 
                typeof(DisarmedPlayers_CanDisarmed_Patch), typeof(Escape_ServerHandlePlayer), typeof(Scp1162Controller),
                typeof(CustomTeslaController), typeof(SpawnPositionFix), typeof(RainbowWarheadController) },

            [ConfigType.VoiceGeneral] = new Type[] { typeof(VoiceChatManager), typeof(CustomVoiceProcessor), 
                typeof(CustomVoiceState), typeof(CustomVoiceKeyStateCommand) },

            [ConfigType.VoiceAdmin] = new Type[] { typeof(AdminVoiceProcessor), typeof(AdminVoiceState) },

            [ConfigType.PlayerStates] = new Type[] { typeof(PlayerStateController), typeof(PlayerFreezeState), 
                typeof(InvisibilityState), typeof(ResizeState), typeof(RocketState), typeof(SpectateState) },

            [ConfigType.Commands] = new Type[] { typeof(AdminSpectateCommand), typeof(AdminVoiceCommand), 
                typeof(AdminVoiceChannelCommand), typeof(AudioCommand), typeof(DisintegrateCommand), typeof(FreezeCommand), 
                typeof(GhostCommand), typeof(MuteAudioCommand), typeof(NetCommand), typeof(ResizeCommand), typeof(RocketCommand), 
                typeof(SpawnableCommand), typeof(TargetGhostCommand) },

            [ConfigType.Events] = new Type[] { typeof(CustomEventManager) },

            [ConfigType.RespawnTimer] = new Type[] { typeof(RespawnTimerController) },

            [ConfigType.RandomEvents] = new Type[] { typeof(RandomEventManager), typeof(Scp575Event), typeof(RandomBlackoutEvent) },

            [ConfigType.CustomLoadouts] = new Type[] { typeof(CustomLoadoutsController) }
        };

        public static Dictionary<ConfigType, string> Paths = new Dictionary<ConfigType, string>()
        {
            [ConfigType.Main] = $"{TopPath}/main.ini",
            [ConfigType.Features] = $"{TopPath}/features.ini",
            [ConfigType.VoiceGeneral] = $"{VoicePath}/general.ini",
            [ConfigType.VoiceAdmin] = $"{VoicePath}/admin.ini",
            [ConfigType.PlayerStates] = $"{TopPath}/states.ini",
            [ConfigType.Commands] = $"{TopPath}/commands.ini",
            [ConfigType.Events] = $"{TopPath}/events.ini",
            [ConfigType.RandomEvents] = $"{TopPath}/random_events.ini",
            [ConfigType.RespawnTimer] = $"{TopPath}/respawn_timer.ini",
            [ConfigType.CustomLoadouts] = $"{TopPath}/custom_loadouts.ini"
        };

        static CustomConfigManager()
        {
            if (!Directory.Exists(TopPath)) Directory.CreateDirectory(TopPath);
            if (!Directory.Exists(VoicePath)) Directory.CreateDirectory(VoicePath);

            foreach (var pathPair in Paths)
            {
                var handler = GetHandler(pathPair.Key);

                if (handler is null)
                {
                    Logger.Warn($"[Custom Config Manager] Handler for {pathPair.Key} is null.");
                    continue;
                }

                _handlers[pathPair.Value] = handler;
            }
        }

        public static void Reload()
        {
            foreach (var pair in _handlers)
            {
                if (!File.Exists(pair.Key))
                {
                    pair.Value.SaveToFile(pair.Key);
                    continue;
                }

                pair.Value.LoadFromFile(pair.Key);
                pair.Value.SaveToFile(pair.Key);
            }
        }

        public static ConfigHandler GetHandler(this ConfigType type)
        {
            if (HandlerBuilders.TryGetValue(type, out var types))
            {
                var handler = new ConfigHandler(new YamlConfigConverter());

                foreach (var cfgType in types)
                    handler.RegisterConfigs(cfgType);

                return handler;
            }

            return null;
        }
    }
}