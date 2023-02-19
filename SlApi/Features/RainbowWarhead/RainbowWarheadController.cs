using AzyWorks.Randomization.Weighted;

using HarmonyLib;

using MEC;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Features.ColorHelpers;

using System.Collections.Generic;

using UnityEngine;

namespace SlApi.Features.RainbowWarhead
{
    [HarmonyPatch(typeof(AlphaWarheadController), nameof(AlphaWarheadController.StartDetonation))]
    public static class RainbowWarheadController
    {
        public static bool RainbowLightsDecided;

        [Config("RainbowWarhead.Chance", "The chance of a rainbow warhead occuring.")]
        public static int Chance = 25;

        public static ColorFader RainbowColor;

        static RainbowWarheadController()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.WarheadStop, OnWarheadStopped));
        }

        public static void Postfix(AlphaWarheadController __instance, bool isAutomatic = false, bool suppressSubtitles = false, ReferenceHub trigger = null)
        {
            Timing.CallDelayed(0.5f, () =>
            {
                if (AlphaWarheadController.InProgress)
                {
                    if (!RainbowLightsDecided)
                        PickChance();
                }
            });
        }

        private static void OnWarheadStopped(object[] args)
        {
            OnRoundRestart(args);
        }

        private static void OnRoundRestart(object[] args)
        {
            if (!RainbowLightsDecided || RainbowColor is null)
                return;

            RainbowLightsDecided = false;
            RainbowColor.Stop();
            RainbowColor.OnColorChanged -= DoRainbowLights;
            RainbowColor = null;

            foreach (var light in FlickerableLightController.Instances)
            {
                light.Network_warheadLightColor = Color.red;
                light.Network_warheadLightOverride = false;
            }
        }

        private static void DoRainbowLights(Color newColor)
        {
            foreach (var light in FlickerableLightController.Instances)
            {
                light.Network_warheadLightColor = newColor;
                light.Network_warheadLightOverride = true;
                light.Network_lightIntensityMultiplier = 1f;
            }
        }

        private static void PickChance()
        {
            var dict = new Dictionary<bool, int>(2);

            dict[true] = Chance;
            dict[false] = 100 - Chance;

            if (WeightPicker.Pick(dict, x => x.Value).Key)
            {
                RainbowColor = new ColorFader();
                RainbowColor.OnColorChanged += DoRainbowLights;
                RainbowLightsDecided = true;
                RainbowColor.Start();
            }
        }
    }
}
