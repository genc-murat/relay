using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Circular buffer for efficient time-series storage
    /// </summary>
    internal class CircularBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>
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

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
                    return _buffer[(_start + index) % _buffer.Length];
                }
            }
        }

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

        public void RemoveFront(int count)
        {
            if (count < 0 || count > _count) throw new ArgumentOutOfRangeException(nameof(count));
            lock (_lock)
            {
                _start = (_start + count) % _buffer.Length;
                _count -= count;
            }
        }

        public T[] ToArray()
        {
            lock (_lock)
            {
                var result = new T[_count];
                var resultSpan = result.AsSpan();
                if (_count < _buffer.Length)
                {
                    _buffer.AsSpan(0, _count).CopyTo(resultSpan);
                }
                else
                {
                    var firstPart = _buffer.Length - _start;
                    _buffer.AsSpan(_start, firstPart).CopyTo(resultSpan);
                    _buffer.AsSpan(0, _start).CopyTo(resultSpan.Slice(firstPart));
                }
                return result;
            }
        }



        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                for (int i = 0; i < _count; i++)
                {
                    int index = (_start + i) % _buffer.Length;
                    yield return _buffer[index];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
