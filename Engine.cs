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
//   Matrix instancetransform = bonetransforms[instance.BoneIndex0];
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
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

// Include the necessary SunBurn namespaces.
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using SynapseGaming.LightingSystem.Rendering.Forward;
using SynapseGaming.LightingSystem.Shadows;
#if !SUNBURN_FRAMEWORK
using SynapseGaming.LightingSystem.Editor;
#endif
#endregion


namespace VoxelEngine
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Engine : Microsoft.Xna.Framework.Game
    {
        public static bool DEBUG = false;

        VoxelManager voxelManager = new VoxelManager();

        // The SunBurn lighting system.
        LightingSystemManager lightingSystemManager;
        SceneInterface sceneInterface;
        SceneState sceneState;
        SceneEnvironment environment;
        LightingSystemPreferences preferences;
        SplashScreenGameComponent splashScreenGameComponent;
        LightingSystemStatistic totalObjectCountStat;

        // Scene/camera supporting members.
        bool firstMouseSample = true;
        Vector3 viewPosition = new Vector3(86.5f, 11.2f, 57.0f);
        Vector3 viewRotation = new Vector3(-2.2f, 0.16f, 0.0f);
        Matrix view = Matrix.Identity;
        Matrix projection = Matrix.Identity;

        // Default XNA members.
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        const float moveScale = 20.0f;
        const string userPreferencesFile = "UserPreferences.xml";

        public Engine()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content-" + LightingSystemManager.Edition;

            // Minimum requirement.
            graphics.MinimumPixelShaderProfile = ShaderProfile.PS_3_0;
            graphics.MinimumVertexShaderProfile = ShaderProfile.VS_3_0;

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = true;
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.PreparingDeviceSettings += PrepareDeviceSettings;            

            // Used for advanced edge cleanup.
            graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

            // Required for lighting system.
            splashScreenGameComponent = new SplashScreenGameComponent(this, graphics);
            Components.Add(splashScreenGameComponent);

            // Create the lighting system.
            lightingSystemManager = new LightingSystemManager(Services);
            sceneState = new SceneState();

            // Create the scene interface. Acts as a service provider containing all scene managers
            // and returning them by type (including custom managers). Also acts as a component
            // container where calls to manager methods on the SceneInterface (such as BeginFrameRendering,
            // Unload, ...) are automatically called on all contained managers.
            //
            // This design allows managers to be plugged-in like modular components and for managers
            // to easily be added, removed, or replaced with custom implementations.
            //
            sceneInterface = new SceneInterface(graphics);
            sceneInterface.CreateDefaultManagers(false, false, false);
            


            // The skybox handles the back buffer clear.
            if (sceneInterface.RenderManager is BaseRenderManager)
                (sceneInterface.RenderManager as BaseRenderManager).ClearBackBufferEnabled = true;

            // Create a custom statistic, which is rendered to the screen with SunBurn's statistics.
            totalObjectCountStat = LightingSystemStatistics.GetStatistic("Instancing_TotalObjectCount", LightingSystemStatisticCategory.Rendering);

            // Load the user preferences (example - not required).
            preferences = new LightingSystemPreferences();
            if (File.Exists(userPreferencesFile))
                preferences.LoadFromFile(userPreferencesFile);
            else
            {
                preferences.EffectDetail = DetailPreference.High;
                preferences.MaxAnisotropy = 4;
                preferences.PostProcessingDetail = DetailPreference.High;
                preferences.ShadowDetail = DetailPreference.High;
                preferences.ShadowQuality = 1.0f;
                preferences.TextureQuality = DetailPreference.High;
                preferences.TextureSampling = SamplingPreference.Anisotropic;
            }

            view = GetViewMatrix();
        }

        /// <summary>
        /// Improves overall performance with dynamic shadows.
        /// </summary>
        private void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);


            // TODO: use this.Content to load your game content here


            // Add objects and lights to the ObjectManager and LightManager
            // respectively. The ObjectManager accepts objects in several forms:
            //
            //   -As SceneObjects, which can be dynamic (movable) or static and are
            //    created from XNA Models or custom vertex / index buffer.
            //
            //   -As XNA Models, which can only be static.
            //




            voxelManager.AddVoxelType("Models/cobblestone", 10000);
            voxelManager.FinishInit(Content, GraphicsDevice, sceneInterface);

            

            //Window.Title = Window.Title + " - Instanced Object Count: " + (instancesPerContainerObject * containerObjects.Length);



            // LightRigs contain many lights and light groups.
            LightRig rig = new LightRig();

            // Ambient lights uniformly illuminate the scene.
            AmbientLight ambientlight = new AmbientLight();
            ambientlight.Enabled = true;
            ambientlight.DiffuseColor = new Vector3(0.8f, 0.98f, 0.99f);
            ambientlight.Intensity = 0.5f;

            // Directional lights illuminate the scene from a specific direction, similar to sunlight.
            DirectionalLight sunlight = new DirectionalLight();
            sunlight.Enabled = true;
            sunlight.DiffuseColor = new Vector3(1.0f, 0.97f, 0.77f);
            sunlight.Intensity = 2.6f;
            sunlight.Direction = new Vector3(-0.60f, -0.73f, -0.32f);
            sunlight.ShadowType = ShadowType.AllObjects;
            sunlight.ShadowQuality = 1.0f;
            sunlight.ShadowPrimaryBias = 1.0f;
            sunlight.ShadowSecondaryBias = 0.04f;

            DirectionalLight sunlightB = new DirectionalLight();
            sunlightB.Enabled = true;
            sunlightB.DiffuseColor = new Vector3(0.0f, 0.97f, 0.77f);
            sunlightB.Intensity = 2.6f;
            sunlightB.Direction = new Vector3(0.60f, -0.73f, -0.32f);
            sunlightB.ShadowType = ShadowType.AllObjects;
            sunlightB.ShadowQuality = 1.0f;
            sunlightB.ShadowPrimaryBias = 1.0f;
            sunlightB.ShadowSecondaryBias = 0.04f;

            // Add the lights to a group.
            LightGroup group = new LightGroup();
            group.Add(ambientlight);
            group.Add(sunlight);
            group.Add(sunlightB);

            // Add the group to the light rig and commit the changes.
            rig.LightGroups.Add(group);
            rig.CommitChanges();

            // Submit the light rig to the light manager.
            sceneInterface.LightManager.Submit(rig);


            // Setup the scene settings.
            environment = new SceneEnvironment();
            environment.VisibleDistance = 250;
            environment.FogEnabled = true;
            environment.FogColor = new Vector3(0.5f, 0.5f, 0.5f);
            environment.FogStartDistance = 200;
            environment.FogEndDistance = 250;
            environment.ShadowFadeStartDistance = 200;
            environment.ShadowFadeEndDistance = 250;
            environment.ShadowCasterDistance = 250;

            

            // Apply the user preferences (example - not required).
            sceneInterface.ApplyPreferences(preferences);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            
            sceneInterface.Unload();
            lightingSystemManager.Unload();

            environment = null;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        KeyboardState prev_ks = new KeyboardState();
        protected override void Update(GameTime gameTime)
        {
            KeyboardState ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.Escape))
                this.Exit();

            view = ProcessCameraInput(gameTime);
        

            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(70.0f),
                graphics.GraphicsDevice.Viewport.AspectRatio, 0.1f, environment.VisibleDistance);

            sceneInterface.Update(gameTime);

            if (ks.IsKeyDown(Keys.OemTilde) && !prev_ks.IsKeyDown(Keys.OemTilde))
                DEBUG = !DEBUG;
            voxelManager.Update();

            prev_ks = ks;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Check to see if the splash screen is finished.
            if (!SplashScreenGameComponent.DisplayComplete)
            {
                base.Draw(gameTime);
                return;
            }
            
            // Render the scene.            
            sceneState.BeginFrameRendering(view, projection, gameTime, environment, true);
            sceneInterface.BeginFrameRendering(sceneState);            
            

            // Add custom rendering that should occur before the scene is rendered.

            sceneInterface.RenderManager.Render();

            // Add custom rendering that should occur after the scene is rendered.

            sceneInterface.EndFrameRendering();
            sceneState.EndFrameRendering();


            // Get SunBurn's stats for the frame.
            //totalObjectCountStat.AccumulationValue = instancesPerContainerObject * containerObjectCount;

            if (DEBUG)
                LightingSystemStatistics.Render(GraphicsDevice, LightingSystemStatisticCategory.Rendering, new Vector2(20.0f), Vector2.One, Color.White, gameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Handles controller input.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public Matrix ProcessCameraInput(GameTime gameTime)
        {
            if (IsActive)
            {
                GamePadState gamepad = GamePad.GetState(PlayerIndex.One);
                KeyboardState keyboard = Keyboard.GetState();
                MouseState mouse = Mouse.GetState();

                float timescale = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float rotatescale = 3.0f * timescale;
                float movescale = timescale * moveScale;

                // Get the right trigger, which affects speed.
                if (gamepad.IsConnected)
                {
                    rotatescale *= (1.0f - gamepad.Triggers.Right * 0.5f);
                    movescale *= (1.0f - gamepad.Triggers.Right * 0.5f);
                }
                else if (mouse.RightButton == ButtonState.Pressed)
                    movescale *= 0.25f;

                // If the gamepad is connected use its input instead of the mouse and keyboard.
                if (gamepad.IsConnected)
                    viewRotation -= new Vector3(gamepad.ThumbSticks.Right.X * rotatescale, gamepad.ThumbSticks.Right.Y * rotatescale, 0.0f);
                else
                {
                    GraphicsDevice device = graphics.GraphicsDevice;
                    int halfx = device.Viewport.Width / 2;
                    int halfy = device.Viewport.Height / 2;

                    if (!firstMouseSample)
                    {
                        // Convert the amount the mouse was moved into camera rotation.
                        viewRotation.X += MathHelper.ToRadians((float)(halfx - mouse.X) * rotatescale * 1.5f);
                        viewRotation.Y -= MathHelper.ToRadians((float)(halfy - mouse.Y) * rotatescale * 1.5f);
                    }
                    else
                        firstMouseSample = false;

                    Mouse.SetPosition(halfx, halfy);
                }

                if (viewRotation.Y > MathHelper.PiOver2 - 0.01f)
                    viewRotation.Y = MathHelper.PiOver2 - 0.01f;
                else if (viewRotation.Y < -MathHelper.PiOver2 + 0.01f)
                    viewRotation.Y = -MathHelper.PiOver2 + 0.01f;

                Quaternion rot = Quaternion.CreateFromYawPitchRoll(viewRotation.X, viewRotation.Y, viewRotation.Z);

                // Now apply the camera movement based on either the gamepad or keyboard input.
                if (gamepad.IsConnected)
                {
                    viewPosition += Vector3.Transform(new Vector3(movescale, 0, movescale) * new Vector3(
                        -gamepad.ThumbSticks.Left.X, 0,
                        gamepad.ThumbSticks.Left.Y), rot);
                }
                else
                {
                    Vector3 move = new Vector3();
                    if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                        move.Z += 1.0f;
                    if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                        move.Z -= 1.0f;
                    if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                        move.X += 1.0f;
                    if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                        move.X -= 1.0f;
                    viewPosition += Vector3.Transform(new Vector3(movescale, 0, movescale) * move, rot);
                }
            }

            // mouse visibility...
            if (!IsActive || GamePad.GetState(PlayerIndex.One).IsConnected)
                IsMouseVisible = true;
            else
                IsMouseVisible = false;

            // Convert the camera rotation and movement into a view transform.
            return GetViewMatrix();
        }

        /// <summary>
        /// Convert the camera rotation and movement into a view transform.
        /// </summary>
        /// <returns></returns>
        private Matrix GetViewMatrix()
        {
            Matrix rotation = Matrix.CreateFromYawPitchRoll(viewRotation.X, viewRotation.Y, viewRotation.Z);
            Vector3 target = viewPosition + Vector3.Transform(Vector3.Backward, rotation);
            return Matrix.CreateLookAt(viewPosition, target, Vector3.Up);
        }
    }
}
