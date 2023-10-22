using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NatPear2Pear
{
    public class Acceptor : IAcceptor, IStMachineSubscriber,IDisposable
    {
        private readonly IFormatter _formatter;
        private readonly UdpClient _udpClient;

        private readonly Dictionary<State, int> _retryCounters = new Dictionary<State, int>();
        private readonly Settings _settings;
        private readonly string _acceptorPeerName;
        private readonly IAcceptorResultMessageBroker _acceptorResultMessageBroker;
        
       

        private bool _recvStarted;
        private readonly object _syncObj=new object();
        ConcurrentQueue<TaskCompletionSource<Connection>> _taskCompletionSourcesQueue = new ConcurrentQueue<TaskCompletionSource<Connection>>();
        private ConcurrentDictionary<string,StateMachine> _stateMachines;

        public Acceptor(Settings settings, IFormatter formatter, UdpClient udpClient, string acceptorPeerName, IAcceptorResultMessageBroker acceptorResultMessageBroker)
        {
            _settings = settings;
            _formatter = formatter;
            _udpClient = udpClient;
            _udpClient.AllowNatTraversal(true);
            _acceptorPeerName = acceptorPeerName;
            _acceptorResultMessageBroker = acceptorResultMessageBroker;
        }

        

        public async Task<Connection> Accept()
        {

            if (!_recvStarted)
            {
                lock (_syncObj)
                {
                    if (!_recvStarted)
                    {
                        RecvAsync();
                    }
                }
            }
            return await _acceptorResultMessageBroker.GetResult();

        }

        private async void RecvAsync()
        {
            while (true)
            {
                UdpReceiveResult res;
           
                    Prolong();
                    StateMachine stateMachine=null;
                
                    res = await _udpClient.ReceiveAsync();  
                    var msg = DeserializeMessage(res.Buffer);
                //TODO: сделать в StateMachine метод который возвращает удовлетворяет текущая машина состояний тоиму что к нам пришло
                stateMachine =
                    _stateMachines[res.RemoteEndPoint.ToString()];
                    if (stateMachine == null && !res.RemoteEndPoint.Equals(_settings.HubAddr))
                    {
                        stateMachine = new StateMachine();
                    _stateMachines[res.RemoteEndPoint.ToString()] = stateMachine;
                        stateMachine.PassInput(Per2PeerMessageType.RegisterPeer);
                        continue;
                    }

                    if (stateMachine == null && res.RemoteEndPoint.Equals(_settings.HubAddr) &&
                        msg.MessageType == Per2PeerMessageType.ConnectRequestToPeer)
                    {
                        stateMachine = _stateMachines[ new IPEndPoint(IPAddress.Parse(msg.Ip), msg.Port).ToString()];
                    }
                    //TODO: сделать в машине состояний (или подумать еще где-то) метод OnProlong который будет будет прокидывать RegisterPeerOk
                    else if (stateMachine == null && res.RemoteEndPoint.Equals(_settings.HubAddr) &&
                             msg.MessageType == Per2PeerMessageType.RegisterPeerOk)
                    {
                    //TODO: подумать как избавиться 
                        foreach (var sm in _stateMachines.Values.Where(s => s.CurrentState == State.RegisterRequestSendedToHub))
                            sm.PassInput(msg.MessageType, msg);
                        continue;
                    }

                 stateMachine.PassInput(msg.MessageType, msg);
            }
        }

        private Peer2PeerMessage DeserializeMessage(byte[] buff)
        {
            try
            {
                Peer2PeerMessage msg;
                using Stream stream = new MemoryStream(buff);
                msg = _formatter.Deserialize(stream) as Peer2PeerMessage;

                return msg;
            }
            catch (Exception ex)
            {
                return new Peer2PeerMessage { Exception = ex };
            }
        }

        

        private byte[] SerializeMessage(Peer2PeerMessage msg)
        {
            using var stream = new MemoryStream();
            _formatter.Serialize(stream, msg);
            return stream.ToArray();
        }

        private async void SetTimeout(StateMachine stateMachine, State currentState, Action act)
        {
            _retryCounters[currentState] = _retryCounters.ContainsKey(currentState)
                ? _retryCounters[currentState]
                : 1;
            await Task.Delay(_settings.TimeOutForChangeState);
            if (currentState == stateMachine.CurrentState
                && _retryCounters[currentState] < _settings.MaxAttempts)
            {
                _retryCounters[currentState]++;
                act?.Invoke();
            }
        }

        public void OnStateChanged(State currentState, State oldState, Peer2PeerMessage message, StateMachine stateMachine)
        {
            switch (currentState)
            {
                case State.RegisterRequestSendedToHub:
                    SetTimeout(stateMachine,currentState, () =>
                    {
                        var message = SerializeMessage(new Peer2PeerMessage
                        {
                            MessageType = Per2PeerMessageType.RegisterPeer,
                            PeerName = _acceptorPeerName
                        });
                        _udpClient.SendAsync(message, message.Length, _settings.HubAddr);
                        stateMachine.PassInput(Per2PeerMessageType.RegisterPeer);
                    });
                    break;
                case State.RegisteredOnHub:
                    break;
                case State.ConnectRequestAccepted:
                    var buf = SerializeMessage(new Peer2PeerMessage
                    {
                        Ip = message.Ip,
                        Port = message.Port,
                        MessageType = Per2PeerMessageType.RequestAcceptedMessage,
                        PeerName = _acceptorPeerName
                    });
                    _udpClient.SendAsync(buf, buf.Length, _settings.HubAddr);
                    _udpClient.Connect(message.GetEndPoint());
                    buf = SerializeMessage(new Peer2PeerMessage
                    { MessageType = Per2PeerMessageType.HelloMessage, PeerName = _acceptorPeerName });
                    _udpClient.SendAsync(buf, buf.Length);
                    Thread.Sleep(200);
                    _udpClient.SendAsync(buf, buf.Length);
                    Thread.Sleep(200);
                    _udpClient.SendAsync(buf, buf.Length);
                    break;
                case State.Connected:
                    buf = SerializeMessage(new Peer2PeerMessage
                        { MessageType = Per2PeerMessageType.HelloMessage,PeerName=_acceptorPeerName });
                    _udpClient.SendAsync(buf, buf.Length);

                    _acceptorResultMessageBroker.SetResult(new Connection() { });
                    break;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async void Prolong()
        {
            var message = SerializeMessage(new Peer2PeerMessage
            {
                MessageType = Per2PeerMessageType.RegisterPeer,
                PeerName = _acceptorPeerName
            });
            await _udpClient.SendAsync(message, message.Length, _settings.HubAddr);
           
        }
    }
}