using System;
using System.Net;

namespace NatPear2Pear
{
    [Serializable]
    public class Peer2PeerMessage
    {
        public Per2PeerMessageType MessageType { get; set; }
        public string PeerName { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public Exception Exception { get; set; }

        public IPEndPoint GetEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(Ip), Port);
        }
    }
}