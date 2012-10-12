using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Planet : Body
    {
        static Color[] planetColors =
            new Color[] { Color.Aquamarine, Color.Azure, Color.Blue
                , Color.Brown, Color.MediumBlue, Color.CornflowerBlue
                , Color.DarkGreen, Color.DarkSlateBlue, Color.DodgerBlue
                , Color.ForestGreen, Color.GhostWhite, Color.Green
                , Color.GreenYellow, Color.MediumSlateBlue, Color.LawnGreen
                , Color.LightGreen, Color.LightSeaGreen, Color.LightSkyBlue
                , Color.MidnightBlue, Color.LimeGreen, Color.MediumSeaGreen
                , Color.MediumSpringGreen, Color.Navy, Color.Olive
                , Color.BurlyWood, Color.PowderBlue, Color.RoyalBlue
                , Color.SeaGreen, Color.SkyBlue, Color.ForestGreen
                , Color.SteelBlue, Color.SpringGreen };

        Texture2D NoiseMap;
        Texture2D ColorMap;

        Vector3 RotationAxis;
        float RotationTime;

        Vector3 Velocity;

        public Planet(Vector3 Position, float Scale, float ParentMass, Vector3 OrbitalPlaneNormal)
            : base()
        {
            Body.CreateUVSphere(32, 32, out ModelVertices, out ModelIndices);
            effect = Manager.TexturedEffect;

            NoiseMap = Manager.WrappedNoiseTextures[MyGame.random.Next(Manager.WrappedNoiseTextures.Length)];
            Color[] StaticNoise = Manager.GenerateStaticNoise(5, 5);
            for (int i = 0; i < StaticNoise.Length; i++)
            {
                StaticNoise[i] = Color.Lerp(planetColors[MyGame.random.Next(planetColors.Length)]
                    , StaticNoise[i]
                    , 0.1f * (float)MyGame.random.NextDouble());
            }
            ColorMap = new Texture2D(MyGame.graphics.GraphicsDevice, 5, 5);
            ColorMap.SetData<Color>(StaticNoise);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, 2.0f * Scale);
            this.Transforms = new ScalePositionRotation(
                Scale
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = 10.0f * (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));

            this.Velocity = Body.GetRandomInitialOrbitVelocity(Position, OrbitalPlaneNormal, ParentMass, Mass);
        }

        public override void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            //Orbit around the star
            Velocity -= Vector3.Normalize(Transforms.Position)
                * Body.GetGravitationalForce(Transforms.Position, ParentMass, Mass)
                * (float)gameTime.ElapsedGameTime.TotalMinutes;

            //Rotate along a random axis
            RotationTime += 0.1f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Transforms = new ScalePositionRotation(
                Transforms.Scale
                , Transforms.Position + Velocity * (float)gameTime.ElapsedGameTime.TotalMinutes
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));

            base.Update(gameTime, Position + Transforms.Position, ParentMass);
        }

        protected override void InitializeNestedBodies()
        {
            if (NestedBodies.Count == 0)
            {
                //Add some Moons
                int NumMoons = MyGame.random.Next(2, 5);
                for (int i = 0; i < NumMoons; i++)
                {
                    NestedBodies.Add(new Moon(Manager.GetRandomNormal() * (float)(Transforms.Scale + Transforms.Scale * MyGame.random.NextDouble())
                        , (float)(Transforms.Scale * 0.1 * MyGame.random.NextDouble())
                        , Mass));
                }
            }
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            Manager.ResetFor3D();
            Manager.TexturedEffect.CurrentTechnique = Manager.TexturedEffect.Techniques["TextureDiffuse"];
            Manager.TexturedEffect.Parameters["InputTexture"].SetValue(NoiseMap);
            Manager.TexturedEffect.Parameters["ColorMapTexture"].SetValue(ColorMap);
            Manager.TexturedEffect.Parameters["TextureAlphaThreshold"].SetValue(0.0f);
            //Set the direction of the light to be away from Vector3.Zero
            Matrix InverseRotation = Matrix.Invert(Transforms.Rotation);
            Manager.TexturedEffect.Parameters["LightDirection"].SetValue(
                Vector3.Transform(
                    Vector3.Normalize(-Transforms.Position)
                    , InverseRotation));
            Manager.TexturedEffect.Parameters["AmbientIntensity"].SetValue(0.15f);
            Manager.TexturedEffect.Parameters["ViewDirection"].SetValue(
                Vector3.Transform(
                    Vector3.Normalize(Manager.CameraFocus + Manager.CameraLocation)
                    , InverseRotation));
            Manager.TexturedEffect.Parameters["SpecularIntensity"].SetValue(1f);
            base.Draw(VisibleArea, Position);
        }
    }
}
