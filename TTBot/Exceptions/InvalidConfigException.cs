using System;

namespace TTBot.Exceptions
{
    [Serializable]
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException() : base() { }
        public InvalidConfigException(string message) : base(message) { }
    }
    
}
