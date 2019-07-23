using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
