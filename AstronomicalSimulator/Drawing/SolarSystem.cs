using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class SolarSystem : Body
    {
        static Color[] starColors =
            new Color[] { Color.Aqua, Color.LightYellow, Color.LightSkyBlue
                , Color.BlueViolet, Color.Chartreuse, Color.Cornsilk
                , Color.Crimson, Color.Cyan, Color.DarkRed
                , Color.DarkViolet, Color.DimGray, Color.Firebrick
                , Color.Fuchsia, Color.Pink, Color.Gold
                , Color.Honeydew, Color.Ivory, Color.Lavender
                , Color.LemonChiffon, Color.LightCoral, Color.LightPink
                , Color.Lime, Color.Magenta, Color.MediumPurple
                , Color.MediumTurquoise, Color.MistyRose, Color.NavajoWhite
                , Color.Orange, Color.OrangeRed, Color.Red
                , Color.White, Color.Yellow };

        Texture2D NoiseMap;
        Texture2D ColorMap;

        Vector3 RotationAxis;
        float RotationTime;

        VertexPositionColor[] Glow;
        int[] GlowIndex;
        VertexDeclaration GlowDeclaration;

        public SolarSystem(Vector3 Position)
            : base()
        {
            Body.CreateUVSphere(64, 64, out ModelVertices, out ModelIndices);
            effect = Manager.TexturedEffect;

            NoiseMap = Manager.WrappedNoiseTextures[MyGame.random.Next(Manager.WrappedNoiseTextures.Length)];
            Color[] StaticNoise = Manager.GenerateStaticNoise(5, 5);
            for (int i = 0; i < StaticNoise.Length; i++)
            {
                StaticNoise[i] = Color.Lerp(starColors[MyGame.random.Next(starColors.Length)]
                    , StaticNoise[i]
                    , 0.1f * (float)MyGame.random.NextDouble());
            }
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice, 5, 5);
            ColorMap.SetData<Color>(StaticNoise);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, 1.0f);
            this.Transforms = new ScalePositionRotation(
                (float)(0.04 + 0.06 * MyGame.random.NextDouble())
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = 100.0f * (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));

            //Initialize the glow
            int glowSpikes = MyGame.random.Next(25, 50);
            Glow = new VertexPositionColor[1 + glowSpikes * 2];
            GlowIndex = new int[glowSpikes * 9];
            Glow[0] = new VertexPositionColor(Vector3.Zero, new Color(Color.White, 0.0f));
            for (int i = 0; i < glowSpikes; i++)
            {
                Glow[1 + i] = new VertexPositionColor(
                    new Vector3((float)Math.Sin(i * MathHelper.TwoPi / glowSpikes)
                        , (float)Math.Cos(i * MathHelper.TwoPi / glowSpikes)
                        , (float)MyGame.random.NextDouble() * MathHelper.TwoPi)
                    , new Color(StaticNoise[MyGame.random.Next(StaticNoise.Length)]
                        , 0.1f + 0.15f * (float)MyGame.random.NextDouble()));
                GlowIndex[i * 9] = 0;
                GlowIndex[i * 9 + 1] = 1 + i;
                GlowIndex[i * 9 + 2] = 2 + i;
                Glow[1 + glowSpikes + i] = new VertexPositionColor(
                    Glow[1 + i].Position * (2.0f + 3.0f * (float)MyGame.random.NextDouble())
                    , new Color(Glow[1 + i].Color, 0.0f));
                Glow[1 + glowSpikes + i].Position.Z = Glow[1 + i].Position.Z;
                GlowIndex[i * 9 + 3] = 1 + i;
                GlowIndex[i * 9 + 4] = 1 + glowSpikes + i;
                GlowIndex[i * 9 + 5] = 2 + glowSpikes + i;
                GlowIndex[i * 9 + 6] = 1 + i;
                GlowIndex[i * 9 + 7] = 2 + glowSpikes + i;
                GlowIndex[i * 9 + 8] = 2 + i;
            }
            GlowIndex[(glowSpikes - 1) * 9 + 2] = 1;
            GlowIndex[(glowSpikes - 1) * 9 + 5] = 1 + glowSpikes;
            GlowIndex[(glowSpikes - 1) * 9 + 7] = 1 + glowSpikes;
            GlowIndex[(glowSpikes - 1) * 9 + 8] = 1;
            GlowDeclaration = new VertexDeclaration(MyGame.graphics.GraphicsDevice, VertexPositionColor.VertexElements);
        }

        public override void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            RotationTime += 0.05f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Transforms = new ScalePositionRotation(
                Transforms.Scale
                , Transforms.Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));

            base.Update(gameTime, Position + Transforms.Position, ParentMass);
        }

        protected override void InitializeNestedBodies()
        {
            if (NestedBodies.Count == 0)
            {
                //Add the other parts of the SolarSystem
                Vector3 OrbitalPlaneNormal = Manager.GetRandomNormal();
                int NumPlanets = MyGame.random.Next(7, 10);
                for (int i = 0; i < NumPlanets; i++)
                {
                    NestedBodies.Add(new Planet(Manager.GetRandomNormal() * (float)(Transforms.Scale + 0.9 * MyGame.random.NextDouble())
                        , Transforms.Scale * 0.1f * (float)MyGame.random.NextDouble()
                        , Mass
                        , OrbitalPlaneNormal));
                }
                int NumAsteroids = MyGame.random.Next(1, 4);
                for (int i = 0; i < NumAsteroids; i++)
                {
                    NestedBodies.AddRange(
                        Asteroid.CreateBelt(
                            Transforms.Scale + 0.9f * (float)MyGame.random.NextDouble()
                            , Transforms.Scale * (0.5f + 0.5f * (float)MyGame.random.NextDouble())
                            , Transforms.Scale * (0.1f + 0.1f * (float)MyGame.random.NextDouble())
                            , Manager.GetRandomNormal()
                            , MyGame.random.Next(1, 4)
                            , Mass));
                }
            }
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            //Draw the Glow
            Manager.ResetFor2D();
            MyGame.graphics.GraphicsDevice.VertexDeclaration = GlowDeclaration;
            Manager.OrdinaryEffect.CurrentTechnique = Manager.OrdinaryEffect.Techniques["Glow"];
            Manager.OrdinaryEffect.Parameters["CameraPosition"].SetValue(Manager.CameraFocus + Manager.CameraLocation);
            Manager.OrdinaryEffect.Parameters["CameraUpVector"].SetValue(Manager.CameraUp);
            Manager.OrdinaryEffect.Parameters["PointSpriteSize"].SetValue(Transforms.Scale * 2.0f);
            Manager.OrdinaryEffect.Parameters["PositionValue"].SetValue(Transforms.Position);
            Manager.OrdinaryEffect.Parameters["Offset"].SetValue(RotationTime / 40.0f);
            Manager.OrdinaryEffect.Begin();
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
            MyGame.graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                PrimitiveType.TriangleList
                , Glow
                , 0
                , Glow.Length
                , GlowIndex
                , 0
                , GlowIndex.Length / 3);
            Manager.OrdinaryEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            Manager.OrdinaryEffect.End();
            //Draw the Star, Planets, Asteroids, Etc.
            Manager.ResetFor3D();
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["Textured"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(NoiseMap);
            Manager.TexturedEffect.Parameters["InputTextureInterpolation"].SetValue(0.0f);
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.0f);
            base.Draw(VisibleArea, Position);
        }
    }
}
