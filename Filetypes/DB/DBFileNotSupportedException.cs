namespace Filetypes
{
    using System;

    public class DBFileNotSupportedException : Exception
    {
        public DBFile DbFile { get; set; }

        public DBFileNotSupportedException(string message) : base(message) {
        }

        public DBFileNotSupportedException(DBFile file)
            : this("DB File not supported", file) {}

		public DBFileNotSupportedException (string message, Exception x)
            : base(message, x) {
		}

        public DBFileNotSupportedException(string message, DBFile file)
            : base(message)
        {
            DbFile = file;
        }
    }
}

