using PlayerStatsSystem;

namespace SlApi.Features.CustomStats.Stats
{
    public class CustomHealthStat : HealthStat
    {
        private float _customMax;
        private bool _customMaxSet;

        public override float MaxValue
        {
            get
            {
                if (!_customMaxSet)
                    return base.MaxValue;

                return _customMax;
            }
        }

        public void OverrideMaxValue(float value)
        {
            _customMax = value;
            _customMaxSet = true;
        }

        public void ResetMaxValue()
        {
            _customMax = 0f;
            _customMaxSet = false;
        }
    }
}
