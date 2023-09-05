using System.Net;

namespace Common.Interface
{
    public interface ISession
    {
        public bool SendAsync(byte[] buffer, long offset, long size);
        public EndPoint Endpoint { get; }
    }
}
