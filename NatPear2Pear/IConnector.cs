using System.Threading.Tasks;

namespace NatPear2Pear
{
    public interface IConnector
    {
        Task<Connection> Connect(string remotePeerName);
    }
}