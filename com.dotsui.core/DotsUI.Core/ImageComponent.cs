using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsUI.Core
{
	//public struct SvgImage : ISharedComponentData, IEquatable<SvgImage>
	//{
	//    public Sprite Value;

	//    public bool Equals(SvgImage other)
	//    {
	//        return Equals(Value, other.Value);
	//    }

	//    public override bool Equals(object obj)
	//    {
	//        if (ReferenceEquals(null, obj)) return false;
	//        return obj is SvgImage other && Equals(other);
	//    }

	//    public override int GetHashCode()
	//    {
	//        return (Value != null ? Value.GetHashCode() : 0);
	//    }
	//}

	[Serializable]
	public struct SpriteAsset : ISharedComponentData, IEquatable<SpriteAsset>
	{
	    public Sprite Value;

	    public bool Equals(SpriteAsset other)
	    {
	        return Equals(Value, other.Value);
	    }

	    public override bool Equals(object obj)
	    {
	        if (ReferenceEquals(null, obj)) return false;
	        return obj is SpriteAsset other && Equals(other);
	    }

	    public override int GetHashCode()
	    {
	        return (Value != null ? Value.GetHashCode() : 0);
	    }
	}

    public struct SpriteImage : IComponentData
    {
        public Entity Asset;
    }

    public struct SpriteVertexData : IComponentData
    {
        public float4 Outer;
        public float4 Inner;
        public float4 Padding;
        public float4 Border;
        public float PixelsPerUnit;
        public int NativeMaterialId;    // TODO: Temporary hack

        public static SpriteVertexData Default { get
        {
            return new SpriteVertexData
            {
                Outer = new float4(0.0f),
                Inner = new float4(0.0f),
                Padding = new float4(0.0f),
                Border = new float4(0.0f),
                PixelsPerUnit = 100,
                NativeMaterialId = -1
            };
        } }
    }
}
