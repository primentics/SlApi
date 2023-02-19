using CommandSystem;

using MapGeneration;

using SlApi.Features.RainbowWarhead;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RainbowLightCommand : ICommand
    {
        public static int Id;
        public static Dictionary<int, HashSet<RainbowLightController>> ActiveRainbow = new Dictionary<int, HashSet<RainbowLightController>>();

        public string Command { get; } = "rainbowlights";
        public string Description { get; } = "rainbowlights <target>";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "rainbowlights <target>";
                return false;
            }

            if (arguments.Count is 1)
            {
                if (!arguments.At(0).Contains(','))
                {
                    if (Enum.TryParse<RoomName>(arguments.At(0), out var name))
                    {
                        var id = DoLight(new RoomName[] { name });

                        response = $"Created a rainbow light controller with ID {id}.";
                        return true;
                    }
                    else if (Enum.TryParse<FacilityZone>(arguments.At(0), out var zone))
                    {
                        var id = DoLight(null, new FacilityZone[] { zone });

                        response = $"Created a rainbow light controller with ID {id}.";
                        return true;
                    }
                    else
                    {
                        response = "Invalid target!";
                        return false;
                    }
                }
                else
                {
                    var splitValues = arguments.At(0).Split(',');

                    if (Enum.TryParse<RoomName>(splitValues[0], out _))
                    {
                        var parsedValues = splitValues
                            .Select(x => Enum.TryParse<RoomName>(x, out var res) ? res : RoomName.Unnamed)
                            .Where(y => y != RoomName.Unnamed)
                            .ToArray();

                        var id = DoLight(parsedValues);
                        response = $"Created a rainbow light controller with ID {id}.";
                        return true;
                    }
                    else if (Enum.TryParse<FacilityZone>(splitValues[0], out _))
                    {
                        var parsedValues = splitValues
                            .Select(x => Enum.TryParse<FacilityZone>(x, out var res) ? res : FacilityZone.None)
                            .Where(y => y != FacilityZone.None)
                            .ToArray();

                        var id = DoLight(null, parsedValues);
                        response = $"Created a rainbow light controller with ID {id}.";
                        return true;
                    }
                    else
                    {
                        response = $"Invalid target!";
                        return false;
                    }
                }
            }
            else if (arguments.Count is 2 && arguments.At(0) is "stop")
            {
                if (!int.TryParse(arguments.At(1), out var lightId))
                {
                    response = "Invalid integer.";
                    return false;
                }

                if (!ActiveRainbow.TryGetValue(lightId, out var light))
                {
                    response = "Invalid light ID";
                    return false;
                }

                foreach (var lightC in light)
                    lightC.Stop();

                ActiveRainbow[lightId].Clear();
                ActiveRainbow.Remove(lightId);

                response = $"Stopped light {lightId}";
                return true;
            }
            else
            {
                response = "rainbowlight <target>";
                return false;
            }
        }

        public int DoLight(RoomName[] roomFilter = null, FacilityZone[] zoneFilter = null)
        {
            Id++;
            int id = Id;
            ActiveRainbow[id] = new HashSet<RainbowLightController>();

            foreach (var flicker in FlickerableLightController.Instances)
            {
                if (flicker.Room != null)
                {
                    if (roomFilter != null && !roomFilter.Contains(flicker.Room.Name))
                        continue;

                    if (zoneFilter != null && !zoneFilter.Contains(flicker.Room.Zone))
                        continue;
                }

                ActiveRainbow[id].Add(new RainbowLightController(flicker));
            }

            return id;
        }
    }
}
