using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace VoxelEngine
{
    class Voxel
    {
        public Vector3 position;
        public string texture { get; private set; }
        public bool dynamic { get; private set; }

        public Voxel(Vector3 position, string texture)
            : this(position, texture, false)
        {            
        }

        public Voxel(Vector3 position, string texture, bool dynamic)
        {
            this.position = position;
            this.texture = texture;
            this.dynamic = dynamic;
        }
    }
}
