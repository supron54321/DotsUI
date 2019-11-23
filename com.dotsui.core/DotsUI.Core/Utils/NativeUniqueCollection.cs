using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsUI.Core.Utils
{
    internal unsafe struct NativeCollectionBucket
    {
        public int Count;
        public int Capacity;
        public void* BuffPtr;
    }
    internal unsafe struct NativeCollectionHeader
    {
        public int BucketCount;
        public int BucketCapacity;

        public NativeCollectionBucket* BucketPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeUnorderedCollection<T> : IDisposable where T : struct
    {
        [NativeDisableUnsafePtrRestriction] private NativeCollectionHeader* m_Header;

        Allocator m_AllocatorLabel;
        public NativeUnorderedCollection(int capacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>())
                throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(T)));
#endif
            m_AllocatorLabel = allocator;
            m_Header = (NativeCollectionHeader*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<NativeCollectionHeader>(), 4, allocator);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Header, m_AllocatorLabel);
        }
    }
}
