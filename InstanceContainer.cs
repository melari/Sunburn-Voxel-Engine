//-----------------------------------------------
// Synapse Gaming - Instancing Example
// Copyright © Synapse Gaming 2009
//-----------------------------------------------
//
// This example shows how to perform object instancing using
// SunBurn's built-in effects and skinning support.
//
// The concept behind this type of instancing is: multiple
// meshes packed into a single vertex and index buffer, can be
// rendered in a single draw call, and still be supplied their own
// unique transforms using SunBurn's bone transforms and the
// BlendIndices vertex channel (both normally used for skinning).
//
// During rendering the mesh instance's blend/bone index is used as
// an instance id and index into the provided bone transform
// array.  This effectively becomes:
//
//   Matrix instancetransform = objecttransforms[instance.BoneIndex0];
//
// Because each mesh uses its own transform it can be manipulated
// separately from all other meshes (as though it's a separate object).
//
// While this example uses procedural boxes and XNA Models, any single meshes
// will work well - even meshes that differ from each other like trees.
//
// Note: multiple part meshes either need to be provided several
// transforms (one per-part) or baked down to a single mesh.
//
// Note: all instances packed into the same container object share
// the same effect / material.
//
//-----------------------------------------------------------------------------

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Include the necessary SunBurn namespaces.
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using SynapseGaming.LightingSystem.Rendering.Forward;
#endregion


namespace VoxelEngine
{
    /// <summary>
    /// Common interface for providing instance mesh data to the
    /// container scene object (InstanceContainerSceneObject).
    /// </summary>
    public interface IInstanceSourceData
    {
        /// <summary>
        /// Array of vertices describing the instance mesh.
        /// </summary>
        VertexPositionNormalTextureBump[] Vertices { get; }
        /// <summary>
        /// Array of indices used to render the instance mesh.  Mesh primitives are assumed to be of the type TriangleList.
        /// </summary>
        short[] Indices { get; }
    }

    /// <summary>
    /// Provides a single renderable container object that contains many mesh instances (maximum 75),
    /// which helps limit draw calls and effect setup.  Instances can be individually transformed
    /// using the SceneObject.SkinBones transform array.
    /// 
    /// How it works:
    /// 
    /// The concept behind this type of instancing is: multiple
    /// meshes packed into a single vertex and index buffer, can be
    /// rendered in a single draw call, and still be supplied their own
    /// unique transforms using SunBurn's bone transforms and the
    /// BlendIndices vertex channel (both normally used for skinning).
    ///
    /// During rendering the mesh instance's blend/bone index is used as
    /// an instance id and index into the provided bone transform
    /// array.  This effectively becomes:
    ///
    ///   Matrix instancetransform = bonetransforms[instance.BoneIndex0];
    ///
    /// Because each mesh uses its own transform it can be manipulated
    /// separately from all other meshes (as though it's a separate object).
    /// </summary>
    public class InstanceContainer : IDisposable
    {
        public VertexBuffer VertexBuffer { get { return _VertexBuffer; } }
        public IndexBuffer IndexBuffer { get { return _IndexBuffer; } }
        public VertexDeclaration VertexDeclaration { get { return _VertexDeclaration; } }
        public int PrimitiveCount { get { return _PrimitiveCount; } }
        public int VertexRange { get { return _VertexRange; } }
        public int VertexStride { get { return _VertexStride; } }
        public PrimitiveType PrimitiveType { get { return PrimitiveType.TriangleList; } }
        public Effect Effect { get { return _Effect; } }

        private GraphicsDevice _Device;
        private VertexBuffer _VertexBuffer;
        private IndexBuffer _IndexBuffer;
        private VertexDeclaration _VertexDeclaration;
        private Effect _Effect;
        private int _PrimitiveCount = 0;
        private int _VertexRange = 0;
        private int _VertexStride = 0;

        private List<VertexPositionNormalTextureBumpSkin> _Vertices = new List<VertexPositionNormalTextureBumpSkin>(256);
        private List<short> _Indices = new List<short>(256);

        /// <summary>
        /// Creates a new InstanceContainer instance.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="effect">Effect used to render all instances in the container object.</param>
        public InstanceContainer(GraphicsDevice device, Effect effect)
        {
            _Device = device;
            _Effect = effect;
        }

        /// <summary>
        /// Add an instance to the container object.
        /// </summary>
        /// <param name="data">Instance source data.</param>
        /// <param name="instancetransformindex">Index of the instance's transform within the transform array used during rendering.</param>
        /// <returns>Returns false if the instance cannot be added due to insufficient space.</returns>
        public bool AddInstance(IInstanceSourceData data, int instancetransformindex)
        {
            // Get the number of vertices already in the container object to readjust the new instance's destination indices.
            int indexoffset = _Vertices.Count;

            // Avoid overflowing the indices.
            int finalmaxindex = indexoffset + data.Vertices.Length;
            if (finalmaxindex > short.MaxValue)
                return false;

            // Copy all vertices into the container object.
            foreach (VertexPositionNormalTextureBump vert in data.Vertices)
            {
                VertexPositionNormalTextureBumpSkin dest = new VertexPositionNormalTextureBumpSkin();

                // Copy source data.
                dest.Position = vert.Position;
                dest.TextureCoordinate = vert.TextureCoordinate;
                dest.Normal = vert.Normal;
                dest.Tangent = vert.Tangent;
                dest.Binormal = vert.Binormal;

                // Add instance information that tells SunBurn which transform to use in the instance transform array.
                dest.BoneIndex0 = (byte)instancetransformindex;
                // Required for instancing.
                dest.BoneWeights = Vector4.UnitX;

                _Vertices.Add(dest);
            }

            // Copy all indices into the container object, adjusting for existing instances.
            foreach (short index in data.Indices)
                _Indices.Add((short)(index + indexoffset));

            return true;
        }

        /// <summary>
        /// Build all added instances into renderable information.
        /// </summary>
        public void Build()
        {
            ClearGraphicsResources();

            // Create and fill the buffers.
            _VertexBuffer = new VertexBuffer(_Device, typeof(VertexPositionNormalTextureBumpSkin), _Vertices.Count, BufferUsage.WriteOnly);
            _VertexBuffer.SetData<VertexPositionNormalTextureBumpSkin>(_Vertices.ToArray());

            // Create and fill the buffers.
            _IndexBuffer = new IndexBuffer(_Device, typeof(short), _Indices.Count, BufferUsage.WriteOnly);
            _IndexBuffer.SetData<short>(_Indices.ToArray());

            // Generate the vertex and primitive information.
            _VertexDeclaration = new VertexDeclaration(_Device, VertexPositionNormalTextureBumpSkin.VertexElements);
            _VertexStride = VertexPositionNormalTextureBumpSkin.SizeInBytes;
            _PrimitiveCount = _Indices.Count / 3;
            _VertexRange = _Vertices.Count;

            ClearIntermediateData();
        }

        private void ClearGraphicsResources()
        {
            if (_VertexBuffer != null)
            {
                _VertexBuffer.Dispose();
                _VertexBuffer = null;
            }

            if (_IndexBuffer != null)
            {
                _IndexBuffer.Dispose();
                _IndexBuffer = null;
            }

            if (_VertexDeclaration != null)
            {
                _VertexDeclaration.Dispose();
                _VertexDeclaration = null;
            }
        }

        private void ClearIntermediateData()
        {
            _Vertices.Clear();
            _Indices.Clear();
        }

        public void Dispose()
        {
            _Device = null;
            _Effect = null;

            ClearGraphicsResources();
            ClearIntermediateData();
        }
    }
}
