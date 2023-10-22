using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NatPear2Pear
{
    public class StateMachine
    {
        private readonly Dictionary<KeyValuePair<Per2PeerMessageType, State>, State> _transitionsTable =
            new Dictionary<KeyValuePair<Per2PeerMessageType, State>, State>
            {
                {new KeyValuePair<Per2PeerMessageType, State>(Per2PeerMessageType.ConnectRequestToHub, State.New), State.ConnectRequestSendedToHub},
                {new KeyValuePair<Per2PeerMessageType,State>(Per2PeerMessageType.ResponseFromHub,State.ConnectRequestSendedToHub),State.ResponseReceivedFromHub },
                {new KeyValuePair<Per2PeerMessageType,State>(Per2PeerMessageType.HelloMessage,State.ResponseReceivedFromHub), State.Connected },
                {new KeyValuePair<Per2PeerMessageType, State>(Per2PeerMessageType.RegisterPeer,State.New),State.RegisterRequestSendedToHub  },
                {new KeyValuePair<Per2PeerMessageType, State>(Per2PeerMessageType.RegisterPeerOk,State.RegisterRequestSendedToHub),State.RegisteredOnHub  },
                {new KeyValuePair<Per2PeerMessageType, State>(Per2PeerMessageType.ConnectRequestToPeer,State.RegisteredOnHub),State.ConnectRequestAccepted  },
                {new KeyValuePair<Per2PeerMessageType, State>(Per2PeerMessageType.HelloMessage,State.ConnectRequestAccepted),State.Connected }

            };

        public State CurrentState { get; set; }
        private ConcurrentBag<IStMachineSubscriber> _subscribers=new ConcurrentBag<IStMachineSubscriber>();
        public string RemotePeerName { get; set; }
        public string RemoteEndPoint { get; set; }

        public void Subscribe(IStMachineSubscriber subscriber)
        {
            _subscribers.Add(subscriber);
        }
        public void PassInput(Per2PeerMessageType signal, Peer2PeerMessage message = null)
        {
            var oldState = CurrentState;
            if (_transitionsTable.ContainsKey(new KeyValuePair<Per2PeerMessageType, State>(signal, CurrentState)))
            {
                CurrentState = _transitionsTable[new KeyValuePair<Per2PeerMessageType, State>(signal, CurrentState)];
                _subscribers.ForEach(s => s.OnStateChanged(CurrentState, oldState, message, this));
                
            }

        }
    }
}