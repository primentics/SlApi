using MEC;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SlApi.Features.ColorHelpers
{
    public class ColorFader
    {
        private int _curStep;
        private float _delay = 0f;
        private CoroutineHandle _coroutine;

        public Color[] AllColors { get; } = new Color[]
        {
            new Color(1f, 0f, 0f),
            new Color(1f, 0f, 1f),
            new Color(0f, 0f, 1f),
            new Color(0f, 1f, 1f),
            new Color(0f, 1f, 0f),
            new Color(1f, 1f, 0f),
        };

        public Color Current { get; private set; }

        public bool IsEnabled { get; set; } = true;

        public event Action<Color> OnColorChanged;

        public void Start()
        {
            _coroutine = Timing.RunCoroutine(FadeCoroutine());
        }

        public void Stop()
        {
            IsEnabled = false;

            Timing.KillCoroutines(_coroutine);
        }

        private IEnumerator<float> FadeCoroutine()
        {
            while (IsEnabled)
            {
                if (_curStep + 1 >= AllColors.Length)
                    _curStep = 0;

                Current = Color.Lerp(Current, AllColors[_curStep++], 0.8f);
                OnColorChanged?.Invoke(Current);

                yield return Timing.WaitForSeconds(0.5f);
            }
        }
    }
}