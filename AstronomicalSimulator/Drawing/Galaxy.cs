using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Galaxy : Body
    {
        Texture2D GalaxyTexture;
        Texture2D ColorMap;

        Vector3 RotationAxis;
        float RotationTime;

        public Galaxy(Vector3 Position)
            : base()
        {
            Body.CreateUVSphere(32, 32, out ModelVertices, out ModelIndices);
            effect = Manager.TexturedEffect;

            GalaxyTexture = Manager.GeneratePerlinNoise(MyGame.random.Next(10, 25));
            GalaxyTexture = Manager.SpiralWarp(GalaxyTexture);
            Color[] colorScheme = Manager.GenerateStaticNoise(5, 5);
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice
                , 5
                , 5);
            ColorMap.SetData<Color>(colorScheme);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, 10000.0f);
            this.Transforms = new ScalePositionRotation(
                100.0f
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = 1000.0f * (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));
        }

        protected override void InitializeNestedBodies()
        {
            if (NestedBodies.Count == 0)
            {

            }
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            Manager.spriteBatch.Begin();
            Manager.spriteBatch.Draw(Manager.BlankWhiteTexture, -Vector2.One, Color.TransparentBlack);
            Manager.spriteBatch.End();

            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["Textured"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(GalaxyTexture);
            Manager.TexturedEffect.Parameters["InputTextureInterpolation"].SetValue(0.0f);
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.1f);
            Manager.TexturedEffect.Parameters["World"].SetValue(Matrix.Identity);
            base.Draw(VisibleArea, Position);
            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        }
    }
}
