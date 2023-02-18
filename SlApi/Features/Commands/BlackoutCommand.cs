using CommandSystem;

using MapGeneration;

using SlApi.Extensions;
using SlApi.Features.RandomEvents.Events;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class BlackoutCommand : ICommand
    {
        public string Command { get; } = "cblackout";
        public string Description { get; } = "cblackout <time> (target / zone filter / room filter) (lock doors [true/false])";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = $"cblackout <time> (target / zone filter / room filter) (lock doors [true/false])";
                return false;
            }

            if (!float.TryParse(arguments.At(0), out var time))
            {
                response = $"Failed to parse duration!"; 
                return false;
            }

            bool lockDoors = false;

            if (arguments.Any(x => bool.TryParse(x, out lockDoors)))
                sender.Respond("Doors will be locked.");

            if (arguments.Count is 1)
            {
                RandomBlackoutEvent.DoBlackout(time);

                response = $"Activated blackout for {time} seconds.";
                return true;
            }
            else
            {
                if (HubExtensions.TryGetHub(arguments.At(1), out var targetHub))
                {
                    var room = RoomIdUtils.RoomAtPosition(targetHub.GetRealPosition());

                    if (room is null)
                    {
                        response = $"Failed to identify the room {targetHub.nicknameSync.MyNick} is currently in.";
                        return false;
                    }

                    ServerHelper.DoBlackout(room, time);

                    response = $"Triggered a blackout in {room.Name} for {time} seconds.";
                    return true;
                }
                else
                {
                    if (arguments.At(1).Contains(','))
                    {
                        var values = arguments.At(1).Split(',');

                        if (Enum.TryParse<FacilityZone>(values[0], out _))
                        {
                            var list = new List<FacilityZone>(values.Length);

                            foreach (var value in values)
                            {
                                if (Enum.TryParse<FacilityZone>(value, out var zone))
                                    list.Add(zone);
                            }

                            ServerHelper.DoBlackout(time, lockDoors, list.ToArray());

                            response = $"Triggered blackout for {time} seconds in zones: {string.Join(", ", list)}";
                            return true;
                        }
                        else if (Enum.TryParse<RoomName>(values[0], out _))
                        {
                            var list = new List<RoomName>(values.Length);

                            foreach (var value in values)
                            {
                                if (Enum.TryParse<RoomName>(value, out var zone))
                                    list.Add(zone);
                            }

                            ServerHelper.DoBlackout(time, lockDoors, list.ToArray());

                            response = $"Triggered blackout for {time} seconds in rooms: {string.Join(", ", list)}";
                            return true;
                        }
                        else
                        {
                            response = "Invalid value.";
                            return false;
                        }
                    }
                    else
                    {
                        if (Enum.TryParse<FacilityZone>(arguments.At(1), out var zone))
                        {
                            ServerHelper.DoBlackout(time, lockDoors, new FacilityZone[] { zone });

                            response = $"Triggered blackout in zone {zone} for {time} seconds.";
                            return true;
                        }  
                        else if (Enum.TryParse<RoomName>(arguments.At(1), out var name))
                        {
                            ServerHelper.DoBlackout(time, lockDoors, new RoomName[] {  name });

                            response = $"Triggered blackout in room {name} for {time} seconds.";
                            return true;
                        }
                        else
                        {
                            response = "Invalid value.";
                            return false;
                        }
                    }
                }
            }
        }
    }
}
