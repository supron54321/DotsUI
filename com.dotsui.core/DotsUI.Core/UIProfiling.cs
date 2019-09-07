using System;

namespace DotsUI.Profiling
{
    public struct ProfilerSample : IDisposable
    {
        public ProfilerSample(string name)
        {
            UnityEngine.Profiling.Profiler.BeginSample(name);
        }

        public void Dispose()
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
