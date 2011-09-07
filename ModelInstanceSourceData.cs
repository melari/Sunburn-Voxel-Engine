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
    /// Extracts XNA Model data for use in instancing.
    /// </summary>
    public class ModelInstanceSourceData : IInstanceSourceData
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

        /// <summary>
        /// Creates a new ModelInstanceSourceData instance.
        /// </summary>
        public ModelInstanceSourceData(Model model)
        {
            // Temporary buffers for building the data.
            List<VertexPositionNormalTextureBump> vertices = new List<VertexPositionNormalTextureBump>(256);
            List<short> indices = new List<short>(256);
            Dictionary<int, int> indexremap = new Dictionary<int, int>(256);

            // Temporary buffers for extracting model data.
            VertexPositionNormalTextureBump[] sourcevertices = new VertexPositionNormalTextureBump[1];
            short[] sourceindices = new short[1];

            // Get the model transforms for baking down vertices into object space (XNA Models are stored in mesh space).
            Matrix[] bonearray = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bonearray);

            for (int m = 0; m < model.Meshes.Count; m++)
            {
                ModelMesh mesh = model.Meshes[m];

                // Test for correct vertex format.
                VerifyVertexFormat(mesh);

                // Get the number of verts and indices (only VertexPositionNormalTextureBump and short are supported - these are used by SunBurn).
                int vertcount = mesh.VertexBuffer.SizeInBytes / VertexPositionNormalTextureBump.SizeInBytes;
                int indcount = mesh.IndexBuffer.SizeInBytes / sizeof(short);

                // Readjust the buffer sizes as necessary.
                if (sourcevertices.Length < vertcount)
                    sourcevertices = new VertexPositionNormalTextureBump[vertcount];
                if (sourceindices.Length < indcount)
                    sourceindices = new short[indcount];

                // Get the mesh data.
                mesh.VertexBuffer.GetData<VertexPositionNormalTextureBump>(sourcevertices, 0, vertcount);
                mesh.IndexBuffer.GetData<short>(sourceindices, 0, indcount);

                // Get the mesh-to-object space transform.
                Matrix meshtoobject = bonearray[mesh.ParentBone.Index];

                indexremap.Clear();

                for (int p = 0; p < mesh.MeshParts.Count; p++)
                {
                    ModelMeshPart part = mesh.MeshParts[p];

                    if (part.StreamOffset != 0)
                        throw new Exception("Stream offset not supported.");

                    // Loop through all of the vertices.
                    for (int i = 0; i < (part.PrimitiveCount * 3); i++)
                    {
                        int index = sourceindices[i + part.StartIndex] + part.BaseVertex;

                        // Did we already store the data in the vertex buffer?
                        if (indexremap.ContainsKey(index))
                        {
                            indices.Add((short)indexremap[index]);
                            continue;
                        }

                        // Copy the vertex and convert to object space.
                        VertexPositionNormalTextureBump vert = sourcevertices[index];

                        vert.Position = Vector3.Transform(vert.Position, meshtoobject);
                        vert.Normal = Vector3.TransformNormal(vert.Normal, meshtoobject);
                        vert.Tangent = Vector3.TransformNormal(vert.Tangent, meshtoobject);
                        vert.Binormal = Vector3.TransformNormal(vert.Binormal, meshtoobject);

                        vert.Normal.Normalize();
                        vert.Tangent.Normalize();
                        vert.Binormal.Normalize();

                        // Remap the source index (from the model) to the destination index (in the buffers).
                        int destindex = vertices.Count;
                        indexremap.Add(index, destindex);

                        // Store the data.
                        indices.Add((short)destindex);
                        vertices.Add(vert);
                    }
                }
            }

            // Convert the buffers to the final arrays.
            _Vertices = vertices.ToArray();
            _Indices = indices.ToArray();
        }

        private void VerifyVertexFormat(ModelMesh mesh)
        {
            VertexElement[] validelements = VertexPositionNormalTextureBump.VertexElements;

            for (int p = 0; p < mesh.MeshParts.Count; p++)
            {
                ModelMeshPart part = mesh.MeshParts[p];
                VertexElement[] partelements = part.VertexDeclaration.GetVertexElements();

                foreach (VertexElement partelement in partelements)
                {
                    VertexElement validelement = FindElementByUsage(validelements, partelement.VertexElementUsage);
                    if (validelement.Equals(partelement))
                        continue;

                    throw new Exception("Model mesh '" + mesh.Name + "' contains an incorrect vertex format, " +
                        "example is written to use the VertexPositionNormalTextureBump format.");
                }
            }
        }

        private VertexElement FindElementByUsage(VertexElement[] elements, VertexElementUsage usage)
        {
            foreach (VertexElement element in elements)
            {
                if (element.VertexElementUsage == usage)
                    return element;
            }

            return new VertexElement();
        }
    }
}
