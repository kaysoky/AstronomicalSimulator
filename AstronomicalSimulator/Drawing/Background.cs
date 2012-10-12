using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Background
    {
        VertexDeclaration GridDeclaration;
        VertexPositionColor[] Lines = new VertexPositionColor[256 * 6];

        VertexDeclaration BackgroundDeclaration;
        Texture2D StarMap;
        VertexPositionColorTexture[] Stars = new VertexPositionColorTexture[0];
        int[] StarIndex;

        VertexPositionColorTexture[] SkyBox;
        int[] SkyBoxIndexBuffer;
        Texture2D SkyTexture1;
        Texture2D SkyTexture2;

        Texture2D ColorMap;
        Color[] ColorScheme1;
        Color[] ColorScheme2;
        float TransparencyThreshold;

        public Background()
        {
            //Initialize the Grid
            int xCoord = -127;
            for (int i = 0; i < 255; i++)
            {
                Lines[i * 3] = new VertexPositionColor(
                    new Vector3(xCoord, 0, -127)
                    , new Color(Color.MidnightBlue, 0.0f));
                Lines[i * 3 + 1] = new VertexPositionColor(
                    new Vector3(xCoord, 0, 0)
                    , new Color(Color.DeepSkyBlue
                        , 1.0f - Math.Abs(xCoord * 2.0f) / 255.0f));
                Lines[i * 3 + 2] = new VertexPositionColor(
                    new Vector3(xCoord, 0, 127)
                    , new Color(Color.DodgerBlue, 0.0f));
                xCoord++;
            }
            for (int i = 255; i < 510; i++)
            {
                Lines[i * 3] = new VertexPositionColor(
                    new Vector3(-127, 0, xCoord)
                    , new Color(Color.DarkBlue, 0.0f));
                Lines[i * 3 + 1] = new VertexPositionColor(
                    new Vector3(0, 0, xCoord)
                    , new Color(Color.Blue
                        , 1.0f - Math.Abs(xCoord * 2.0f) / 255f));
                Lines[i * 3 + 2] = new VertexPositionColor(
                    new Vector3(127, 0, xCoord)
                    , new Color(Color.LightBlue, 0.0f));
                xCoord--;
            }
            GridDeclaration = new VertexDeclaration(MyGame.graphics.GraphicsDevice, VertexPositionColor.VertexElements);
            BackgroundDeclaration = new VertexDeclaration(MyGame.graphics.GraphicsDevice, VertexPositionColorTexture.VertexElements);
            StarMap = MyGame.content.Load<Texture2D>("Textures\\Stars");

            //Initialize the Stars
            RefreshBackground(10000);
            
            //Initialize the Skybox
            int width = 32;
            int height = 32;
            SkyTexture1 = Manager.GeneratePerlinNoise(MyGame.random.Next(width / 2, (width + height) / 2));
            SkyTexture1 = Manager.SphericalWrap(SkyTexture1);
            SkyTexture2 = Manager.GeneratePerlinNoise(MyGame.random.Next(width / 2, (width + height) / 2));
            SkyTexture2 = Manager.SphericalWrap(SkyTexture2);
            TransparencyThreshold = 0.3f + 0.1f * (float)MyGame.random.NextDouble();

            SkyBox = new VertexPositionColorTexture[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SkyBox[x + y * width] = new VertexPositionColorTexture(
                        new Vector3((float)Math.Cos((double)x / (width - 1.0) * 2.0 * Math.PI)
                                * (float)Math.Sin((double)y / (height - 1.0) * Math.PI)
                            , -(float)Math.Cos((double)y / (height - 1.0) * Math.PI)
                            , (float)(Math.Sin((double)x / (width - 1.0) * 2.0 * Math.PI))
                                * (float)Math.Sin((double)y / (height - 1.0) * Math.PI))
                        , Color.White
                        , new Vector2((float)x / (width - 1.0f), (float)y / (height - 1.0f)));
                }
            }
            int counter = 0;
            SkyBoxIndexBuffer = new int[6 * (width - 1) * (height - 1)];
            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    SkyBoxIndexBuffer[counter++] = x + y * width;
                    SkyBoxIndexBuffer[counter++] = x + 1 + (y + 1) * width;
                    SkyBoxIndexBuffer[counter++] = x + 1 + y * width;
                    SkyBoxIndexBuffer[counter++] = x + y * width;
                    SkyBoxIndexBuffer[counter++] = x + (y + 1) * width;
                    SkyBoxIndexBuffer[counter++] = x + 1 + (y + 1) * width;
                }
            }

            //Set the colors
            ColorScheme1 = Manager.GenerateStaticNoise(5, 5);
            ColorScheme2 = Manager.GenerateStaticNoise(5, 5);
        }

        public void Update(GameTime gameTime)
        {
            float Zoom = Manager.CameraLocation.Length();
            if (Zoom < 1.0f)
            {
                RefreshBackground(10000);
            }
            else if (Zoom < 1000.0f)
            {
                RefreshBackground(10000 - 9 * (int)Zoom);
            }
            else
            {
                RefreshBackground(1000);
            }
        }

        public void Draw()
        {
            Color[] AvgColorScheme = new Color[25];
            for (int i = 0; i < AvgColorScheme.Length; i++)
            {
                AvgColorScheme[i] = Color.Lerp(ColorScheme1[i], ColorScheme2[i], 0.5f);
            }
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice
                , 5
                , 5);
            ColorMap.SetData<Color>(AvgColorScheme);
            //Draw background
            MyGame.graphics.GraphicsDevice.VertexDeclaration = BackgroundDeclaration;
            Manager.spriteBatch.Begin();
            //Manager.spriteBatch.Draw(skyTexture, Manager.GameWindow, Color.White);
            Manager.spriteBatch.End();
            float Scale = Manager.CameraLocation.Length();
            if (Scale < 1.0f)
            {
                Scale = 1.0f;
            }
            DrawSkyBox(Scale);
            DrawStars(Scale);
            //DrawGrid(Scale);
        }
        void DrawSkyBox(float Scale)
        {
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["Textured"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(SkyTexture1);
            Manager.TexturedEffect.Parameters["InputTexture2"].SetValue(SkyTexture2);
            Manager.TexturedEffect.Parameters["InputTextureInterpolation"].SetValue(1.0f - (float)Math.Pow(1.0 / Scale, 0.3));
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(TransparencyThreshold);
            Manager.TexturedEffect.Parameters["World"].SetValue(Matrix.CreateScale(10.0f * Scale)
                * Matrix.CreateTranslation(Manager.CameraFocus));
            Manager.TexturedEffect.Begin();
            Manager.TexturedEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            MyGame.graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColorTexture>(
                PrimitiveType.TriangleList
                , SkyBox
                , 0
                , SkyBox.Length
                , SkyBoxIndexBuffer
                , 0
                , SkyBoxIndexBuffer.Length / 3);
            Manager.TexturedEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            Manager.TexturedEffect.End();
        }
        void DrawStars(float Scale)
        {
            Manager.ResetFor2D();
            Manager.OrdinaryEffect.CurrentTechnique = Manager.OrdinaryEffect.Techniques["PointSprite"];
            Manager.OrdinaryEffect.Parameters["OrdinaryTransparency"].SetValue(1.0f);
            Manager.OrdinaryEffect.Parameters["CameraPosition"].SetValue(Manager.CameraFocus + Manager.CameraLocation);
            Manager.OrdinaryEffect.Parameters["CameraUpVector"].SetValue(Manager.CameraUp);
            Manager.OrdinaryEffect.Parameters["PointSpriteSize"].SetValue(0.15f * Scale);
            Manager.OrdinaryEffect.Parameters["TextureFraction"].SetValue(0.1f);
            Manager.OrdinaryEffect.Parameters["InputTexture"].SetValue(StarMap);
            Manager.OrdinaryEffect.Parameters["World"].SetValue(Matrix.CreateScale(10.0f * Scale)
                * Matrix.CreateTranslation(Manager.CameraFocus));
            Manager.OrdinaryEffect.Begin();
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            MyGame.graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColorTexture>(
                PrimitiveType.TriangleList
                , Stars
                , 0
                , Stars.Length
                , StarIndex
                , 0
                , StarIndex.Length / 3);
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            Manager.OrdinaryEffect.End();
        }
        void DrawGrid(float Scale)
        {
            MyGame.graphics.GraphicsDevice.VertexDeclaration = GridDeclaration;
            Manager.OrdinaryEffect.CurrentTechnique = Manager.OrdinaryEffect.Techniques["Ordinary"];
            Manager.OrdinaryEffect.Parameters["World"].SetValue(Matrix.CreateScale(0.1f * (float)Math.Sqrt(Scale)));
            Manager.OrdinaryEffect.Begin();
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            MyGame.graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(
                PrimitiveType.LineStrip
                , Lines
                , 0
                , Lines.Length - 1);
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            Manager.OrdinaryEffect.End();
            MyGame.graphics.GraphicsDevice.VertexDeclaration = BackgroundDeclaration;
        }
        /// <summary>
        /// Re-randomizes the stars drawn in the far area
        /// </summary>
        void RefreshBackground(int numStars)
        {
            if (numStars > Stars.Length / 4)
            {
                int PreviousStarNumber = Stars.Length / 4;
                Array.Resize<VertexPositionColorTexture>(ref Stars, numStars * 4);
                Array.Resize<int>(ref StarIndex, numStars * 6);
                for (int i = PreviousStarNumber; i < numStars; i++)
                {
                    Vector3 position = Manager.GetRandomNormal()
                        * (0.6f + 0.4f * (float)MyGame.random.NextDouble());
                    Vector2 texture = new Vector2(0.1f * MyGame.random.Next(10), 0.1f * MyGame.random.Next(10));
                    Stars[i * 4] = new VertexPositionColorTexture(
                        position
                        , Manager.GetRandomColor(true)
                        , texture);
                    Stars[i * 4 + 1] = new VertexPositionColorTexture(
                        position
                        , Manager.GetRandomColor(true)
                        , texture + Vector2.UnitX * 0.099f);
                    Stars[i * 4 + 2] = new VertexPositionColorTexture(
                        position
                        , Manager.GetRandomColor(true)
                        , texture + Vector2.UnitY * 0.099f);
                    Stars[i * 4 + 3] = new VertexPositionColorTexture(
                        position
                        , Manager.GetRandomColor(true)
                        , texture + Vector2.One * 0.099f);
                    StarIndex[i * 6] = i * 4;
                    StarIndex[i * 6 + 1] = i * 4 + 1;
                    StarIndex[i * 6 + 2] = i * 4 + 3;
                    StarIndex[i * 6 + 3] = i * 4;
                    StarIndex[i * 6 + 4] = i * 4 + 3;
                    StarIndex[i * 6 + 5] = i * 4 + 2;
                }
            }
            else if (numStars < Stars.Length / 4)
            {
                Array.Resize<int>(ref StarIndex, numStars * 6);
                //Randomize the order of stars a bit
                int numChanges = Stars.Length / 4 - numStars;
                for (int i = 0; i < numChanges; i++)
                {
                    int OtherIndex = MyGame.random.Next(Stars.Length / 4);
                    VertexPositionColorTexture temp;
                    for (int j = 0; j < 4; j++)
                    {
                        temp = Stars[i * 4 + j];
                        Stars[i * 4 + j] = Stars[OtherIndex * 4 + j];
                        Stars[OtherIndex * 4 + j] = temp;
                    }
                }
                Array.Resize<VertexPositionColorTexture>(ref Stars, numStars * 4);
            }
        }
    }
}
