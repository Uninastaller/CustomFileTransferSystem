using Common.Enum;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Model
{
    public class BytesFlagSwitch
    {

        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private Dictionary<byte[], Action<byte[], long, long>> _binding;
        private Action<string>? _onNonRegisteredAction;

        //private byte[] _cache = new byte[Settings.Default.FlagSwitchCache];
        private int _partSize;
        private Buffer _cache = new Buffer();
        private Action<byte[], long, long>? _cachingAction;
        private int _defaultPartSize;
        private const int _flagBytesCount = 3;

        #endregion PrivateFields

        #region Ctor

        public BytesFlagSwitch()
        {
            _binding = new Dictionary<byte[], Action<byte[], long, long>>(new ByteArrayEqualityComparer());
        }

        #endregion Ctor

        #region PublicMethods

        public void SetLastPartSize(int size)
        {
            _partSize = size;
            Log.WriteLog(LogLevel.WARNING, $"Setting part size for last part, size is now: {_partSize}");
        }

        public void SetCaching(int partSize, Action<byte[], long, long> cachingAction)
        {
            _defaultPartSize = partSize;
            _partSize = partSize;
            _cache.Reserve(partSize * 2);
            _cachingAction = cachingAction;
        }

        public void OnNonRegistered(Action<string> handler)
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
                Log.WriteLog(LogLevel.WARNING, $"Warning: Handler for flag '{flag}' is already registered.");
            }
        }

        public void Switch(byte[] buffer, long offset, long size)
        {
            // Check if message is long enought to have flag
            if (size < 3)
            {
                Log.WriteLog(LogLevel.WARNING, $"Warning, received message with too few bytes, size: {size}");
                _onNonRegisteredAction?.Invoke(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
            }
            // Try found action by message flag
            else if (_binding.TryGetValue(buffer.Skip((int)offset).Take(_flagBytesCount).ToArray(), out Action<byte[], long, long>? action))
            {
                // Action is caching-action and message size is not full file part
                if (action == _cachingAction && (size - _flagBytesCount - sizeof(long)) < _partSize)
                {
                    // Save message for later bcs its not completed
                    _cache.Clear();
                    _cache.Append(buffer, offset, size);
                }
                // Action is not caching action
                else
                {
                    // Invoke coresponding action
                    action.Invoke(buffer, offset, size);
                }
            }
            // Flag is not registered or is missing
            // If there is already something in cache and new message + data in cache are not larger than file part
            else if (_cache.Size > 0 && (_cache.Size + size - _flagBytesCount - sizeof(long) <= _partSize))
            {
                _cache.Append(buffer, offset, size);

                // Check if in cache is not full file part
                if ((_cache.Size - _flagBytesCount - sizeof(long)) == _partSize)
                {
                    _partSize = _defaultPartSize;
                    _cachingAction.Invoke(_cache.Data, 0, _cache.Size);
                    _cache.Clear();
                }
            }
            // Received data are completly garbage
            else
            {
                _onNonRegisteredAction?.Invoke(size < 100000 ? Encoding.UTF8.GetString(buffer, (int)offset, (int)size) : string.Empty);
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
