using AzyWorks.System;
using AzyWorks.Utilities;

using NorthwoodLib.Pools;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;

using PluginAPI.Core;

using SlApi.Extensions;
using SlApi.Features.CustomStats.Stats;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FakeRoleStates;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutState : PlayerStateBase
    {
        private Dictionary<CustomLoadoutCharacterModifier, CustomLoadoutCharacterModifierUpdateState> _activeModifiers;
        private HashSet<CustomLoadoutCharacterModifier> _removeNextFrame;

        private CustomHealthStat _hpStat;
        private AhpStat _ahpStat;

        public CustomLoadoutState(ReferenceHub target) : base(target) { }

        public override bool CanUpdateState()
            => _activeModifiers.Count > 0;

        public override void OnRoleChanged()
        {
            _activeModifiers.Clear();
            _hpStat = Target.playerStats.GetModule<HealthStat>() as CustomHealthStat;
            _ahpStat = Target.playerStats.GetModule<AhpStat>();
        }

        public override void OnDied()
        {
            _activeModifiers.Clear();

            _hpStat = null;
            _ahpStat = null;
        }

        public override void OnAdded()
        {
            _activeModifiers = new Dictionary<CustomLoadoutCharacterModifier, CustomLoadoutCharacterModifierUpdateState>();
            _removeNextFrame = HashSetPool<CustomLoadoutCharacterModifier>.Shared.Rent();

            CustomLoadoutsController.OnLoadoutApplied += OnLoadoutApplied;
            CustomLoadoutsController.OnModifierApplied += OnModifierApplied;
        }

        public override void DisposeState()
        {
            _activeModifiers.Clear();
            _activeModifiers = null;

            _hpStat = null;
            _ahpStat = null;

            HashSetPool<CustomLoadoutCharacterModifier>.Shared.Return(_removeNextFrame);

            CustomLoadoutsController.OnLoadoutApplied -= OnLoadoutApplied;
            CustomLoadoutsController.OnModifierApplied -= OnModifierApplied;
        }

        public override void UpdateState()
        {
            if (_removeNextFrame.Count > 0)
            {
                for (int i = 0; i < _removeNextFrame.Count; i++)
                {
                    var remove = _removeNextFrame.ElementAt(i);
                    if (remove != null)
                        _activeModifiers.Remove(remove);
                }

                _removeNextFrame.Clear();
            }

            foreach (var modifier in _activeModifiers)
            {
                if (!modifier.Value.EverUpdated)
                {
                    if (!modifier.Value.IsPermanent)
                    {
                        ParseModifierValue(modifier.Key, modifier.Value);
                        UpdateSpecificModifier(modifier.Key, modifier.Value);
                        modifier.Value.EverUpdated = true;
                    }
                    else
                    {
                        ParseModifierValue(modifier.Key, modifier.Value);
                        UpdatePermanentModifier(modifier.Key, modifier.Value);
                        modifier.Value.EverUpdated = true;
                    }
                }
                else
                {
                    if (modifier.Value.ParsedValue is null && !modifier.Value.ValueParseAttempted)
                        ParseModifierValue(modifier.Key, modifier.Value);

                    if (!modifier.Value.IsPermanent)
                        UpdateSpecificModifier(modifier.Key, modifier.Value);
                    else
                        UpdatePermanentModifier(modifier.Key, modifier.Value);
                }
            }
        }

        private void UpdatePermanentModifier(CustomLoadoutCharacterModifier modifier, CustomLoadoutCharacterModifierUpdateState state)
        {
            if (!state.EverUpdated)
                SetupModifier(modifier);

            if ((DateTime.Now - state.LastUpdate).TotalSeconds < 1)
                return;

            DoModifier(modifier, state);
        }

        private void UpdateSpecificModifier(CustomLoadoutCharacterModifier modifier, CustomLoadoutCharacterModifierUpdateState state)
        {
            if (!state.EverUpdated)
                SetupModifier(modifier);

            if ((DateTime.Now - state.LastUpdate).TotalSeconds < 1)
                return;

            state.DurationTracker--;
            state.LastUpdate = DateTime.Now;

            if (state.DurationTracker <= 0)
            {
                _removeNextFrame.Add(modifier);
                return;
            }

            if (modifier.ValueType != CustomLoadoutCharacterModifierValueType.Permanent)
            {
                if (!state.CustomEverUpdated)
                {
                    DoModifier(modifier, state);

                    state.CustomEverUpdated = true;
                    state.CustomUpdate = DateTime.Now;

                    return;
                }
                else
                {
                    if ((DateTime.Now - state.CustomUpdate).TotalSeconds < 1)
                        return;

                    state.CustomTracker--;
                    state.CustomUpdate = DateTime.Now;

                    if (state.CustomTracker <= 0)
                    {
                        DoModifier(modifier, state);
                        state.CustomTracker = state.CustomTrackerValue;
                        return;
                    }
                }
            }
            else
                DoModifier(modifier, state);
        }

        private void SetupModifier(CustomLoadoutCharacterModifier modifier)
        {

        }

        private void DoModifier(CustomLoadoutCharacterModifier modifier, CustomLoadoutCharacterModifierUpdateState state)
        {
            if (!(Target.roleManager.CurrentRole is IFpcRole) || !Target.IsAlive())
                return;

            switch (modifier.Type)
            {
                case CustomLoadoutCharacterModifierType.ArtificialHealth:
                    {
                        if (state.ParsedValue is null)
                            return;

                        if (_ahpStat is null)
                            _ahpStat = Target.playerStats.GetModule<AhpStat>();

                        var value = state.ParsedValue.As<int>();
                        var process = _ahpStat._activeProcesses.FirstOrDefault();

                        if (process is null)
                            process = _ahpStat.ServerAddProcess(value, value, 1.2f, 0.7f, 0f, false);

                        if (value > process.Limit)
                            process.Limit = value;

                        process.CurrentAmount = value;

                        break;
                    }

                case CustomLoadoutCharacterModifierType.MaxArtificialHealth:
                    {
                        if (state.ParsedValue is null)
                            return;

                        if (_ahpStat is null)
                            _ahpStat = Target.playerStats.GetModule<AhpStat>();

                        var value = state.ParsedValue.As<int>();
                        var ahpProcesses = _ahpStat._activeProcesses;
                        var ahpProcess = ahpProcesses.FirstOrDefault();

                        if (ahpProcess is null)
                            ahpProcess = _ahpStat.ServerAddProcess(value, value, 1.2f, 0.7f, 0f, false);

                        ahpProcess.Limit = value;
                        break;
                    }

                case CustomLoadoutCharacterModifierType.Health:
                    {
                        if (state.ParsedValue is null)
                            return;

                        if (_hpStat is null)
                            _hpStat = Target.playerStats.GetModule<HealthStat>() as CustomHealthStat;

                        _hpStat.ServerHeal(state.ParsedValue.As<int>());
                        break;
                    }

                case CustomLoadoutCharacterModifierType.MaxHealth:
                    {
                        if (state.ParsedValue is null)
                            return;

                        if (_hpStat is null)
                            _hpStat = Target.playerStats.GetModule<HealthStat>() as CustomHealthStat;

                        _hpStat.OverrideMaxValue(state.ParsedValue.As<int>());
                        break;
                    }

                case CustomLoadoutCharacterModifierType.FakeRole:
                    {
                        if (!Target.TryGetState<FakeRoleState>(out var roleState))
                            Target.TryAddState((roleState = new FakeRoleState(Target)));

                        if (state.ParsedValue is null)
                            return;

                        roleState.FakeRole(state.ParsedValue.As<RoleTypeId>());
                        break;
                    }
            }
        }

        private void ParseModifierValue(CustomLoadoutCharacterModifier modifier, CustomLoadoutCharacterModifierUpdateState state)
        {
            switch (modifier.Type)
            {
                case CustomLoadoutCharacterModifierType.ArtificialHealth:
                case CustomLoadoutCharacterModifierType.MaxHealth:
                case CustomLoadoutCharacterModifierType.Health:
                    ParseInt(modifier.Value, state);
                    break;

                case CustomLoadoutCharacterModifierType.FakeRole:
                    ParseRoleType(modifier.Value, state);
                    break;
            }
        }

        private void ParseRoleType(string input, CustomLoadoutCharacterModifierUpdateState state)
        {
            state.ValueParseAttempted = true;

            if (string.IsNullOrWhiteSpace(input))
            {
                Log.Warning("Failed to parse RoleTypeId - empty input.");
                return;
            }

            if (int.TryParse(input, out var value))
            {
                try
                {
                    var role = (RoleTypeId)value;

                    state.ParsedValue = role;
                }
                catch { Log.Warning($"Failed to parse RoleTypeId - invalid input."); }
            }
            else if (Enum.TryParse<RoleTypeId>(input, true, out var role))
            {
                state.ParsedValue = role;
            }
        }

        private void ParseInt(string input, CustomLoadoutCharacterModifierUpdateState state)
        {
            state.ValueParseAttempted = true;

            if (string.IsNullOrWhiteSpace(input))
            {
                Log.Warning("Failed to parse Int32 - empty input.");
                return;
            }

            if (!int.TryParse(input, out var value))
            {
                Log.Warning("Failed to parse Int32 - invalid input.");
                return;
            }

            state.ParsedValue = value;
        }

        private void OnModifierApplied(ReferenceHub hub, CustomLoadoutCharacterModifier modifier)
        {
            if (hub.netId != Target.netId)
                return;
            else if (modifier.Duration is CustomLoadoutCharacterModifierDuration.Instant)
                return;
            else if (modifier.Duration is CustomLoadoutCharacterModifierDuration.Permanent)
                _activeModifiers[modifier] = new CustomLoadoutCharacterModifierUpdateState(true, -1, modifier.ValueInterval);
            else if (modifier.Duration is CustomLoadoutCharacterModifierDuration.Specified)
                _activeModifiers[modifier] = new CustomLoadoutCharacterModifierUpdateState(false, modifier.DurationValue, modifier.ValueInterval);

            Target.ConsoleMessage($"[Custom Loadouts] Applied modifier: {modifier.Type} ({modifier.Value})");
        }

        private void OnLoadoutApplied(ReferenceHub hub, CustomLoadout customLoadout)
        {
            if (hub.netId != Target.netId)
                return;

            Target.ConsoleMessage($"[Custom Loadouts] Applied loadout: {customLoadout.Name}");
        }
    }
}
