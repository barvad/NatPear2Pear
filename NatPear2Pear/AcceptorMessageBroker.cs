using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatPear2Pear
{
    public class AcceptorResultMessageBroker : IAcceptorResultMessageBroker
    {
        readonly object _syncObj = new object();
        Queue<TaskCompletionSource<Connection>> _taskCompletionSources = new Queue<TaskCompletionSource<Connection>>();
        Queue<Task<Connection>>  _resultsQueue = new Queue<Task<Connection>>();
        public Task<Connection> GetResult()
        {
            lock (_syncObj)
            {
                if (_resultsQueue.Count >0)
                {
                    return   _resultsQueue.Dequeue();
                }
                else
                {
                    var tcs = new TaskCompletionSource<Connection>();
                    _taskCompletionSources.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }
        public void SetResult(Connection connection)
        {
            lock (_syncObj)
            {
                if (_taskCompletionSources.Count > 0)
                {
                    var tcs = _taskCompletionSources.Dequeue();
                    tcs.SetResult(connection);
                }
                else
                {
                    _resultsQueue.Enqueue(Task.FromResult(connection));
                }
            }
        }

        public void SetError(Exception exception)
        {
            lock (_syncObj)
            {
                if (_taskCompletionSources.Count > 0)
                {
                    var tcs = _taskCompletionSources.Dequeue();
                    tcs.SetException(exception);
                }
                else
                {
                    _resultsQueue.Enqueue(Task.FromException<Connection>(exception));
                }
            }

        }

       



    }
}
