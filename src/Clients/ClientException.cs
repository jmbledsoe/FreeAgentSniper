using System;

namespace FreeAgentSniper.Clients
{
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message) {}
    }
}