namespace NatPear2Pear
{
    public interface IMessageSerializator
    {
        Peer2PeerMessage DeserializeMessage(byte[] buff);
        byte[] SerializeMessage(Peer2PeerMessage msg);
    }
}