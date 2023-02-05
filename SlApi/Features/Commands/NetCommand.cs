using CommandSystem;

using Mirror;

using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class NetCommand : ICommand
    {
        public static readonly string[] BlacklistedNames = new string[]
        {
            "All",
            "RespawnManager",
            "RESPAWN EFFECTS CONTROLLER",
            "Lights",
            "Light Controller Bottom",
            "Light Controller Top",
            "Config Synchronizer",
            "Obj",
            "FlickerableLightController",
            "NukesiteScript",
            "Player",
            "OutsitePanelScript",
            "StartRound",
            "Summary",
            "B272sa",
            "GameManager",
            "DecontaminationManager",
            "Tesla Gate Controller",
            "SCP-914 Controller",
            "TestElevator",
            "Teleport",
            "Intercom handler"
        };

        public string Command { get; } = "netspawn";
        public string[] Aliases { get; } = new string[] { "nspawn", "netsp" };
        public string Description { get; } = "Provides access to objects with network identity.";

        public static List<NetworkIdentity> GetViableObjects(string name)
        {
            return GetObjects()
                .Where(x => x.name.ToLower() == name.ToLower())
                .ToList();
        }

        public static NetworkIdentity GetObject(uint id)
        {
            return GetObjects()
                .FirstOrDefault(x => x.GetInstanceID() == id);
        }

        public static List<NetworkIdentity> GetObjects()
        {
            return NetworkIdentity.spawned.Values
                .Where
                (x => x.isActiveAndEnabled 
                    && !BlacklistedNames.Any(y => x.name.ToLower().Contains(y.ToLower())))
                .ToList();
        }

        public static List<NetworkIdentity> SortByDistance(Vector3 pos)
        {
            return GetObjects()
                .OrderBy(x => Vector3.Distance(x.transform.position, pos))
                .ToList();
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player senderPly = Player.Get(sender);

            if (senderPly is null)
            {
                response = "Failed to get your Player component.";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Missing arguments! netspawn <list/spawn/despawn/scale>";
                return false;
            }

            switch (arguments.At(0).ToLower())
            {
                case "list":
                    var builder = new StringBuilder();
                    var objs = SortByDistance(senderPly.Position);

                    builder.AppendLine($"Objects with network identity, sorted by closest to you: ({objs.Count()})");

                    foreach (var obj in objs)
                    {
                        builder.AppendLine($"   - [{obj.GetInstanceID()}] {obj.name} {obj.transform.position.ToPreciseString()}");
                    }

                    response = builder.ToString();
                    return true;

                case "spawn":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! netspawn spawn <id>";
                            return false;
                        }

                        if (!uint.TryParse(arguments.At(1), out var netId))
                        {
                            response = "Invalid arguments! netspawn spawn <id>";
                            return false;
                        }

                        var netObj = GetObject(netId);

                        if (netObj is null)
                        {
                            response = $"Failed to find an object with ID {netId}.";
                            return true;
                        }

                        NetworkServer.Spawn(netObj.gameObject);

                        response = $"Success! Spawned an object with ID {netId} at {netObj.transform.position}";
                        return true;
                    }

                case "despawn":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! netspawn despawn <id>";
                            return false;
                        }


                        if (!uint.TryParse(arguments.At(1), out var netId))
                        {
                            response = "Invalid arguments! netspawn despawn <id>";
                            return false;
                        }

                        var netObj = GetObject(netId);

                        if (netObj is null)
                        {
                            response = $"Failed to find an object with ID {netId}.";
                            return true;
                        }

                        NetworkServer.UnSpawn(netObj.gameObject);

                        response = $"Despawned object with ID {netId}";
                        return true;
                    }

                case "scale":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "Missing arguments! netspawn scale <id> <x,y,z>";
                            return false;
                        }

                        if (!uint.TryParse(arguments.At(1), out var netId))
                        {
                            response = "Invalid arguments! netspawn scale <id> <x,y,z>";
                            return false;
                        }

                        var netObj = GetObject(netId);

                        if (netObj is null)
                        {
                            response = $"Failed to find an object with ID {netId}.";
                            return true;
                        }

                        string[] scaleParts = arguments.At(2).Split(',');

                        if (scaleParts.Length != 3)
                        {
                            response = "Missing arguments! netspawn scale <id> <x,y,z>";
                            return false;
                        }

                        if (!float.TryParse(scaleParts[0], out var x))
                        {
                            response = "Invalid value for the X axis.";
                            return false;
                        }

                        if (!float.TryParse(scaleParts[1], out var y))
                        {
                            response = "Invalid value for the Y axis.";
                            return false;
                        }

                        if (!float.TryParse(scaleParts[2], out var z))
                        {
                            response = "Invalid value for the Z axis.";
                            return false;
                        }

                        var newScale = new Vector3(x, y, z);

                        NetworkServer.UnSpawn(netObj.gameObject);

                        netObj.transform.localScale = newScale;

                        NetworkServer.Spawn(netObj.gameObject);

                        response = $"Scaled object with ID {netId} to {x}, {y}, {z}";
                        return true;
                    }

                case "tpto":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! netspawn tpto <id>";
                            return false;
                        }

                        if (!uint.TryParse(arguments.At(1), out var netId))
                        {
                            response = "Invalid arguments! netspawn tpto <id>";
                            return false;
                        }

                        var netObj = GetObject(netId);

                        if (netObj is null)
                        {
                            response = $"Failed to find an object with ID {netId}.";
                            return true;
                        }

                        var pos = netObj.transform.position;

                        pos.y += 2f;

                        senderPly.Position = pos;
                        senderPly.Rotation = netObj.transform.rotation.eulerAngles;

                        response = $"Teleported you to object with ID {netId}";
                        return true;
                    }

                case "tp":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! netspawn tpto <id>";
                            return false;
                        }

                        if (!uint.TryParse(arguments.At(1), out var netId))
                        {
                            response = "Invalid arguments! netspawn tpto <id>";
                            return false;
                        }

                        var netObj = GetObject(netId);

                        if (netObj is null)
                        {
                            response = $"Failed to find an object with ID {netId}.";
                            return true;
                        }

                        var pos = senderPly.Position;
                        var rot = Quaternion.Euler(senderPly.Rotation);
                        var scale = senderPly.ReferenceHub.transform.localScale;

                        NetworkServer.UnSpawn(netObj.gameObject);

                        netObj.transform.position = pos;
                        netObj.transform.rotation = rot;
                        netObj.transform.localPosition = pos;
                        netObj.transform.localRotation = rot;
                        netObj.transform.localScale = scale;

                        NetworkServer.Spawn(netObj.gameObject);

                        response = $"Teleported object with ID {netId} to you.";
                        return true;
                    }

                default:
                    response = "Invalid action.";
                    return false;
            }
        }
    }
}
