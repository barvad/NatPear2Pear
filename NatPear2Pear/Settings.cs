using System.Net;

namespace NatPear2Pear
{
    public class Settings
    {
        public IPEndPoint HubAddr { get; set; }
        public int TimeOutForChangeState { get; set; } = 2000;
        public int MaxAttempts { get; set; } = 3;
        
    }
}