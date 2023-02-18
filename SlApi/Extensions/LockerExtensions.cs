using MapGeneration.Distributors;

namespace SlApi.Extensions
{
    public static class LockerExtensions
    {
        public static void ToggleState(this LockerChamber chamber, Locker locker)
        {
            chamber.SetDoor(!chamber.IsOpen, locker._grantedBeep);
            locker.RefreshOpenedSyncvar();
        }
    }
}