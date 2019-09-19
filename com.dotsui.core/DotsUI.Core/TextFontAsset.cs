using System;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace DotsUI.Core
{
    public struct TextFontAsset : IComponentData
    {
        public float LineHeight;
        public float NormalSpace;
        public float BoldSpace;
        public float AscentLine;
        public float CapLine;
        public float MeanLine;
        public float Baseline;
        public float DescentLine;
        public float PointSize;
        public float BoldStyle;
        public float NormalStyle;

        public float2 AtlasSize;
        public int NativeMaterialId;    // TODO: Temporary hack
    }

	[Serializable]
    public struct LegacyTextFontAsset : ISharedComponentData, IEquatable<LegacyTextFontAsset>
    {
        public Material FontMaterial;
        public TMP_FontAsset Asset;

        public bool Equals(LegacyTextFontAsset other)
        {
            return Equals(FontMaterial, other.FontMaterial);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LegacyTextFontAsset other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (FontMaterial != null ? FontMaterial.GetHashCode() : 0);
        }
    }

    public struct FontGlyphData : IBufferElementData
    {
        public ushort Character;
        public float Scale;
        public GlyphRect Rect;
        public GlyphMetrics Metrics;
    }
}
