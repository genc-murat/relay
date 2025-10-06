using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Circular buffer for efficient time-series storage
    /// </summary>
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _start;
        private int _count;
        private readonly object _lock = new object();

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _start = 0;
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;

        public void Add(T item)
        {
            lock (_lock)
            {
                if (_count < _buffer.Length)
                {
                    _buffer[_count] = item;
                    _count++;
                }
                else
                {
                    _buffer[_start] = item;
                    _start = (_start + 1) % _buffer.Length;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _start = 0;
                _count = 0;
            }
        }

        public T[] ToArray()
        {
            lock (_lock)
            {
                var result = new T[_count];
                if (_count < _buffer.Length)
                {
                    Array.Copy(_buffer, 0, result, 0, _count);
                }
                else
                {
                    var firstPart = _buffer.Length - _start;
                    Array.Copy(_buffer, _start, result, 0, firstPart);
                    Array.Copy(_buffer, 0, result, firstPart, _start);
                }
                return result;
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return ToArray().Where(predicate);
        }
    }
}
