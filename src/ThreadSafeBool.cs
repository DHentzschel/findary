﻿using System.Threading;

namespace Findary
{
    public class ThreadSafeBool
    {
        private int _threadSafeBoolBackValue;

        public bool Value
        {
            get => Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 1) == 1;
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 0);
                }
                else
                {
                    Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 0, 1);
                }
            }
        }
    }
}
