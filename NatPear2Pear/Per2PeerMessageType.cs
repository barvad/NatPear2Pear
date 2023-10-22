using System;

namespace NatPear2Pear
{
    [Serializable]
    public enum Per2PeerMessageType
    {
        ConnectRequestToHub,
        ResponseFromHub,
        NetworkException,
        HelloMessage,
        RegisterPeer,
        ConnectRequestToPeer,
        RegisterPeerOk,

        /// <summary>
        ///     Accept message from acceptor to hub
        /// </summary>
        RequestAcceptedMessage
    }
}