using DotsUI.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Controls
{
    public struct Slider : IComponentData
    {
        public enum Direction
        {
            /// <summary>
            /// From the left to the right
            /// </summary>
            LeftToRight,

            /// <summary>
            /// From the right to the left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// From the bottom to the top.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// From the top to the bottom.
            /// </summary>
            TopToBottom,
        }

        public Direction SliderDirection;
        public float MinValue;
        public float MaxValue;
        public float Value;
        public bool WholeNumbers;
        public Entity FillRect;
        public Entity HandleRect;


        public float NormalizedValue
        {
            get
            {
                if (MathExtensions.Approximately(MinValue, MaxValue))
                    return 0;
                return math.saturate((Value - MinValue) / (MaxValue - MinValue));
            }
            set => SetAndValidateValue(math.lerp(MinValue, MaxValue, value));
        }

        public void SetAndValidateValue(float value)
        {
            Value = math.clamp(value, MinValue, MaxValue);
            if (WholeNumbers)
                Value = math.round(Value);
        }

        public bool Reversed
        {
            get { return SliderDirection == Direction.RightToLeft || SliderDirection == Direction.TopToBottom; }
        }
    }
    public struct SliderValueChangedEvent : IComponentData
    {

    }
    static class SliderExtensions
    {
        public static int GetAxis(this Slider slider)
        {
            return slider.SliderDirection == Slider.Direction.LeftToRight ||
                   slider.SliderDirection == Slider.Direction.RightToLeft
                ? 0
                : 1;
        }
    }
}
