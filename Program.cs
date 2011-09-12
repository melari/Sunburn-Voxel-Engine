//-----------------------------------------------
// Synapse Gaming - Instancing Example
// Copyright © Synapse Gaming 2009
//-----------------------------------------------

using System;
using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace VoxelEngine
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (Engine game = new Engine())
            {
                try
                {
                    game.Run();
                }
                catch (NoSuitableGraphicsDeviceException)
                {
                    MessageBox.Show("This application requires Shader Model 3.0.  If your video card supports " +
                        "Shader Model 3.0 please make sure XNA is installed and your drivers are up to date.", "Shader Model 3.0 Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

