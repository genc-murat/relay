using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Circular buffer for efficient time-series storage
    /// </summary>
    internal class CircularBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>, IDisposable
    {
        private readonly T[] _buffer;
        private int _start;
        private int _count;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

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
                _lock.EnterReadLock();
                try
                {
                    if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
                    return _buffer[(_start + index) % _buffer.Length];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
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
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _start = 0;
                _count = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveFront(int count)
        {
            if (count < 0 || count > _count) throw new ArgumentOutOfRangeException(nameof(count));
            _lock.EnterWriteLock();
            try
            {
                _start = (_start + count) % _buffer.Length;
                _count -= count;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public T[] ToArray()
        {
            _lock.EnterReadLock();
            try
            {
                var result = new T[_count];
                var resultSpan = result.AsSpan();
                if (_start == 0)
                {
                    _buffer.AsSpan(0, _count).CopyTo(resultSpan);
                }
                else
                {
                    var firstPart = _buffer.Length - _start;
                    if (_count <= firstPart)
                    {
                        _buffer.AsSpan(_start, _count).CopyTo(resultSpan);
                    }
                    else
                    {
                        _buffer.AsSpan(_start, firstPart).CopyTo(resultSpan);
                        _buffer.AsSpan(0, _count - firstPart).CopyTo(resultSpan.Slice(firstPart));
                    }
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }



        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            T[] snapshot;
            int start, count;
            try
            {
                snapshot = (T[])_buffer.Clone();
                start = _start;
                count = _count;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            for (int i = 0; i < count; i++)
            {
                int index = (start + i) % snapshot.Length;
                yield return snapshot[index];
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
