using CommandSystem;

using PlayerRoles;

using PluginAPI.Core;


using SlApi.Dummies;
using SlApi.Extensions;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DummyCommand : ICommand
    {
        public string Command { get; } = "dummy";
        public string Description { get; } = "dummy <spawn/despawn/role/scale/teleport/rotate/tpto/item/follow>";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = Description;
                return false;
            }

            var player = Player.Get(sender)?.ReferenceHub;

            switch (arguments.At(0).ToLower())
            {
                case "spawn":
                    {
                        var dummy = new DummyPlayer(player);

                        response = $"Spawned a new dummy with ID {dummy.DummyId}.";
                        return true;
                    }

                case "despawn":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "dummy despawn <ID>";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        dummy.Destroy();

                        response = "Dummy despawned.";
                        return true;
                    }

                case "role":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "dummy role <ID> <role>";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        if (!Enum.TryParse<RoleTypeId>(arguments.At(2), out var role))
                        {
                            response = "Invalid role ID.";
                            return false;
                        }

                        dummy.RoleId = role;

                        response = $"Set role of {id} to {role}.";
                        return true;
                    }

                case "scale":
                    {
                        if (arguments.Count < 5)
                        {
                            response = "dummy scale <ID> <X> <Y> <Z>";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(2), out var x))
                        {
                            response = "Invalid X.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(3), out var y))
                        {
                            response = "Invalid Y.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(4), out var z))
                        {
                            response = "Invalid Z.";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        dummy.Scale = new UnityEngine.Vector3(x, y, z);

                        response = $"Scaled {id} to {x} {y} {z}";
                        return true;
                    }

                case "teleport":
                    {
                        if (arguments.Count < 5)
                        {
                            response = "dummy teleport <ID> <X> <Y> <Z>";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(2), out var x))
                        {
                            response = "Invalid X.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(3), out var y))
                        {
                            response = "Invalid Y.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(4), out var z))
                        {
                            response = "Invalid Z.";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        dummy.Position = new UnityEngine.Vector3(x, y, z);

                        response = $"Teleported {id} to {x} {y} {z}";
                        return true;
                    }

                case "rotate":
                    {
                        if (arguments.Count < 6)
                        {
                            response = "dummy rotate <ID> <X> <Y> <Z> <W>";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(2), out var x))
                        {
                            response = "Invalid X.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(3), out var y))
                        {
                            response = "Invalid Y.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(4), out var z))
                        {
                            response = "Invalid Z.";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(5), out var w))
                        {
                            response = "Invalid W.";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        dummy.Rotation = new UnityEngine.Quaternion(x, y, z, w);

                        response = $"Rotated {id} to {x} {y} {z} {w}";
                        return true;
                    }

                case "tpto":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "dummy tpto <ID>";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        player.SetPosition(dummy.Position);

                        response = $"Teleported you to {id}";
                        return true;
                    }

                case "item":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "dummy item <ID> <item>";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        if (!Enum.TryParse<ItemType>(arguments.At(2), out var item))
                        {
                            response = "Invalid item ID.";
                            return false;
                        }

                        dummy.CurItem = item;

                        response = $"Set item of {id} to {item}";
                        return true;
                    }

                case "follow":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "dummy follow <ID> <target>";
                            return false;
                        }

                        if (!byte.TryParse(arguments.At(1), out var id))
                        {
                            response = "Invalid ID.";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummy(id, out var dummy))
                        {
                            response = "Dummy has not been found.";
                            return false;
                        }

                        if (!HubExtensions.TryGetHub(arguments.At(2), out var target))
                        {
                            response = "Target has not been found.";
                            return false;
                        }

                        if (dummy.IsFollowing(out var curTarget) && curTarget.netId == target.netId)
                        {
                            dummy.Follow(null);

                            response = $"{id} stopped following.";
                            return true;
                        }
                        else
                        {
                            dummy.Follow(target);

                            response = $"{id} is now following {target.nicknameSync.MyNick}";
                            return true;
                        }
                    }

                default:
                    {
                        response = "Invalid operation.";
                        return false;
                    }
            }
        }
    }
}