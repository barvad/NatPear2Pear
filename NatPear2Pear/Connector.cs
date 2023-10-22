using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NatPear2Pear
{
    public class Connector : IConnector, IStMachineSubscriber, IDisposable
    {
        private readonly object _syncObj = new object();
        private readonly UdpClient _udpClient;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<Connection>> _completitionSources =
            new ConcurrentDictionary<string, TaskCompletionSource<Connection>>();
        public ConcurrentBag<StateMachine> _stateMachines = new ConcurrentBag<StateMachine>();
        private bool _disposed;
        private IMessageSerializator _messageSerializator;
        private readonly Settings _settings;
        private bool _recvStarted;

        public Connector(IMessageSerializator messageSerializator, Settings settings)
        {
            _messageSerializator = messageSerializator;
            _settings = settings;
            _udpClient = new UdpClient();
            _udpClient.AllowNatTraversal(true);
        }

        public async Task<Connection> Connect(string remotePeerName)
        {
            if (_completitionSources.ContainsKey(remotePeerName) &&
                !_completitionSources[remotePeerName].Task.IsCompleted)
                return await _completitionSources[remotePeerName].Task;

            // ReSharper disable once InconsistentlySynchronizedField
            if (!_recvStarted)
                lock (_syncObj)
                {
                    if (!_recvStarted) RecvAsync();
                }
            var msg = new Peer2PeerMessage
            { MessageType = Per2PeerMessageType.ConnectRequestToHub, PeerName = remotePeerName };
            var stMachine = new StateMachine() { RemotePeerName = remotePeerName };
            stMachine.Subscribe(this);
            var buf = _messageSerializator.SerializeMessage(msg);

            await _udpClient.SendAsync(buf, buf.Length, _settings.HubAddr);
            _stateMachines.Add(stMachine);
            var ret = _completitionSources[remotePeerName] = new TaskCompletionSource<Connection>();
            return await ret.Task;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public void OnStateChanged(State currentState, State oldState, Peer2PeerMessage message, StateMachine stateMachine)
        {
            byte[] buf;
            switch (currentState)
            {
                case State.New:
                    break;
                case State.ConnectRequestSendedToHub:
                    
                    Task.Delay(_settings.TimeOutForChangeState).ContinueWith(t =>
                    {
                        if (stateMachine.CurrentState == State.ConnectRequestSendedToHub)
                            _completitionSources[stateMachine.RemotePeerName].SetException(new TimeoutException("Time out exception, no response from hub"));
                    });
                    break;
                case State.ResponseReceivedFromHub:
                     buf = _messageSerializator.SerializeMessage(new Peer2PeerMessage { MessageType = Per2PeerMessageType.HelloMessage });
                    _udpClient.SendAsync(buf, buf.Length, message.GetEndPoint());
                    _udpClient.SendAsync(buf, buf.Length, message.GetEndPoint());
                    _udpClient.SendAsync(buf, buf.Length, message.GetEndPoint());
                    break;
                case State.Connected:
                    buf = _messageSerializator.SerializeMessage(new Peer2PeerMessage { MessageType = Per2PeerMessageType.HelloMessage });
                    _udpClient.SendAsync(buf, buf.Length,IPEndPoint.Parse(stateMachine.RemoteEndPoint));
                    _completitionSources[stateMachine.RemotePeerName].SetResult(/*Save connection and return*/((Connection)null)??throw new NotImplementedException());
                    break;
                default:
                    _completitionSources[stateMachine.RemotePeerName].SetException(new NatPeer2PeerConnectionException($"Unknown state {currentState}"));
                    break;
            }
        }

        private async Task RecvAsync()
        {
            while (!_disposed)
            {
                UdpReceiveResult res;
                try
                {
                    res = await _udpClient.ReceiveAsync();
                }
                catch
                {
                    return;
                }

                var msg = _messageSerializator.DeserializeMessage(res.Buffer);
                StateMachine stateMachine = null;
                stateMachine = msg.MessageType == Per2PeerMessageType.ResponseFromHub
                    ? _stateMachines.FirstOrDefault(x => x.RemoteEndPoint == msg.GetEndPoint().ToString())
                    : _stateMachines.FirstOrDefault(x => x.RemotePeerName == msg.PeerName);
                if (stateMachine != null) stateMachine.PassInput(msg.MessageType, msg);
            }
        }
    }
}