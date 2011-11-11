namespace Common
{
    using System;

    public class DBFileNotSupportedException : Exception
    {
        public DBFileNotSupportedException(string message) : base(message)
        {
        }
    }
}

