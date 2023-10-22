using System.Net.Sockets;
using System.Threading.Tasks;

namespace NatPear2Pear
{
    public interface IAcceptor
    {
        Task<Connection> Accept();
        void Prolong();  
    }
}