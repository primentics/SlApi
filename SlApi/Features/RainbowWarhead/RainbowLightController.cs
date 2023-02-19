using PluginAPI.Core;

using SlApi.Features.ColorHelpers;

using UnityEngine;

namespace SlApi.Features.RainbowWarhead
{
    public class RainbowLightController
    {
        private FlickerableLightController _light;
        private ColorFader _color;
        private Color _origColor;

        public RainbowLightController(FlickerableLightController light)
        {
            _light = light;
            _color = new ColorFader();
            _origColor = light.Network_warheadLightColor;
            _color.OnColorChanged += OnColorChanged;
            _color.Start();
        }

        public void Stop()
        {
            _color.Stop();
            _color.OnColorChanged -= OnColorChanged;

            _light.Network_warheadLightColor = _origColor;
            _light.Network_warheadLightOverride = false;

            _light = null;
            _color = null;
        }

        private void OnColorChanged(Color newColor)
        {
            _light.Network_warheadLightColor = newColor;
            _light.Network_warheadLightOverride = true;
            _light.Network_lightIntensityMultiplier = 1f;
        }
    }
}