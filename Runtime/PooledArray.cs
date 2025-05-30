using System;
using System.Runtime.CompilerServices;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    public sealed class PooledArray<T> : IReadOnlyList<T>, IDisposable
    {
        public const int minCapacity = 1024;

        T[] array;
        
        public int Length { get; private set; }
        public Span<T> Span => new(array, 0, Length);

        public T this[int index] => array[index];
        int IReadOnlyCollection<T>.Count => Length;

        public PooledArray(int length = 0)
        {
            array = ArrayPool<T>.Shared.Rent(length);
            Array.Clear(array, 0, length);
            Length = length;
        }
        public PooledArray(IEnumerable<T> enumerable, int length = -1)
        {
            array = ArrayPool<T>.Shared.Rent(length != -1 ? length : (enumerable as ICollection)?.Count ?? minCapacity);
            AddRange(enumerable);
        }
        ~PooledArray()
        {
            if (array != null)
            {
#if UNITY_EDITOR
                Debug.LogError($"An instance of PooledArray<{typeof(T).Name}> was not disposed!");
#endif
                Dispose();
            }
        }

        public void Reserve(int capacity)
        {
            if (capacity > array.Length)
                EnsureCapacity(capacity);
        }
        public T[] ToArray()
        {
            T[] newArray = new T[Length];
            Array.Copy(array, newArray, Length);
            return newArray;
        }
        public void Add(T item)
        {
            EnsureCapacity(Length);
            array[Length++] = item;
        }
        public void AddRange(IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection collection)
            {
                int count = collection.Count;
                EnsureCapacity(Length + count);
                collection.CopyTo(array, Length);
                Length += count;
            }
            else
            {
                int index = Length;
                foreach (T item in enumerable)
                {
                    EnsureCapacity(index);
                    array[index++] = item;
                }
                Length = index;
            }
        }
        public void AddRange(Span<T> span)
        {
            EnsureCapacity(Length + span.Length);
            span.CopyTo(array.AsSpan(Length));
            Length += span.Length;
        }
        public void Clear() => Length = 0;
        public int IndexOf(T item) => Array.IndexOf(array, item, 0, Length);
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Length)
#pragma warning disable S112 // General or reserved exceptions should never be thrown
                throw new IndexOutOfRangeException();
#pragma warning restore S112 // General or reserved exceptions should never be thrown

            if (index < Length - 1)
                Array.Copy(array, index + 1, array, index, Length - index - 1);

            Length--;
        }
        public void Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
                RemoveAt(index);
        }
        public void RemoveLast() => Length--;
        public void RemoveFast(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                array[index] = array[Length - 1];
                Length--;
            }
        }
        public void Sort(Comparison<T> comparer) => Array.Sort(array, 0, Length, Comparer<T>.Create(comparer));
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return array[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(array);
            array = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int index)
        {
            if (index >= array.Length)
            {
                T[] newArray = ArrayPool<T>.Shared.Rent(Mathf.Max(minCapacity, array.Length * 2));
                Array.Copy(array, newArray, Mathf.Min(index, array.Length));
                ArrayPool<T>.Shared.Return(array);
                array = newArray;
            }
        }
    }
}
