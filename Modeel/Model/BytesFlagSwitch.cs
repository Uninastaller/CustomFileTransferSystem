using Modeel.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public class BytesFlagSwitch
    {

        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private Dictionary<byte[], Action<byte[], long, long>> _binding;
        private Action _onNonRegisteredAction;

        #endregion PrivateFields

        #region Ctor

        public BytesFlagSwitch()
        {
            _binding = new Dictionary<byte[], Action<byte[], long, long>>(new ByteArrayEqualityComparer());
        }

        #endregion Ctor

        #region PublicMethods

        public void OnNonRegistered(Action handler)
        {
            _onNonRegisteredAction = handler;
        }

        public void Register(SocketMessageFlag flag, Action<byte[], long, long> handler)
        {
            byte[] flagBytes = Encoding.UTF8.GetBytes(flag.GetStringValue());
            if (!_binding.ContainsKey(flagBytes))
            {
                _binding.Add(flagBytes, handler);
            }
            else
            {
                Logger.WriteLog($"Warning: Handler for flag '{flag}' is already registered.", LoggerInfo.warning);
            }
        }

        public void Switch(byte[] flag, byte[] buffer, long offset, long size)
        {
            if (_binding.TryGetValue(flag, out Action<byte[], long, long>? handler))
            {
                handler.Invoke(buffer, offset, size);
            }
            else
            {
                _onNonRegisteredAction?.Invoke();
            }
        }

        #endregion PublicMethods

        #region PrivateMethods



        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler



        #endregion EventHandler

        #region OverridedMethods



        #endregion OverridedMethods

        public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                if (x == null || y == null)
                    return false;

                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (byte b in obj)
                    {
                        hash = hash * 31 + b.GetHashCode();
                    }
                    return hash;
                }
            }
        }
    }
}
