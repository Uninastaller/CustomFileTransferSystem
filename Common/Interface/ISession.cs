namespace Common.Interface
{
    public interface ISession
    {
        public bool SendAsync(byte[] buffer, long offset, long size);
        public string IpAndPort { get; }
    }
}
