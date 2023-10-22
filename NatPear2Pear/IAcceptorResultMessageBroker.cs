using System;
using System.Threading.Tasks;

namespace NatPear2Pear
{
    public interface IAcceptorResultMessageBroker
    {  Task<Connection> GetResult();
        void SetResult(Connection connection);

        void SetError(Exception exception);
    }
}