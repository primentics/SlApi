using PlayerRoles;

using SlApi.Configs;
using SlApi.Extensions;

using UnityEngine;

namespace SlApi.Features.PlayerStates.RocketStates
{
    public class RocketState : PlayerStateBase
    {
        private Vector3 _basePos;
        private float _maxY;

        [Config("RocketState.HeightMultiplier", "Sets the multiplier used to determine maximum height of a rocket.")]
        public static float HeightMultiplier { get; set; } = 2.3f;

        [Config("RocketState.Steps", "Sets the value added to the target's Y axis (higher value = faster ascension).")]
        public static float Steps { get; set; } = 0.05f;

        public RocketState(ReferenceHub target) : base(target)
        {

        }

        public override bool CanUpdateState()
            => Target != null && Target.IsAlive();

        public override void OnDied()
            => IsActive = false;

        public override bool IsFinished()
            => !CanUpdateState();

        public override void OnRoleChanged()
            => IsActive = false;

        public override bool ShouldClearOnDeath()
            => true;

        public override bool ShouldClearOnRoleChange()
            => true;

        public override void OnAdded()
        {
            _basePos = Target.GetRealPosition();
            _maxY = _basePos.y * HeightMultiplier;
        }

        public override void UpdateState()
        {
            _basePos.y += Steps;

            if (_basePos.y >= _maxY)
            {
                Target.UseDisruptor();
                return;
            }

            Target.SetPosition(_basePos);
        }
    }
}