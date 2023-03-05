using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Mirror;

namespace SlApi.Extensions
{
    public static class NetworkExtensions 
    {
        public static readonly Dictionary<Type, MethodInfo> WriterExtensions;
        public static readonly Dictionary<string, ulong> SyncVarDirtyBits;

        static NetworkExtensions() 
        {
            WriterExtensions = new Dictionary<Type, MethodInfo>();
            SyncVarDirtyBits = new Dictionary<string, ulong>();

            var writerExtensions = typeof(NetworkExtensions);
            var gCode = typeof(GeneratedNetworkCode);
            var console = typeof(ServerConsole);
            var assembly = console.Assembly;
            var types = assembly.GetTypes();
            var serializerTypes = types.Where(x => x.Name.StartsWith("Serializer"));

            foreach (var method in writerExtensions.GetMethods()) 
            {
                var parameters = method.GetParameters();
                if (!method.IsGenericMethod && (parameters != null && parameters.Length == 2)) 
                    WriterExtensions.Add(parameters.First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);
            }

            foreach (var method in gCode.GetMethods()) 
            {
                var parameters = method.GetParameters();
                if (!method.IsGenericMethod && (parameters != null && parameters.Length == 2) && method.ReturnType == typeof(void)) 
                {
                    WriterExtensions.Add(parameters.First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);
                }
            }

            foreach (var serializer in serializerTypes) {
                foreach (var method in serializer.GetMethods()) {
                    if (method.ReturnType == typeof(void) && method.Name.StartsWith("Write"))
                        WriterExtensions.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);
                }
            }

            foreach (var property in types.SelectMany(x => x.GetProperties()).Where(y => y.Name.StartsWith("Network"))) {
                var setMethod = property.GetSetMethod();
                if (setMethod is null)
                    continue;

                var body = setMethod.GetMethodBody();
                if (body is null)
                    continue;

                var bytecode = body.GetILAsByteArray();
                if (bytecode is null)
                    continue;

                if (!SyncVarDirtyBits.ContainsKey(property.Name))
                    SyncVarDirtyBits.Add(property.Name, bytecode[bytecode.LastIndexOf((byte)OpCodes.Ldc_I8.Value) + 1]);
            }
        }

        public static void FakeSyncVar(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string propertyName, object value) {
            
            void CustomSyncVarGenerator(NetworkWriter targetWriter) {
                targetWriter.WriteUInt64(SyncVarDirtyBits[propertyName]);

                WriterExtensions[value.GetType()]?.Invoke(null, new[] { targetWriter, value });
            }

            var writer = NetworkWriterPool.GetWriter();
            var writer2 = NetworkWriterPool.GetWriter();

            MakeWriter(behaviorOwner, targetType, null, CustomSyncVarGenerator, writer, writer2);

            target.networkIdentity.connectionToClient?.Send(new UpdateVarsMessage() 
            {
                netId = behaviorOwner.netId, 
                payload = writer.ToArraySegment() 
            });

            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        public static void ResyncSyncVar(this NetworkIdentity behaviorOwner, string propertyName)
            => behaviorOwner.gameObject.GetComponent<NetworkBehaviour>()?.SetDirtyBit(SyncVarDirtyBits[$"{propertyName}"]);

        public static void FakeRpc(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string rpcName, params object[] values) {
            var writer = NetworkWriterPool.GetWriter();

            foreach (object value in values)
                WriterExtensions[value.GetType()]?.Invoke(null, new[] { writer, value });

            var msg = new RpcMessage
            {
                netId = behaviorOwner.netId,
                componentIndex = GetComponentIndex(behaviorOwner, targetType),
                functionHash = (targetType.FullName.GetStableHashCode() * 503) + rpcName.GetStableHashCode(),
                payload = writer.ToArraySegment()
            };

            target.connectionToClient.Send(msg, 0);

            NetworkWriterPool.Recycle(writer);
        }

        public static void FakeObject(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customAction) {
            var writer = NetworkWriterPool.GetWriter();
            var writer2 = NetworkWriterPool.GetWriter();
            
            MakeWriter(behaviorOwner, targetType, customAction, null, writer, writer2);
            
            target.networkIdentity.connectionToClient?.Send(new UpdateVarsMessage() 
            { 
                netId = behaviorOwner.netId, 
                payload = writer.ToArraySegment() 
            });
            
            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        public static void EditObject(this NetworkIdentity identity, Action<NetworkIdentity> customAction) {
            customAction?.Invoke(identity);

            var objectDestroyMessage = new ObjectDestroyMessage()
            {
                netId = identity.netId
            };

            foreach (var hub in ReferenceHub.AllHubs) {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.connectionToClient?.Send(objectDestroyMessage, 0);

                NetworkServer.SendSpawnMessage(identity, hub.connectionToClient);
            }
        }

        public static int GetComponentIndex(this NetworkIdentity identity, Type type) {
            return Array.FindIndex(identity.NetworkBehaviours, x => x.GetType() == type);
        }

        public static void MakeWriter(this NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer) {
            byte behaviorDirty = 0;
            NetworkBehaviour behaviour = null;

            for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++) {
                if (behaviorOwner.NetworkBehaviours[i].GetType() == targetType) {
                    behaviour = behaviorOwner.NetworkBehaviours[i];
                    behaviorDirty = (byte)i;
                    break;
                }
            }


            owner.WriteByte(behaviorDirty);
            var position = owner.Position;
            owner.WriteInt32(0);
            var position2 = owner.Position;

            if (customSyncObject != null)
                customSyncObject.Invoke(owner);
            else
                behaviour.SerializeObjectsDelta(owner);

            customSyncVar?.Invoke(owner);

            var position3 = owner.Position;
            owner.Position = position;
            owner.WriteInt32(position3 - position2);
            owner.Position = position3;

            if (behaviour.syncMode != SyncMode.Observers) {
                ArraySegment<byte> arraySegment = owner.ToArraySegment();
                observer.WriteBytes(arraySegment.Array, position, owner.Position - position);
            }
        }
    }
}
