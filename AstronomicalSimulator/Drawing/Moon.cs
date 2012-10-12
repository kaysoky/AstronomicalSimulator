using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Moon : Body
    {
        Texture2D NoiseMap;
        Texture2D ColorMap;

        Vector3 RotationAxis;
        float RotationTime;
        Vector3 Velocity;

        public Moon(Vector3 Position, float Scale, float ParentMass)
            : base()
        {
            Body.CreateUVSphere(16, 16, out ModelVertices, out ModelIndices);
            effect = Manager.TexturedEffect;

            Color[] StaticNoise = Manager.GenerateStaticNoise(5, 5);
            NoiseMap = new Texture2D(MyGame.graphics.GraphicsDevice, 5, 5);
            NoiseMap.SetData<Color>(StaticNoise);
            NoiseMap = Manager.SphericalWrap(NoiseMap);
            StaticNoise = Manager.GenerateStaticNoise(5, 5);
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice, 5, 5);
            ColorMap.SetData<Color>(StaticNoise);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, Scale);
            this.Transforms = new ScalePositionRotation(
                Scale
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));

            //Set the initial velocity
           Velocity = Body.GetRandomInitialOrbitVelocity(Transforms.Position, null
                , ParentMass, Mass);
        }

        public override void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            //Orbit around the planet
            Velocity -= Vector3.Normalize(Transforms.Position)
                * Body.GetGravitationalForce(Transforms.Position, ParentMass, Mass)
                * (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Rotate along a random axis
            RotationTime += 0.2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Transforms = new ScalePositionRotation(
                Transforms.Scale
                , Transforms.Position + Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));

            base.Update(gameTime, Position + Transforms.Position, Mass);
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            Manager.ResetFor3D();
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["TextureDiffuse"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(NoiseMap);
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.0f);
            //Set the direction of the light to be away from the parent
            Manager.TexturedEffect.Parameters["LightDirection"].SetValue(
                Vector3.Transform(
                    Vector3.Normalize(-Transforms.Position)
                    , Matrix.Invert(Transforms.Rotation)));
            Manager.TexturedEffect.Parameters["AmbientIntensity"].SetValue(0.15f);
            Manager.TexturedEffect.Parameters["ViewDirection"].SetValue(
                Vector3.Transform(
                    Vector3.Normalize(Manager.CameraFocus + Manager.CameraLocation)
                    , Matrix.Invert(Transforms.Rotation)));
            Manager.TexturedEffect.Parameters["SpecularIntensity"].SetValue(1f);
            base.Draw(VisibleArea, Position);
        }
    }
}
