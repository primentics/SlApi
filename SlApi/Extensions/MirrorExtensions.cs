using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using InventorySystem.Items.Firearms;

using Mirror;

using NorthwoodLib.Pools;

using RelativePositioning;

using Respawning;

using UnityEngine;

namespace SlApi.Extensions
{
    public static class MirrorExtensions
    {
        private static readonly Dictionary<Type, MethodInfo> WriterExtensionsValue = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<string, ulong> SyncVarDirtyBitsValue = new Dictionary<string, ulong>();
        private static readonly ReadOnlyDictionary<Type, MethodInfo> ReadOnlyWriterExtensionsValue = new ReadOnlyDictionary<Type, MethodInfo>(WriterExtensionsValue);
        private static readonly ReadOnlyDictionary<string, ulong> ReadOnlySyncVarDirtyBitsValue = new ReadOnlyDictionary<string, ulong>(SyncVarDirtyBitsValue);

        private static MethodInfo setDirtyBitsMethodInfoValue;
        private static MethodInfo sendSpawnMessageMethodInfoValue;

        public static ReadOnlyDictionary<Type, MethodInfo> WriterExtensions
        {
            get
            {
                if (WriterExtensionsValue.Count == 0)
                {
                    foreach (MethodInfo method in typeof(NetworkWriterExtensions).GetMethods().Where(x => !x.IsGenericMethod && (x.GetParameters()?.Length == 2)))
                        WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);

                    foreach (MethodInfo method in typeof(GeneratedNetworkCode).GetMethods().Where(x => !x.IsGenericMethod && (x.GetParameters()?.Length == 2) && (x.ReturnType == typeof(void))))
                        WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);

                    foreach (Type serializer in typeof(ServerConsole).Assembly.GetTypes().Where(x => x.Name.EndsWith("Serializer")))
                    {
                        foreach (MethodInfo method in serializer.GetMethods().Where(x => (x.ReturnType == typeof(void)) && x.Name.StartsWith("Write")))
                            WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);
                    }
                }

