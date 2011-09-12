using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using SynapseGaming.LightingSystem.Effects.Forward;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Rendering;
using SynapseGaming.LightingSystem.Core;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelEngine
{
    class Container
    {
        public string type;
        public SceneObject sceneObject;
        public int count = 0;

        public Container(string type)
        {
            this.type = type;
        }
    }

    class VoxelManager
    {
        public static int VOXELS_PER_CONTAINER = 75;
                
        Dictionary<string, List<Container>> Containers = new Dictionary<string, List<Container>>();
        List<Voxel> Voxels = new List<Voxel>();

        public VoxelManager()
        {
        }
        
        public void Update()
        {
        }

        public void AddVoxel(Voxel voxel)
        {

        }
        
        /* Initilization Methods. Do not call after calling FinishInit() */        
        public void AddVoxelType(string type, int maxAmount)
        {
            int count = (int)Math.Ceiling((double)maxAmount / VOXELS_PER_CONTAINER);
            for (int i = 0; i < count; i++)
            {
                if (!Containers.ContainsKey(type))
                {
                    Containers.Add(type, new List<Container>());
                }
                Containers[type].Add(new Container(type));
            }
        }
        
        public void FinishInit(ContentManager Content, GraphicsDevice device, SceneInterface sceneInterface)
        {                        
            BoundingSphere bounds = new BoundingSphere(Vector3.Zero, 1000.0f);

            // Build the instance source data. We can reuse this single source, since all voxels are the exact same, just with different effects.
            IInstanceSourceData source;
            source = new BoxInstanceSourceData();

            foreach (List<Container> container_list in Containers.Values)
            {
                foreach (Container container in container_list)
                {
                    LightingEffect effect = Content.Load<LightingEffect>(container.type);
                    effect.Skinned = true;

                    InstanceContainer Icontainer = new InstanceContainer(device, effect);
                    for (int i = 0; i < VOXELS_PER_CONTAINER; i++)
                    {
                        Icontainer.AddInstance(source, i);
                    }
                    Icontainer.Build();


                    // Create a SceneObject to contain the custom geometry data.                
                    container.sceneObject = new SceneObject(Icontainer.Effect, bounds, Matrix.Identity,
                        Icontainer.IndexBuffer, Icontainer.VertexBuffer, Icontainer.VertexDeclaration, 0,
                        Icontainer.PrimitiveType, Icontainer.PrimitiveCount, 0, Icontainer.VertexRange, 0,
                        Icontainer.VertexStride);
                    container.sceneObject.ObjectType = ObjectType.Static;
                    container.sceneObject.Visibility = ObjectVisibility.RenderedAndCastShadows;

                    //Set Locations of voxels.
                    Matrix[] boxobjecttransforms = new Matrix[VOXELS_PER_CONTAINER];

                    int j = 0;
                    while (j < boxobjecttransforms.Length)
                    {
                        boxobjecttransforms[j++] = Matrix.CreateTranslation(j, 0, 0);
                    }
                    container.sceneObject.SkinBones = boxobjecttransforms;

                    // Submit to the ObjectManager.
                    sceneInterface.ObjectManager.Submit(container.sceneObject);
                }
            }

            Update();
        }
    }
}
