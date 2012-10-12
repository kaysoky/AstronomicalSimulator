using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

/*To Do List;
 * Determine how to make a cloud field
 *   Asteroids and Nebulae -> Small boxes of graphics with normals ?
 * Improve Nebulae and Galaxy graphics
 * Detect collisions and allow aggregation and breakup
 */
namespace AstronomicalSimulator
{
    public class MyGame : Microsoft.Xna.Framework.Game
    {
        public static GraphicsDeviceManager graphics;
        public static ContentManager content;
        public static Random random;

        Texture2D PleaseWaitTexture;
        Drawing.Manager drawingManager;

        public MyGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            content = this.Content;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.IsFullScreen = false;  //For debug purposes, full screen is off
            graphics.ApplyChanges();
            Window.AllowUserResizing = false;
            IsMouseVisible = false;

            random = new Random();

            PleaseWaitTexture = content.Load<Texture2D>("Textures\\PleaseWait");
            ThreadPool.QueueUserWorkItem(new WaitCallback(InitializeDrawingManager), new SpriteBatch(graphics.GraphicsDevice));

            base.Initialize();
        }
        bool isInitialized = false;
        void InitializeDrawingManager(object spriteBatch)
        {
            drawingManager = new Drawing.Manager((SpriteBatch)spriteBatch);
            isInitialized = true;
        }

        protected override void LoadContent()
        {

        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            if (isInitialized)
            {
                drawingManager.Update(gameTime);
            }

            base.Update(gameTime);
        }

        public static bool isDrawing = false;
        protected override void Draw(GameTime gameTime)
        {
            while (Drawing.Manager.isUsingGraphicsDevice)
            {
                Thread.Sleep(10);
            }
            isDrawing = true;
            GraphicsDevice.Clear(Color.Black);

            if (isInitialized)
            {
                drawingManager.Draw(gameTime);
            }
            else
            {
                SpriteBatch temp = new SpriteBatch(graphics.GraphicsDevice);
                temp.Begin();
                temp.Draw(PleaseWaitTexture
                    , new Vector2(graphics.PreferredBackBufferWidth - PleaseWaitTexture.Width
                        , graphics.PreferredBackBufferHeight - PleaseWaitTexture.Height) / 2.0f
                    , Color.White);
                temp.End();
            }

            base.Draw(gameTime);
            isDrawing = false;
        }
    }
}
