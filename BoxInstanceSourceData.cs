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
    /// Creates a single procedural box for use as instancing data.
    /// </summary>
    public class BoxInstanceSourceData : IInstanceSourceData
    {
        /// <summary>
        /// Array of vertices describing the instance mesh.
        /// </summary>
        public VertexPositionNormalTextureBump[] Vertices { get { return _Vertices; } }
        /// <summary>
        /// Array of indices used to render the instance mesh.  Mesh primitives are assumed to be of the type TriangleList.
        /// </summary>
        public short[] Indices { get { return _Indices; } }

        private VertexPositionNormalTextureBump[] _Vertices = new VertexPositionNormalTextureBump[1];
        private short[] _Indices = new short[1];

        // Used to procedurally create box meshes.
        static private Vector3[] _BoxCorners = {
            new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f),
            new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f),
        };

        // Used to procedurally create box meshes.
        static private int[] _BoxIndices = {
            1, 0, 3, 2, 4, 5, 6, 7,
            0, 4, 2, 6, 5, 1, 7, 3,
            0, 1, 4, 5, 3, 2, 7, 6,
        };

        /// <summary>
        /// Creates a new BoxInstanceSourceData instance.
        /// </summary>
        public BoxInstanceSourceData()
        {
            // Create the box data buffers.
            _Indices = new short[36];
            _Vertices = new VertexPositionNormalTextureBump[24];

            // Create and fill the box data.
            int vertindex = 0;
            int indexoffset = 0;

            for (int f = 0; f < 6; f++)
            {
                // Get the static box builder data.
                Vector3 vert0 = _BoxCorners[_BoxIndices[vertindex]];
                Vector3 vert1 = _BoxCorners[_BoxIndices[vertindex + 1]];
                Vector3 vert2 = _BoxCorners[_BoxIndices[vertindex + 2]];
                Vector3 vert3 = _BoxCorners[_BoxIndices[vertindex + 3]];

                // Create and set the vertex positions, normals, uvs, and indices.
                _Vertices[vertindex].Position = vert0;
                _Vertices[vertindex + 1].Position = vert1;
                _Vertices[vertindex + 2].Position = vert2;
                _Vertices[vertindex + 3].Position = vert3;

                Plane plane = new Plane(vert0, vert2, vert1);

                _Vertices[vertindex].Normal = plane.Normal;
                _Vertices[vertindex + 1].Normal = plane.Normal;
                _Vertices[vertindex + 2].Normal = plane.Normal;
                _Vertices[vertindex + 3].Normal = plane.Normal;

                _Vertices[vertindex].TextureCoordinate = GenerateBoxMap(vert0, plane.Normal);
                _Vertices[vertindex + 1].TextureCoordinate = GenerateBoxMap(vert1, plane.Normal);
                _Vertices[vertindex + 2].TextureCoordinate = GenerateBoxMap(vert2, plane.Normal);
                _Vertices[vertindex + 3].TextureCoordinate = GenerateBoxMap(vert3, plane.Normal);

                _Indices[indexoffset++] = (byte)(vertindex);
                _Indices[indexoffset++] = (byte)(vertindex + 1);
                _Indices[indexoffset++] = (byte)(vertindex + 2);
                _Indices[indexoffset++] = (byte)(vertindex + 3);
                _Indices[indexoffset++] = (byte)(vertindex + 2);
                _Indices[indexoffset++] = (byte)(vertindex + 1);

                vertindex += 4;
            }

            // Generate the tangent space info for bump and specular mapping.
            VertexPositionNormalTextureBump.BuildTangentSpaceDataForTriangleList(_Indices, _Vertices);
        }


        #region Code from other community resources.

        /// <summary>
        /// Performs traditional "box" uv mapping based on the vertex position and normal.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public Vector2 GenerateBoxMap(Vector3 position, Vector3 normal)
        {
            if (Math.Abs(normal.X) > Math.Abs(normal.Y))
            {
                if (Math.Abs(normal.X) > Math.Abs(normal.Z))
                    return new Vector2(position.Y, position.Z);//X
                else
                    return new Vector2(position.X, position.Y);//Z
            }
            else
            {
                if (Math.Abs(normal.Y) > Math.Abs(normal.Z))
                    return new Vector2(position.X, position.Z);//Y
                else
                    return new Vector2(position.X, position.Y);//Z
            }
        }

        #endregion
    }
}
