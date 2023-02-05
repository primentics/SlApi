namespace SlApi.Features.PlayerStates
{
    public class PlayerStateBase
    {
        public ReferenceHub Target { get; }

        public bool IsActive { get; set; }

        public PlayerStateBase(ReferenceHub target)
        {
            Target = target;
        }

        public virtual void OnAdded()
        {

        }

        public virtual void OnRoleChanged()
        {

        }

        public virtual void OnDied()
        {

        }

        public virtual void UpdateState()
        {

        }

        public virtual void DisposeState()
        {

        }

        public virtual bool CanUpdateState()
        {
            return false;
        }

        public virtual bool ShouldClearOnRoleChange()
        {
            return false;
        }

        public virtual bool ShouldClearOnDeath()
        {
            return false;
        }

        public virtual bool IsFinished()
        {
            return false;
        }
    }
}