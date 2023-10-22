using System;

namespace NatPear2Pear
{
    public class NatPeer2PeerConnectionException : Exception
    {
        public NatPeer2PeerConnectionException()
        {
        }

        public NatPeer2PeerConnectionException(string message)
            : base(message)
        {
        }

        public NatPeer2PeerConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}