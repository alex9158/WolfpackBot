using System;

namespace WolfpaackBot.Exceptions
{
    [Serializable]
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException() : base() { }
        public InvalidConfigException(string message) : base(message) { }
    }
    
}
