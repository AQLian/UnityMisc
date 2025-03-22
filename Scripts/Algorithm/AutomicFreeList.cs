using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport.Utilities.LowLevel.UnsafeAtomicFreeListEnhanced
{
    internal unsafe struct UnsafeAtomicFreeList : IDisposable
    {
        // used count
        // free list size
        // free indices...
        [NativeDisableUnsafePtrRestriction]
        private int* m_Buffer;
        private int m_BufferSize;
        private int m_Length;
        private Allocator m_Allocator;

        public int Capacity => m_Length;

        // todo! should make it thread safe also !
        public int InUse => m_Buffer[0] - m_Buffer[1];

        public unsafe int InUseThreadSafe
        {
            get
            {
                long combined = Interlocked.Read(ref *(long*)m_Buffer);
                int allocated;
                int free;
                UnsafeUtility.MemCpy(&allocated, &combined, 4);              // Copy first 4 bytes (m_Buffer[0]).
                UnsafeUtility.MemCpy(&free, (byte*)&combined + 4, 4);        // Copy next 4 bytes (m_Buffer[1]).
                return allocated - free;
            }
        }

        public bool IsCreated => m_Buffer != null;

        private int* m_InUseFreeFlag; // 0: free, 1: in use

        /// <summary>
        /// Initializes a new instance of the AtomicFreeList struct.
        /// </summary>
        /// <param name="capacity">The number of elements the free list can store.</param>
        /// <param name="allocator">The <see cref="Allocator"/> used to allocate the memory.</param>
        public UnsafeAtomicFreeList(int capacity, Allocator allocator)
        {
            m_Allocator = allocator;
            m_Length = capacity;
            m_BufferSize = UnsafeUtility.SizeOf<int>() * (capacity + 2);
            m_InUseFreeFlag = (int*)UnsafeUtility.Malloc(capacity * UnsafeUtility.SizeOf<int>(),
             UnsafeUtility.AlignOf<long>(), allocator);
            m_Buffer = (int*)UnsafeUtility.Malloc(m_BufferSize, 
            UnsafeUtility.AlignOf<long>(), allocator);
            UnsafeUtility.MemClear(m_Buffer, m_BufferSize);
        }

        public void Dispose()
        {
            if (IsCreated)
            {
                UnsafeUtility.Free(m_Buffer, m_Allocator);
                UnsafeUtility.Free(m_InUseFreeFlag, m_Allocator);
            }
        }

        public void Reset()
        {
            UnsafeUtility.MemClear(m_Buffer, m_BufferSize);
            UnsafeUtility.MemClear(m_InUseFreeFlag, m_Length * UnsafeUtility.SizeOf<int>());
        }

        /// <summary>
        /// Inserts an item on top of the stack.
        /// </summary>
        /// <param name="item">The item to push onto the stack.</param>
        public unsafe void PushUnsafe(int item)
        {
            int* buffer = m_Buffer;
            int idx = Interlocked.Increment(ref buffer[1]) - 1;
            while (Interlocked.CompareExchange(ref buffer[idx + 2], item + 1, 0) != 0)
            {
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true for push success </returns>
        public unsafe bool TryPush(int item){
            if (Interlocked.CompareExchange(ref m_InUseFreeFlag[item], 0, 1) == 1){
                Push(item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove and return a value from the top of the stack
        /// </summary>
        /// <remarks>
        /// <value>The removed value from the top of the stack.</value>
        public unsafe int Pop()
        {
            int* buffer = m_Buffer;
            int idx = buffer[1] - 1;
            while (idx >= 0 && Interlocked.CompareExchange(ref buffer[1], idx, idx + 1) != idx + 1)
                idx = buffer[1] - 1;

            if (idx >= 0)
            {
                int val = 0;
                while (val == 0)
                {
                    val = Interlocked.Exchange(ref buffer[2 + idx], 0);
                }

                return val - 1;
            }

            idx = Interlocked.Increment(ref buffer[0]) - 1;
            if (idx >= Capacity)
            {
                Interlocked.Decrement(ref buffer[0]);
                return -1;
            }

            // Mark the index as in-use.
            Interlocked.Exchange(ref m_InUseFreeFlag[idx], 1);

            return idx;
        }
    }
}
