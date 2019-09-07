using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsUI.Core
{
    public static unsafe class LowLevelUtils
    {
        //private static Func<object, Array> ExtractArrayFromList;
        //private static Action<object, int> ResizeList;

        //static LowLevelUtils()
        //{
        //    var asm = typeof(UnityEngine.Object).Assembly;
        //    var NativeHelper = asm.GetType("UnityEngine.NoAllocHelpers");
        //    ExtractArrayFromList = (Func<object, Array>)NativeHelper.GetMethod("ExtractArrayFromList").CreateDelegate(typeof(Func<object, Array>));
        //    var method = NativeHelper.GetMethod("Internal_ResizeList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        //    ResizeList = (Action<object, int>)method.CreateDelegate(typeof(Action<object, int>));
        //}


        //public static unsafe void UnsafeArrayCopy<T, U>(NativeArray<T> src, List<U> dst) where T : struct
        //    where U : struct
        //{
        //    EnsureListElemCount(dst, src.Length);
        //    if (src.Length > 0)
        //    {
        //        U[] asArray = (U[])ExtractArrayFromList(dst);
        //        void* vPtr = UnsafeUtility.AddressOf(ref asArray[0]);
        //        using (new ProfilerSample("UnsafeArrayCopy.ActualCopy"))
        //            UnsafeUtility.MemCpy(vPtr, src.GetUnsafeReadOnlyPtr(), src.Length * UnsafeUtility.SizeOf<T>());
        //    }
        //}
        //public static unsafe void UnsafeArrayCopy<T, U>(NativeSlice<T> src, List<U> dst) where T : struct
        //    where U : struct
        //{
        //    EnsureListElemCount(dst, src.Length);
        //    if (src.Length > 0)
        //    {
        //        U[] asArray = (U[])ExtractArrayFromList(dst);
        //        void* vPtr = UnsafeUtility.AddressOf(ref asArray[0]);
        //        using (new ProfilerSample("UnsafeArrayCopy.ActualCopy"))
        //            UnsafeUtility.MemCpy(vPtr, src.GetUnsafeReadOnlyPtr(), src.Length * UnsafeUtility.SizeOf<T>());
        //    }
        //}

        //public static void EnsureListElemCount<T>(List<T> list, int count)
        //{
        //    //list.Clear();
        //    if (list.Capacity < count)
        //    {
        //        list.Capacity = count;
        //    }
        //    ResizeList(list, count);
        //}

        //public static unsafe void UnsafeArrayInitialize(List<Vector3> mTempNormalList, int count, float3 float5)
        //{
        //    EnsureListElemCount(mTempNormalList, count);
        //    if (count > 0)
        //    {
        //        Vector3[] asArray = (Vector3[])ExtractArrayFromList(mTempNormalList);
        //        void* vPtr = UnsafeUtility.AddressOf(ref asArray[0]);
        //        using (new ProfilerSample("UnsafeArrayCopy.ActualCopy"))
        //            UnsafeUtility.MemCpyReplicate(vPtr, UnsafeUtility.AddressOf(ref float5), UnsafeUtility.SizeOf<float3>(), count);
        //    }
        //}

        public static void MemSet<T>(NativeArray<T> array, T value) where T : struct
        {
            UnsafeUtility.MemCpyReplicate(array.GetUnsafePtr(), UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>(), array.Length);
        }
    }
}
