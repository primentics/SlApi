using SlApi.Extensions;
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

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (RainbowWarheadController.BlacklistedUsers.Contains(hub.characterClassManager.UserId))
                    continue;

                _light.SetRoomColorForTargetOnly(hub, _origColor);
            }

            _light = null;
            _color = null;
        }

        private void OnColorChanged(Color newColor)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (RainbowWarheadController.BlacklistedUsers.Contains(hub.characterClassManager.UserId))
                    continue;

                _light.SetRoomColorForTargetOnly(hub, newColor);
            }
        }
    }
}