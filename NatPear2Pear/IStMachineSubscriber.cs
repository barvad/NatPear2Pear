namespace NatPear2Pear
{
    public interface IStMachineSubscriber
    {
        void OnStateChanged(State currentState, State oldState, Peer2PeerMessage message, StateMachine stateMachine);
    }
}