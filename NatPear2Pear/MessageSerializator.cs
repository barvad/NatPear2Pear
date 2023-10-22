using System.IO;
using System.Runtime.Serialization;

namespace NatPear2Pear
{
    public class MessageSerializator : IMessageSerializator
    {
        private readonly IFormatter _formatter;

        public MessageSerializator(IFormatter formatter)
        {
            _formatter = formatter;
        }

        public Peer2PeerMessage DeserializeMessage(byte[] buff)
        {
            Peer2PeerMessage msg;
            using Stream stream = new MemoryStream(buff);
            msg = _formatter.Deserialize(stream) as Peer2PeerMessage;

            return msg;
        }

        public byte[] SerializeMessage(Peer2PeerMessage msg)
        {
            byte[] buf;
            using var stream = new MemoryStream();
            _formatter.Serialize(stream, msg);
            buf = new byte[stream.Length];
            return stream.ToArray();
        }
    }
}