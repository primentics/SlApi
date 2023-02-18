using Mirror;

using System;

namespace SlApi.Dummies
{
    public class DummyConnection : NetworkConnectionToClient
    {
        public override string address => "localhost";

        public override void Send(ArraySegment<byte> segment, int channelId = 0) { }

        public DummyConnection(int id) : base(id, false, 0f) { }
    }
}