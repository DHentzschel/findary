using System.Threading;

namespace Findary
{
    public class ThreadSafeUInt
    {
        private long _value;

        public uint Value
        {
            get => (uint)Interlocked.CompareExchange(ref _value, 0, 0);
            set => Interlocked.Exchange(ref _value, value);
        }
    }
}
