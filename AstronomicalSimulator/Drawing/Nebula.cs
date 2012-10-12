using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Nebula : Body
    {
        Texture2D NoiseMap;
        Texture2D ColorMap;

        Vector3 RotationAxis;
        float RotationTime;

        public Nebula(Vector3 Position)
            : base()
        {
            Body.CreateUVSphere(32, 32, out ModelVertices, out ModelIndices);
            effect = Manager.TexturedEffect;

            NoiseMap = Manager.WrappedNoiseTextures[MyGame.random.Next(Manager.WrappedNoiseTextures.Length)];
            Color[] StaticNoise = Manager.GenerateStaticNoise(5, 5);
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice, 5, 5);
            ColorMap.SetData<Color>(StaticNoise);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, 100.0f);
            this.Transforms = new ScalePositionRotation(
                100.0f
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = 1000.0f * (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));
        }

        public override void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            RotationTime += 0.025f * (float)gameTime.ElapsedGameTime.TotalSeconds;
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
                int NumSystems = MyGame.random.Next(7, 13);
                NestedBodies.Add(new SolarSystem(Vector3.Zero));
                for (int i = 1; i < NumSystems; i++)
                {
                    NestedBodies.Add(new SolarSystem(75.0f * Manager.GetRandomNormal() * (float)MyGame.random.NextDouble()));
                }
            }
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            Manager.ResetFor2D();
            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["TexturedCloud"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(NoiseMap);
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            for (int i = 1; i < 2; i++)
            {
                Matrix transform = Transforms.Rotation
                        * Matrix.CreateScale(Transforms.Scale * 0.75f * i / 3)
                        * Matrix.CreateTranslation(Transforms.Position + Position);
                effect.Parameters["World"].SetValue(transform);
                Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.33f + 0.27f * i / 3);
                MyGame.graphics.GraphicsDevice.VertexDeclaration = DefaultDeclaration;

                effect.Begin();
                effect.CurrentTechnique.Passes.First<EffectPass>().Begin();
                MyGame.graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionTexture>(
                    PrimitiveType.TriangleList
                    , ModelVertices
                    , 0
                    , ModelVertices.Length
                    , ModelIndices
                    , 0
                    , ModelIndices.Length / 3);
                effect.CurrentTechnique.Passes.First<EffectPass>().End();
                effect.End();
            }
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.33f);
            base.Draw(VisibleArea, Position);
            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        }
    }
}
