using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

[assembly: InternalsVisibleTo("DotsUI.Core.Tests")]
[assembly: InternalsVisibleTo("DotsUI.Editor")]
[assembly: InternalsVisibleTo("DotsUI.Hybrid")]

namespace DotsUI.Core
{
    public struct CanvasConstantPhysicalSizeScaler : IComponentData
    {
        public float Factor;
    }

    public struct CanvasScreenSize : IComponentData
    {
        public int2 Value;
    }

    public struct CanvasConstantPixelSizeScaler : IComponentData
    {

    }
    public struct CanvasSortLayer : IComponentData
    {
        /// <summary>
        /// </summary>
        public int Value;
    }
    /// <summary>
    /// 16 seems like a quite big buffer, but it's enough to store 9-slice sprite vertices.
    /// It is the most common sprite type used in UI. I noticed performance increase compared to smaller buffers
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct ControlVertexData : IBufferElementData
    {
        public float3 Position;
        public float3 Normal;
        public float4 Color;
        public float2 TexCoord0;
        public float2 TexCoord1;
    }
    [InternalBufferCapacity(0)]    // 54 is the worst case scenario for 9-slice sprite
    public struct ControlVertexIndex : IBufferElementData
    {
        public int Value;
        public static implicit operator ControlVertexIndex(int v) { return new ControlVertexIndex { Value = v }; }
        public static implicit operator int(ControlVertexIndex v) { return v.Value; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex : IBufferElementData
    {
        public float3 Position;
        public float3 Normal;
        public float4 Color;
        public float2 TexCoord0;
        public float2 TexCoord1;

        public static implicit operator MeshVertex(ControlVertexData v)
        {
            return new MeshVertex
            {
                Position = v.Position,
                Normal = v.Normal,
                Color = v.Color,
                TexCoord0 = v.TexCoord0,
                TexCoord1 = v.TexCoord1
            };
        }
        public static implicit operator ControlVertexData(MeshVertex v)
        {
            return new ControlVertexData()
            {
                Position = v.Position,
                Normal = v.Normal,
                Color = v.Color,
                TexCoord0 = v.TexCoord0,
                TexCoord1 = v.TexCoord1
            };
        }
    }
    public struct MeshVertexIndex : IBufferElementData
    {
        public int Value;
        public static implicit operator MeshVertexIndex(int v) { return new MeshVertexIndex { Value = v }; }
        public static implicit operator int(MeshVertexIndex v) { return v.Value; }
    }
    public enum SubMeshType : byte
    {
        SpriteImage,
        SvgImage,
        Text,
    }
    /// <summary>
    /// Sub-meshes in UI mesh. Material ID is SCD index. Soon it will be replaced with Entity material.
    /// </summary>
    public struct SubMeshInfo : IBufferElementData
    {
        public int Offset;
        public int MaterialId;
        public SubMeshType MaterialType;
    }

    /// <summary>
    /// Pointer to the first vertex of UI control, stored in Canvas Mesh. Useful for fast color update without whole mesh rebuild.
    /// </summary>
    public struct ElementVertexPointerInMesh : IComponentData
    {
        public int VertexPointer;
    }
}
