using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace DotsUI.Core
{
    [BurstCompile]
    public struct ClearHashMap<T0, T1> : IJob where T0 : struct, System.IEquatable<T0> where T1 : struct, System.IEquatable<T1>
    {
        public NativeHashMap<T0, T1> Container;
        public void Execute()
        {
            Container.Clear();
        }
    }

}
