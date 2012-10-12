using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AstronomicalSimulator.Drawing
{
    class Manager
    {
        public static SpriteBatch spriteBatch;
        public static SpriteFont Kootenay16;
        public static string DebugText = "";

        /// <summary>
        /// For any complex models, this corrects the changes the SpriteBatch makes to the MyGame.graphicsDevice
        /// Prevents triangles in the back from being drawn in the front
        /// </summary>
        public static void ResetFor3D()
        {
            MyGame.graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
            MyGame.graphics.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            MyGame.graphics.GraphicsDevice.RenderState.AlphaTestEnable = true;
        }
        /// <summary>
        /// Makes 3D drawing behave like 2D drawing
        /// </summary>
        public static void ResetFor2D()
        {
            MyGame.graphics.GraphicsDevice.RenderState.DepthBufferEnable = false;
            MyGame.graphics.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            MyGame.graphics.GraphicsDevice.RenderState.AlphaTestEnable = true;
        }

        public static Cursor cursor = new Cursor();
        public static Rectangle GameWindow;

        public static Vector3 CameraFocus;
        public static Vector3 CameraLocation;
        public static Vector3 CameraUp;
        public static Matrix View;
        public static Matrix Projection;

        public static KeyboardState PreviousKeyboard;
        public static MouseState PreviousMouse;

        public static Effect OrdinaryEffect;
        public static Effect DiffuseEffect;
        public static Effect TexturedEffect;
        public static Effect PostProcessEffect;

        public static Texture2D BlankWhiteTexture;
        public static Texture2D[] WrappedNoiseTextures;
        Texture2D FocusTexture;

        Background background;
        List<Body> CelestialBodies = new List<Body>();
        List<Body> ContainingBodies = new List<Body>();
        List<Body> VisibleBodies = new List<Body>();
        Body FocalBody;
        double FocalBodyFocusTimer = 1.0;

        public Manager(SpriteBatch spriteBatch) 
        {
            Manager.GameWindow = new Rectangle(0, 0, MyGame.graphics.PreferredBackBufferWidth, MyGame.graphics.PreferredBackBufferHeight);
            Manager.spriteBatch = spriteBatch;

            Kootenay16 = MyGame.content.Load<SpriteFont>("Kootenay16");

            Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4
                , MyGame.graphics.GraphicsDevice.Viewport.AspectRatio
                , 0.001f
                , 1000.0f);
            CameraFocus = Vector3.Zero;
            CameraLocation = 1000.0f * Vector3.One;
            CameraUp = Vector3.UnitY;
            View = Matrix.CreateLookAt(
                CameraFocus + CameraLocation
                , CameraFocus
                , CameraUp);

            PreviousKeyboard = Keyboard.GetState();
            PreviousMouse = Mouse.GetState();

            OrdinaryEffect = MyGame.content.Load<Effect>("Effects\\Ordinary");
            DiffuseEffect = MyGame.content.Load<Effect>("Effects\\Diffuse");
            TexturedEffect = MyGame.content.Load<Effect>("Effects\\Textured");
            PostProcessEffect = MyGame.content.Load<Effect>("Effects\\PostProcess");

            BlankWhiteTexture = MyGame.content.Load<Texture2D>("Textures\\BlankBox");
            WrappedNoiseTextures = new Texture2D[25];
            for (int i = 0; i < WrappedNoiseTextures.Length; i++)
            {
                WrappedNoiseTextures[i] = GeneratePerlinNoise(MyGame.random.Next(10, 25));
                WrappedNoiseTextures[i] = SphericalWrap(WrappedNoiseTextures[i]);
            }
            FocusTexture = MyGame.content.Load<Texture2D>("Textures\\Target");

            //Initialize the background
            background = new Background();

            //Get the starting view
            CelestialBodies.Add(new Nebula(Vector3.Zero));
            Body.DefaultDeclaration = new VertexDeclaration(MyGame.graphics.GraphicsDevice, VertexPositionTexture.VertexElements);
        }

        public void Update(GameTime gameTime)
        {
            DebugText = "";

            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            UpdateCamera(gameTime, keyboard, mouse);
            cursor.Update();

            //Update the _3DObjects
            for (int i = 0; i < CelestialBodies.Count; i++)
            {
                CelestialBodies[i].Update(gameTime, Vector3.Zero, 0.0f);
                if (CelestialBodies[i].isInactive)
                {
                    CelestialBodies.RemoveAt(i--);
                    continue;
                }
            }
            #region Mouse Processing
            ContainingBodies.Clear();
            VisibleBodies.Clear();
            Ray ScreenCenter = Cursor.Unproject(new Vector2(GameWindow.Width, GameWindow.Height) / 2.0f);
            BoundingFrustum VisibleArea = new BoundingFrustum(
                View * Projection);
            Body.PartiallySortBodies(ScreenCenter, VisibleArea, ref CelestialBodies, ref VisibleBodies, ref ContainingBodies);
            for (int i = 0; i < VisibleBodies.Count; i++)
            {
                if (mouse.LeftButton == ButtonState.Pressed
                    && PreviousMouse.LeftButton == ButtonState.Released)
                {
                    float ButtonScale = 250.0f / (i + 10);
                    float DistanceToButtonCenter = (float)Math.Sqrt(Math.Pow(mouse.X - VisibleBodies[i].ScreenPosition.X, 2.0)
                        + Math.Pow(mouse.Y - VisibleBodies[i].ScreenPosition.Y, 2.0));
                    if (DistanceToButtonCenter <= ButtonScale)
                    {
                        FocalBody = VisibleBodies[i];
                        FocalBodyFocusTimer = 1.0;
                    }
                }
            }
            #endregion

            PreviousKeyboard = keyboard;
            PreviousMouse = mouse;
        }
        public void UpdateCamera(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            //Find the three spatial directions
            Vector3 forward = Vector3.Normalize(-CameraLocation);
            Vector3 side = Vector3.Normalize(Vector3.Cross(forward, CameraUp));
            CameraUp = Vector3.Normalize(Vector3.Cross(side, forward));
            float panSpeed = 0.75f * CameraLocation.Length();
            //The arrow pad rotates the screen 
            if (keyboard.IsKeyDown(Keys.Up))
            {
                CameraLocation = Vector3.Transform(CameraLocation
                    , Matrix.CreateFromAxisAngle(side, 0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            if (keyboard.IsKeyDown(Keys.Down))
            {
                CameraLocation = Vector3.Transform(CameraLocation
                    , Matrix.CreateFromAxisAngle(side, -0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            if (keyboard.IsKeyDown(Keys.Q))
            {
                CameraUp = Vector3.Transform(CameraUp
                    , Matrix.CreateFromAxisAngle(forward, -0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            if (keyboard.IsKeyDown(Keys.E))
            {
                CameraUp = Vector3.Transform(CameraUp
                    , Matrix.CreateFromAxisAngle(forward, 0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            float endLength = CameraLocation.Length();
            endLength += (PreviousMouse.ScrollWheelValue - mouse.ScrollWheelValue)
                 * panSpeed / 8.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            //Page up/down and the mouse wheel controls zoom level
            if (keyboard.IsKeyDown(Keys.PageUp))
            {
                endLength -= panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (keyboard.IsKeyDown(Keys.PageDown))
            {
                endLength += panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            CameraLocation =
                Vector3.Normalize(CameraLocation)
                * endLength;
            if (endLength < 1.0f)
            {
                endLength = 1.0f;
            }
            Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4
                , MyGame.graphics.GraphicsDevice.Viewport.AspectRatio
                , endLength * 0.0005f
                , 1000.0f + endLength * 25.0f);
            //WASD controls panning
            if (keyboard.IsKeyDown(Keys.Left))
            {
                CameraLocation = Vector3.Transform(CameraLocation
                    , Matrix.CreateFromAxisAngle(CameraUp, 0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            if (keyboard.IsKeyDown(Keys.Right))
            {
                CameraLocation = Vector3.Transform(CameraLocation
                    , Matrix.CreateFromAxisAngle(CameraUp, -0.75f * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
            if (keyboard.IsKeyDown(Keys.W))
            {
                CameraFocus += forward * panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                FocalBody = null;
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                CameraFocus -= forward * panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                FocalBody = null;
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                CameraFocus += side * panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                FocalBody = null;
            }
            if (keyboard.IsKeyDown(Keys.A))
            {
                CameraFocus -= side * panSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                FocalBody = null;
            }
            if (FocalBody != null)
            {
                FocalBodyFocusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (FocalBodyFocusTimer < 0.0)
                {
                    FocalBodyFocusTimer = 0.0;
                }
                CameraFocus = Vector3.Lerp(CameraFocus, FocalBody.Bounds.Center
                    , (float)Math.Pow(1.0 - FocalBodyFocusTimer, 1.5) + (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            //Recalculate the camera
            View = Matrix.CreateLookAt(
                CameraLocation + CameraFocus
                , CameraFocus
                , CameraUp);
        }

        public void Draw(GameTime gameTime)
        {
            //Set the camera matrix for all the effects
            Matrix ViewXProjection = View * Projection;
            OrdinaryEffect.Parameters["ViewXProjection"].SetValue(ViewXProjection);
            DiffuseEffect.Parameters["ViewXProjection"].SetValue(ViewXProjection);
            TexturedEffect.Parameters["ViewXProjection"].SetValue(ViewXProjection);
            PostProcessEffect.Parameters["ViewXProjection"].SetValue(ViewXProjection);

            spriteBatch.Begin();
            spriteBatch.Draw(BlankWhiteTexture, Vector2.Zero, Color.TransparentWhite);
            spriteBatch.End();

            background.Draw();

            //Sort objects according to distance from camera
            BoundingFrustum VisibleArea = new BoundingFrustum(
                Matrix.CreateLookAt(
                    CameraLocation + CameraFocus
                    , CameraFocus
                    , CameraUp) * Projection);
            //Draw the objects
            for (int i = 0; i < CelestialBodies.Count; i++)
            {
                CelestialBodies[i].Draw(VisibleArea, Vector3.Zero);
            }
            spriteBatch.Begin();
            for (int i = 0; i < ContainingBodies.Count; i++)
            {
                spriteBatch.Draw(FocusTexture, ContainingBodies[i].ScreenPosition
                    , null
                    , new Color(ContainingBodies[i].DisplayColor, 0.4f * ContainingBodies[i].DisplayOpacity)
                    , 0.0f
                    , Vector2.One * 50.0f
                    , 5.0f / (i + 10)
                    , SpriteEffects.None
                    , 0.0f);
            }
            for (int i = 0; i < VisibleBodies.Count; i++)
            {
                spriteBatch.Draw(FocusTexture, VisibleBodies[i].ScreenPosition
                    , null
                    , new Color(VisibleBodies[i].DisplayColor, 0.4f * VisibleBodies[i].DisplayOpacity)
                    , 0.0f
                    , Vector2.One * 50.0f
                    , 5.0f / (i + 10)
                    , SpriteEffects.None
                    , 0.0f);
            }
            //Draw a five pixel black border around the edge
            spriteBatch.Draw(BlankWhiteTexture
                , new Rectangle(0, 0, 5, GameWindow.Height)
                , Color.Black);
            spriteBatch.Draw(BlankWhiteTexture
                , new Rectangle(GameWindow.Width - 5, 0, 5, GameWindow.Height)
                , Color.Black);
            spriteBatch.Draw(BlankWhiteTexture
                , new Rectangle(0, 0, GameWindow.Width, 5)
                , Color.Black);
            spriteBatch.Draw(BlankWhiteTexture
                , new Rectangle(0, GameWindow.Height - 5, GameWindow.Width, 5)
                , Color.Black);
            spriteBatch.DrawString(Kootenay16, DebugText, Vector2.One * 5.0f, Color.White);
            spriteBatch.End();

            cursor.Draw();
        }

        /// <summary>
        /// Provides the transformation matrix from one normal to another
        /// </summary>
        /// <param name="objectNormal">The starting normal of the object</param>
        /// <param name="desiredNormal">The resulting normal after rotation</param>
        public static Matrix GetRotationFromNormal(Vector3 objectNormal, Vector3 desiredNormal)
        {
            objectNormal.Normalize();
            desiredNormal.Normalize();
            Vector3 axis = Vector3.Cross(objectNormal
                , desiredNormal);
            axis.Normalize();
            float dotAngle = Vector3.Dot(objectNormal
                , desiredNormal);
            float angle = 0f;
            if (dotAngle < 0)
            {
                angle = MathHelper.PiOver2 + (float)Math.Asin(Math.Abs(dotAngle));
            }
            else
            {
                angle = (float)Math.Acos(dotAngle);
            }
            if (float.IsNaN(angle))
            {
                return Matrix.Identity;
            }
            else
            {
                return Matrix.CreateFromAxisAngle(axis, angle);
            }

        }

        /// <summary>
        /// Generates a normalized vector of random direction
        /// </summary>
        public static Vector3 GetRandomNormal()
        {
            return Vector3.Normalize(new Vector3(
                   0.5f - (float)MyGame.random.NextDouble()
                   , 0.5f - (float)MyGame.random.NextDouble()
                   , 0.5f - (float)MyGame.random.NextDouble()));
        }

        /// <summary>
        /// Generates a color with random RGB components
        /// </summary>
        public static Color GetRandomColor(bool modulateAlpha)
        {
            Color color = new Color((float)MyGame.random.NextDouble()
                , (float)MyGame.random.NextDouble()
                , (float)MyGame.random.NextDouble());
            if (modulateAlpha)
            {
                color.A = (byte)MyGame.random.Next(byte.MaxValue);
            }
            return color;
        }

        public static bool isUsingGraphicsDevice = false;

        /// <summary>
        /// Returns a custom texture of Perlin Noise
        /// </summary>
        public static Texture2D GeneratePerlinNoise(int TextureWidth, int TextureHeight
            , Color[] NoiseData, int NoiseWidth, int NoiseHeight, float NoiseShift, float Sharpness)
        {
            isUsingGraphicsDevice = true;
            while (MyGame.isDrawing)
            {
                Thread.Sleep(10);
            }
            RenderTarget2D renderTarget = new RenderTarget2D(MyGame.graphics.GraphicsDevice
                , TextureWidth
                , TextureHeight
                , 1
                , MyGame.graphics.GraphicsDevice.DisplayMode.Format);
            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, renderTarget);
            DepthStencilBuffer OriginalBuffer = MyGame.graphics.GraphicsDevice.DepthStencilBuffer;
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = new DepthStencilBuffer(MyGame.graphics.GraphicsDevice
                , renderTarget.Width
                , renderTarget.Height
                , MyGame.graphics.GraphicsDevice.DepthStencilBuffer.Format);

            Texture2D noiseMap = new Texture2D(MyGame.graphics.GraphicsDevice
                , NoiseWidth
                , NoiseHeight);
            noiseMap.SetData<Color>(NoiseData);

            PostProcessEffect.CurrentTechnique = PostProcessEffect.Techniques["GenerateNoise"];
            PostProcessEffect.Parameters["InputTexture"].SetValue(noiseMap);
            PostProcessEffect.Parameters["World"].SetValue(Matrix.Identity);
            PostProcessEffect.Parameters["NoiseShift"].SetValue(NoiseShift);
            PostProcessEffect.Parameters["Sharpness"].SetValue(Sharpness);
            spriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            PostProcessEffect.Begin();
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            spriteBatch.Draw(noiseMap
                , new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)
                , Color.White);
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            PostProcessEffect.End();
            spriteBatch.End();

            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, null);
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = OriginalBuffer;
            MyGame.graphics.GraphicsDevice.Clear(Color.Black);
            
            isUsingGraphicsDevice = false;
            return renderTarget.GetTexture();
        }
        /// <summary>
        /// Returns a 1000 by 1000 pixel texture of Perlin Noise
        /// </summary>
        /// <param name="resolution">The amount of static to generate the noise from</param>
        public static Texture2D GeneratePerlinNoise(int Resolution)
        {
            return GeneratePerlinNoise(1000, 1000
                , GenerateStaticNoise(Resolution, Resolution), Resolution, Resolution
                , (float)MyGame.random.NextDouble(), 0.9f + 0.1f * (float)MyGame.random.NextDouble());
        }

        /// <summary>
        /// Deflates the input texture sinusoidally at the top and bottom
        /// Partially copies some colors from the right edge to the left edge
        /// </summary>
        public static Texture2D SphericalWrap(Texture2D texture)
        {
            isUsingGraphicsDevice = true;
            while (MyGame.isDrawing)
            {
                Thread.Sleep(10);
            }
            RenderTarget2D renderTarget = new RenderTarget2D(MyGame.graphics.GraphicsDevice
                , texture.Width
                , texture.Width
                , 1
                , MyGame.graphics.GraphicsDevice.DisplayMode.Format);
            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, renderTarget);
            DepthStencilBuffer OriginalBuffer = MyGame.graphics.GraphicsDevice.DepthStencilBuffer;
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = new DepthStencilBuffer(MyGame.graphics.GraphicsDevice
                , renderTarget.Width
                , renderTarget.Height
                , MyGame.graphics.GraphicsDevice.DepthStencilBuffer.Format);

            PostProcessEffect.CurrentTechnique = PostProcessEffect.Techniques["SphericalWrap"];
            PostProcessEffect.Parameters["InputTexture"].SetValue(texture);
            PostProcessEffect.Parameters["World"].SetValue(Matrix.Identity);
            PostProcessEffect.Parameters["WrapMagnitude"].SetValue(0.1f + 0.2f * (float)MyGame.random.NextDouble());
            spriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            PostProcessEffect.Begin();
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            spriteBatch.Draw(texture
                , new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)
                , Color.White);
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            PostProcessEffect.End();
            spriteBatch.End();

            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, null);
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = OriginalBuffer;
            MyGame.graphics.GraphicsDevice.Clear(Color.Black);

            isUsingGraphicsDevice = false;
            return renderTarget.GetTexture();
        }
        /// <summary>
        /// Erases the corners and center of input texture and warps the rest into a spiral
        /// </summary>
        public static Texture2D SpiralWarp(Texture2D texture)
        {
            isUsingGraphicsDevice = true;
            while (MyGame.isDrawing)
            {
                Thread.Sleep(10);
            }
            RenderTarget2D renderTarget = new RenderTarget2D(MyGame.graphics.GraphicsDevice
                , texture.Width
                , texture.Width
                , 1
                , MyGame.graphics.GraphicsDevice.DisplayMode.Format);
            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, renderTarget);
            DepthStencilBuffer OriginalBuffer = MyGame.graphics.GraphicsDevice.DepthStencilBuffer;
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = new DepthStencilBuffer(MyGame.graphics.GraphicsDevice
                , renderTarget.Width
                , renderTarget.Height
                , MyGame.graphics.GraphicsDevice.DepthStencilBuffer.Format);

            PostProcessEffect.CurrentTechnique = PostProcessEffect.Techniques["SpiralWarp"];
            PostProcessEffect.Parameters["InputTexture"].SetValue(texture);
            PostProcessEffect.Parameters["World"].SetValue(Matrix.Identity);
            PostProcessEffect.Parameters["WarpMagnitude"].SetValue(2.5f + 1.5f * (float)MyGame.random.NextDouble());
            spriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            PostProcessEffect.Begin();
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            spriteBatch.Draw(texture
                , new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)
                , Color.White);
            PostProcessEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            PostProcessEffect.End();
            spriteBatch.End();

            MyGame.graphics.GraphicsDevice.SetRenderTarget(0, null);
            MyGame.graphics.GraphicsDevice.DepthStencilBuffer = OriginalBuffer;
            MyGame.graphics.GraphicsDevice.Clear(Color.Black);

            isUsingGraphicsDevice = false;
            return renderTarget.GetTexture();
        }

        /// <summary>
        /// Returns a block of bright-ish colors
        /// </summary>
        public static Color[] GenerateStaticNoise(int horizontalResolution, int verticalResolution)
        {
            Color[] noiseData = new Color[horizontalResolution * verticalResolution];
            for (int x = 0; x < horizontalResolution; x++)
            {
                for (int y = 0; y < verticalResolution; y++)
                {
                    noiseData[y * horizontalResolution + x] = GetRandomColor(false);
                }
            }
            return noiseData;
        }
    }
}
