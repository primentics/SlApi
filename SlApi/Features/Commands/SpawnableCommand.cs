using AzyWorks.Utilities;
using CommandSystem;
using Interactables.Interobjects;
using Mirror;

using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using PlayerRoles.Ragdolls;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SpawnableCommand : ICommand
    {
        public static HashSet<GameObject> Spawnables = new HashSet<GameObject>();

        public string Command { get; } = "spawnable";
        public string[] Aliases { get; } = new string[] { "spawna", "spa" };
        public string Description { get; } = "Provides access to spawnable objects.";

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
                response = "Missing arguments! spawnable <list/spawn/despawn/scale>";
                return false;
            }

            switch (arguments.At(0).ToLower())
            {
                case "list":
                    var builder = new StringBuilder();
                    var objs = new List<GameObject>(NetworkManager.singleton.spawnPrefabs);

                    objs.AddRange(RagdollManager.CachedRagdollPrefabs.Select(x => x.gameObject));
                    objs.AddRange(NetworkClient.prefabs.Select(x => x.Value));

                    if (Round.IsRoundStarted)
                    {
                        objs.Add(BreakableDoor.AllDoors.First().As<BreakableDoor>()._brokenPrefab.gameObject);

                        foreach (var pair in ElevatorManager.SpawnedChambers)
                        {
                            var prefab = ElevatorManager.FindObjectOfType<ElevatorManager>().GetChamberForGroup(pair.Key);

                            if (prefab != null)
                            {
                                objs.Add(prefab.gameObject);
                            }
                        }
                    }

                    builder.AppendLine($"Spawnable objects ({objs.Count})");

                    foreach (var obj in objs)
                    {
                        builder.AppendLine($"   - [{obj.tag}] {obj.name}");
                    }
                   
                    response = builder.ToString();
                    return true;

                case "spawn":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! spawnable spawn <name>";
                            return false;
                        }

                        var obj = NetworkManager.singleton.spawnPrefabs.FirstOrDefault(x => x.name == string.Join(" ", arguments.Skip(1)));

                        if (obj is null)
                        {
                            response = "Failed to find an object with said name.";
                            return true;
                        }

                        var copy = UnityEngine.Object.Instantiate(obj);

                        NetworkServer.Spawn(copy);

                        Spawnables.Add(copy);

                        response = $"Success! Spawned a {copy.name} with ID {copy.GetInstanceID()}";
                        return true;
                    }

                case "despawn":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! spawnable despawn <id>";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid arguments! spawnable despawn <id>";
                            return false;
                        }

                        var spawnable = Spawnables.FirstOrDefault(x => x.GetInstanceID() == id);

                        if (spawnable is null)
                        {
                            response = $"Object with ID {id} has not been found.";
                            return false;
                        }

                        Spawnables.RemoveWhere(x => x.GetInstanceID() == id);

                        NetworkServer.Destroy(spawnable);

                        response = $"Despawned object with ID {id}";
                        return true;
                    }

                case "scale":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "Missing arguments! spawnable scale <id> <x,y,z>";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid arguments! spawnable despawn <id>";
                            return false;
                        }

                        var spawnable = Spawnables.FirstOrDefault(xx => xx.GetInstanceID() == id);

                        if (spawnable is null)
                        {
                            response = $"Object with ID {id} has not been found.";
                            return false;
                        }

                        string[] scaleParts = arguments.At(2).Split(',');

                        if (scaleParts.Length != 3)
                        {
                            response = "Missing arguments! spawnable scale <id> <x,y,z>";
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

                        NetworkServer.UnSpawn(spawnable);

                        spawnable.transform.localScale = newScale;

                        NetworkServer.Spawn(spawnable);

                        response = $"Scaled object with ID {id} to {x}, {y}, {z}";
                        return true;
                    }

                case "tpto":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! spawnable tpto <id>";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid arguments! spawnable tpto <id>";
                            return false;
                        }

                        var spawnable = Spawnables.FirstOrDefault(x => x.GetInstanceID() == id);

                        if (spawnable is null)
                        {
                            response = $"Object with ID {id} has not been found.";
                            return false;
                        }

                        var pos = spawnable.transform.position;

                        pos.y += 2f;

                        senderPly.Position = pos;
                        senderPly.Rotation = spawnable.transform.rotation.eulerAngles;

                        response = $"Teleported you to object with ID {id}";
                        return true;
                    }

                case "tp":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! spawnable tp <id>";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid arguments! spawnable tp <id>";
                            return false;
                        }

                        var spawnable = Spawnables.FirstOrDefault(x => x.GetInstanceID() == id);

                        if (spawnable is null)
                        {
                            response = $"Object with ID {id} has not been found.";
                            return false;
                        }

                        var pos = senderPly.Position;
                        var rot = Quaternion.Euler(senderPly.Rotation);

                        NetworkServer.UnSpawn(spawnable);

                        spawnable.transform.position = pos;
                        spawnable.transform.rotation = rot;
                        spawnable.transform.localPosition = pos;
                        spawnable.transform.localRotation = rot;

                        NetworkServer.Spawn(spawnable);

                        response = $"Teleported object with ID {id} to you.";
                        return true;
                    }

                default:
                    response = "Invalid action.";
                    return false;
            }
        }
    }
}