                return ReadOnlyWriterExtensionsValue;
            }
        }

        public static ReadOnlyDictionary<string, ulong> SyncVarDirtyBits
        {
            get
            {
                if (SyncVarDirtyBitsValue.Count == 0)
                {
                    foreach (PropertyInfo property in typeof(ServerConsole).Assembly.GetTypes()
                        .SelectMany(x => x.GetProperties())
                        .Where(m => m.Name.StartsWith("Network")))
                    {
                        MethodInfo setMethod = property.GetSetMethod();

                        if (setMethod is null)
                            continue;

                        MethodBody methodBody = setMethod.GetMethodBody();

                        if (methodBody is null)
                            continue;

                        byte[] bytecodes = methodBody.GetILAsByteArray();

                        if (!SyncVarDirtyBitsValue.ContainsKey($"{property.Name}"))
                            SyncVarDirtyBitsValue.Add($"{property.Name}", bytecodes[bytecodes.LastIndexOf((byte)OpCodes.Ldc_I8.Value) + 1]);
                    }
                }

                return ReadOnlySyncVarDirtyBitsValue;
            }
        }

        public static void PlayBeepSound(this ReferenceHub hub) 
            => SendFakeTargetRpc(hub, ReferenceHub.HostHub.networkIdentity, 
                typeof(AmbientSoundPlayer), nameof(AmbientSoundPlayer.RpcPlaySound), 7);

        public static void SetPlayerInfoForTargetOnly(this ReferenceHub hub, ReferenceHub target, string info) => 
            hub.SendFakeSyncVar(target.networkIdentity, typeof(NicknameSync), 
                nameof(NicknameSync.Network_customPlayerInfoString), info);

        public static void PlayGunSound(this ReferenceHub hub, Vector3 position, ItemType itemType, byte volume, byte audioClipId = 0)
        {
            GunAudioMessage message = new GunAudioMessage()
            {
                Weapon = itemType,
                AudioClipId = audioClipId,
                MaxDistance = volume,
                ShooterHub = hub,
                ShooterPosition = new RelativePosition(position),
            };

            hub.connectionToClient.Send(message);
        }

        public static void SetRoomColorForTargetOnly(this FlickerableLightController lightController, ReferenceHub target, Color color)
        {
            target.SendFakeSyncVar(lightController.netIdentity, typeof(FlickerableLightController), 
                nameof(FlickerableLightController.Network_warheadLightColor), color);
            target.SendFakeSyncVar(lightController.netIdentity, typeof(FlickerableLightController), 
                nameof(FlickerableLightController.Network_warheadLightOverride), true);
        }

        /// <summary>
        /// Sets <see cref="Room.LightIntensity"/> of a <paramref name="room"/> that only the <paramref name="target"/> player can see.
        /// </summary>
        /// <param name="room">Room to modify.</param>
        /// <param name="target">Only this player can see room color.</param>
        /// <param name="multiplier">Light intensity multiplier to set.</param>
        public static void SetRoomLightIntensityForTargetOnly(this FlickerableLightController lightController, ReferenceHub target, float multiplier)
        {
            target.SendFakeSyncVar(lightController.netIdentity, typeof(FlickerableLightController), 
                nameof(FlickerableLightController.Network_lightIntensityMultiplier), multiplier);
        }

        public static void PlayCassieAnnouncement(this ReferenceHub hub, string words, bool makeHold = false, bool makeNoise = true, bool isSubtitles = false)
        {
            foreach (RespawnEffectsController controller in RespawnEffectsController.AllControllers)
            {
                if (controller != null)
                {
                    SendFakeTargetRpc(hub, controller.netIdentity, typeof(RespawnEffectsController), 
                        nameof(RespawnEffectsController.RpcCassieAnnouncement), words, makeHold, makeNoise, isSubtitles);
                }
            }
        }

        public static void MessageTranslated(this ReferenceHub hub, string words, string translation, bool makeHold = false, bool makeNoise = true, bool isSubtitles = true)
        {
            StringBuilder annoucement = StringBuilderPool.Shared.Rent();
            string[] cassies = words.Split('\n');
            string[] translations = translation.Split('\n');
            for (int i = 0; i < cassies.Length; i++)
                annoucement.Append($"{translations[i]}<size=0> {cassies[i].Replace(' ', ' ')} </size><split>");

            foreach (RespawnEffectsController controller in RespawnEffectsController.AllControllers)
            {
                if (controller != null)
                {
                    SendFakeTargetRpc(hub, controller.netIdentity, typeof(RespawnEffectsController), 
                        nameof(RespawnEffectsController.RpcCassieAnnouncement), annoucement, makeHold, makeNoise, isSubtitles);
                }
            }

            StringBuilderPool.Shared.Return(annoucement);
        }

        public static void SendFakeSyncVar(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string propertyName, object value)
        {
            void CustomSyncVarGenerator(NetworkWriter targetWriter)
            {
                targetWriter.WriteUInt64(SyncVarDirtyBits[propertyName]);
                WriterExtensions[value.GetType()]?.Invoke(null, new[] { targetWriter, value });
            }

            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            PooledNetworkWriter writer2 = NetworkWriterPool.GetWriter();

            MakeCustomSyncWriter(behaviorOwner, targetType, null, CustomSyncVarGenerator, writer, writer2);
            target.networkIdentity.connectionToClient.Send(new UpdateVarsMessage() { netId = behaviorOwner.netId, payload = writer.ToArraySegment() });

            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        public static void ResyncSyncVar(NetworkIdentity behaviorOwner, Type targetType, string propertyName)
            => behaviorOwner.gameObject.GetComponent<NetworkBehaviour>()?.SetDirtyBit(SyncVarDirtyBits[$"{propertyName}"]);

        public static void SendFakeTargetRpc(ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string rpcName, params object[] values)
        {
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();

            foreach (object value in values)
                WriterExtensions[value.GetType()].Invoke(null, new[] { writer, value });

            RpcMessage msg = new RpcMessage
            {
                netId = behaviorOwner.netId,
                componentIndex = GetComponentIndex(behaviorOwner, targetType),
                functionHash = (targetType.FullName.GetStableHashCode() * 503) + rpcName.GetStableHashCode(),
                payload = writer.ToArraySegment(),
            };

            target.connectionToClient.Send(msg, 0);

            NetworkWriterPool.Recycle(writer);
        }

        public static void SendFakeSyncObject(ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customAction)
        {
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            PooledNetworkWriter writer2 = NetworkWriterPool.GetWriter();
            MakeCustomSyncWriter(behaviorOwner, targetType, customAction, null, writer, writer2);
            target.networkIdentity.connectionToClient.Send(new UpdateVarsMessage() { netId = behaviorOwner.netId, payload = writer.ToArraySegment() });
            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        public static void EditNetworkObject(NetworkIdentity identity, Action<NetworkIdentity> customAction)
        {
            customAction.Invoke(identity);

            ObjectDestroyMessage objectDestroyMessage = new ObjectDestroyMessage()
            {
                netId = identity.netId,
            };

            foreach (ReferenceHub hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.connectionToClient.Send(objectDestroyMessage, 0);

                NetworkServer.SendSpawnMessage(identity, hub.connectionToClient);
            }
        }

        private static int GetComponentIndex(NetworkIdentity identity, Type type)
        {
            return Array.FindIndex(identity.NetworkBehaviours, (x) => x.GetType() == type);
        }

        private static void MakeCustomSyncWriter(NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer)
        {
            byte behaviorDirty = 0;
            NetworkBehaviour behaviour = null;

            for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++)
            {
                if (behaviorOwner.NetworkBehaviours[i].GetType() == targetType)
                {
                    behaviour = behaviorOwner.NetworkBehaviours[i];
                    behaviorDirty = (byte)i;
                    break;
                }
            }


            owner.WriteByte(behaviorDirty);


            int position = owner.Position;
            owner.WriteInt32(0);
            int position2 = owner.Position;

            if (customSyncObject != null)
                customSyncObject.Invoke(owner);
            else
                behaviour.SerializeObjectsDelta(owner);

            customSyncVar?.Invoke(owner);

            int position3 = owner.Position;
            owner.Position = position;
            owner.WriteInt32(position3 - position2);
            owner.Position = position3;

            if (behaviour.syncMode != SyncMode.Observers)
            {
                ArraySegment<byte> arraySegment = owner.ToArraySegment();
                observer.WriteBytes(arraySegment.Array, position, owner.Position - position);
            }
        }
    }
}
