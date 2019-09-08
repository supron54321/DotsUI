using UnityEngine;

namespace DotsUI.Core
{
    internal struct ScreenUtils
    {
        private const float DpiFallback = 96;
        private static float TargetDpi = 172;

        private static float GetNativeDpi()
        {
            return Screen.dpi != 0 ? Screen.dpi : DpiFallback;
        }
        public static float GetScaledDpi()
        {
            float nativeDpi = GetNativeDpi();
            return nativeDpi > TargetDpi ? TargetDpi : nativeDpi;
        }

        public static void SetTargetDpi(float value)
        {
            TargetDpi = value;
        }
    }
}
