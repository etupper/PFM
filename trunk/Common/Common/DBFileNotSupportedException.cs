namespace Common
{
    using System;

    public class DBFileNotSupportedException : Exception
    {
        public DBFile DbFile { get; set; }

        public DBFileNotSupportedException(DBFile file)
            : this(string.Format("DB File {0} not supported", file.PackedFile.Filepath), file) {}

        public DBFileNotSupportedException(string message, DBFile file)
            : base(message)
        {
            DbFile = file;
        }
    }
}

