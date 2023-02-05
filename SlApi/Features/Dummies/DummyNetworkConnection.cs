using Mirror;

using System;

namespace SlApi.Dummies
{
    public class DummyNetworkConnecton : NetworkConnectionToClient
    {
        public DummyNetworkConnecton(int connectionId) : base(connectionId, false, 0f)
        {

        }

        public override string address
        {
            get
            {
                return "localhost";
            }
        }

        public override void Send(ArraySegment<byte> segment, int channelId = 0)
        {

        }

        public override void Disconnect()
        {

        }
    }
}