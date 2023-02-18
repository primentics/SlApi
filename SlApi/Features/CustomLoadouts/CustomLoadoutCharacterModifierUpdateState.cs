using System;

namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutCharacterModifierUpdateState
    {
        public bool IsPermanent;
        public bool EverUpdated;
        public bool CustomEverUpdated;
        public bool ValueParseAttempted;

        public int DurationTracker;
        public int CustomTracker;
        public int CustomTrackerValue;

        public DateTime LastUpdate;
        public DateTime CustomUpdate;

        public object ParsedValue;

        public CustomLoadoutCharacterModifierUpdateState(bool isPermanent, int duration, int customTracker)
        {
            IsPermanent = isPermanent;
            DurationTracker = duration;
            EverUpdated = false;
            CustomEverUpdated = false;
            LastUpdate = DateTime.Now;
            CustomUpdate = DateTime.Now;
            CustomTracker = customTracker;
            CustomTrackerValue = customTracker;
        }
    }
}