using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace DotsUI.Core
{
    public static class MathExtensions
    {
        public static bool Approximately(float a, float b)
        {
            return (double)math.abs(b - a) < (double)math.max(1E-06f * math.max(math.abs(a), math.abs(b)), math.DBL_MIN_NORMAL * 8f);
        }
    }
}
