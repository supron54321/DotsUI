using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid
{
    public struct CanvasMeshContainer : ISharedComponentData, IEquatable<CanvasMeshContainer>
    {
        public Mesh UnityMesh;

        public bool Equals(CanvasMeshContainer other)
        {
            return Equals(UnityMesh, other.UnityMesh);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CanvasMeshContainer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (UnityMesh != null ? UnityMesh.GetHashCode() : 0);
        }
    }

    public struct CanvasCommandBufferContainer : ISharedComponentData, IEquatable<CanvasCommandBufferContainer>
    {
        public CommandBuffer Value;

        public bool Equals(CanvasCommandBufferContainer other)
        {
            return Equals(Value, other.Value);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CanvasCommandBufferContainer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
    }

    public struct CanvasTargetCamera : ISharedComponentData, IEquatable<CanvasTargetCamera>
    {
        public CameraImageRenderProxy Target;
        public bool Equals(CanvasTargetCamera other)
        {
            return Equals(Target, other.Target);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CanvasTargetCamera other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Target != null ? Target.GetHashCode() : 0);
        }
    }

    public struct CanvasTargetRenderTexture : ISharedComponentData, IEquatable<CanvasTargetRenderTexture>
    {
        public RenderTexture Target;
        public bool Equals(CanvasTargetRenderTexture other)
        {
            return Equals(Target, other.Target);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CanvasTargetRenderTexture other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Target != null ? Target.GetHashCode() : 0);
        }
    }

    public struct CanvasScreenSpaceOverlay : IComponentData { }
}
