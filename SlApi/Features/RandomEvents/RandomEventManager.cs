using AzyWorks.Randomization.Weighted;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Features.RandomEvents.Events;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Features.RandomEvents
{
    public static class RandomEventManager
    {
        private static float _timeScalar = 0f;
        private static bool _pauseUpdate;

        private static RandomEventBase _lastEvent;
        private static RandomEventBase _currentEvent;

        public static HashSet<RandomEventBase> AllEvents { get; } = new HashSet<RandomEventBase>()
        {
            new Scp575Event(),
            new RandomBlackoutEvent()
        };

        [Config("RandomEvents.UpdateInterval", "The interval between each update for all events.")]
        public static float UpdateInterval { get; set; } = 1f;

        static RandomEventManager()
        {
            StaticUnityMethods.OnUpdate += OnUpdate;

            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.WaitingForPlayers, OnWaitingForPlayers));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        public static void StartEvent(RandomEventBase randomEvent)
        {
            _pauseUpdate = true;
            _currentEvent = randomEvent;
            _lastEvent = randomEvent;

            randomEvent.LastTime = DateTime.Now;
            randomEvent.DoEvent();

            _pauseUpdate = false;
            _currentEvent = null;
        }

        public static void CheckSpawnableEvents()
        {
            var chosenEvents = AllEvents.Where(x =>
                                       x.CheckSpawnInterval()
                                    && x.CheckRoundState()
                                    && x.CanDoEvent());

            if (chosenEvents.Any())
            {
                var chosenEvent = WeightPicker.Pick(chosenEvents, x => _lastEvent != null
                                                    && x.Id == _lastEvent.Id ?
                                                       Mathf.CeilToInt(x.Chance / 2) :
                                                       x.Chance);

                if (chosenEvent != null)
                {
                    StartEvent(chosenEvent);
                    return;
                }
            }
        }

        private static void OnUpdate()
        {
            if (!Round.IsRoundStarted || _currentEvent != null || _pauseUpdate)
                return;

            _timeScalar += Time.deltaTime;

            if (_timeScalar >= UpdateInterval)
            {
                CheckSpawnableEvents();
                _timeScalar = 0f;
            }    
        }

        private static void OnRoundRestart(object[] args)
        {
            _pauseUpdate = true;
            _lastEvent = null;
            _currentEvent = null;
        }

        private static void OnWaitingForPlayers(object[] args)
        {
            _pauseUpdate = false;
            _timeScalar = 0f;
        }
    }
}