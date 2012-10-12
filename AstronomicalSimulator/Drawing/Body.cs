using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    /// <summary>
    /// The abstract super class of all objects drawn into 3D space
    /// </summary>
    abstract class Body
    {
        /// <summary>
        /// A collection of properties that is used to draw the model
        /// Bundled together so that more complex models can reallign BoundingSpheres after modifications
        /// </summary>
        public class ScalePositionRotation
        {
            float scale;
            public float Scale
            {
                get { return scale; }
            }
            Vector3 position;
            public Vector3 Position
            {
                get { return position; }
            }
            Matrix rotation;
            public Matrix Rotation
            {
                get { return rotation; }
            }
            public ScalePositionRotation(float Scale, Vector3 Position, Matrix Rotation)
            {
                this.scale = Scale;
                this.position = Position;
                this.rotation = Rotation;
            }

            public ScalePositionRotation Clone()
            {
                return new ScalePositionRotation(Scale, Position, Rotation);
            }
        }

        /// <summary>
        /// Relative the the parent Body
        /// </summary>
        public virtual ScalePositionRotation Transforms
        {
            get
            {
                return transforms;
            }
            set
            {
                transforms = value;
            }
        }
        private ScalePositionRotation transforms
                = new ScalePositionRotation(1f
                    , Vector3.Zero
                    , Matrix.Identity);
        public VertexPositionTexture[] ModelVertices;
        public static VertexDeclaration DefaultDeclaration;
        public int[] ModelIndices;
        public Effect effect;
        public bool isInactive = false;
        /// <summary>
        /// Actual spatial coordinates
        /// </summary>
        public BoundingSphere Bounds;
        public float Mass;
        public List<Body> NestedBodies = new List<Body>();

        public float DistanceToCenter = float.PositiveInfinity;
        /// <summary>
        /// Radius in pixels of the Bounds of the object if placed in the center of the view
        /// </summary>
        public float ScreenScale = 0.0f;
        public Vector2 ScreenPosition = new Vector2();
        public float DisplayOpacity = 0.0f;
        public Color DisplayColor = Manager.GetRandomColor(false);

        /// <summary>
        /// Adjusts the Bounds for any movement
        /// Updates all Nested Bodies
        /// </summary>
        public virtual void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            Bounds.Center = Position;
            for (int i = 0; i < NestedBodies.Count; i++)
            {
                NestedBodies[i].Update(gameTime, Position, Mass);
            }
        }

        protected virtual void InitializeNestedBodies() { }

        protected virtual void DisposeNextedBodies() 
        {
            NestedBodies.Clear();
        }

        public static void CreateUVSphere(int Width, int Height, out VertexPositionTexture[] UVSphere, out int[] IndexBuffer)
        {
            UVSphere = new VertexPositionTexture[Width * Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    UVSphere[x + y * Width] = new VertexPositionTexture(
                        new Vector3((float)Math.Cos((double)x / (Width - 1.0) * 2.0 * Math.PI)
                                * (float)Math.Sin((double)y / (Height - 1.0) * Math.PI)
                            , -(float)Math.Cos((double)y / (Height - 1.0) * Math.PI)
                            , (float)(Math.Sin((double)x / (Width - 1.0) * 2.0 * Math.PI))
                                * (float)Math.Sin((double)y / (Height - 1.0) * Math.PI))
                        , new Vector2((float)x / (Width - 1.0f), (float)y / (Height - 1.0f)));
                }
            }
            int counter = 0;
            IndexBuffer = new int[6 * (Width - 1) * (Height - 1)];
            for (int x = 0; x < Width - 1; x++)
            {
                for (int y = 0; y < Height - 1; y++)
                {
                    IndexBuffer[counter++] = x + y * Width;
                    IndexBuffer[counter++] = x + 1 + y * Width;
                    IndexBuffer[counter++] = x + 1 + (y + 1) * Width;
                    IndexBuffer[counter++] = x + y * Width;
                    IndexBuffer[counter++] = x + 1 + (y + 1) * Width;
                    IndexBuffer[counter++] = x + (y + 1) * Width;
                }
            }
        }

        /// <summary>
        /// Randomly calculates a few Bodies' distances to the Focus
        /// Then sorts the resulting list
        /// Then calculates the screen size of the closest items
        /// In the Process, a list of active Bodies and their containers is made
        /// </summary>
        /// <param name="Focus">Unprojection of the middle of the screen into a ray</param>
        /// <param name="Bodies">Will be rearranged</param>
        /// <param name="VisibleBodies">Should be cleared before input from Manager</param>
        /// <param name="ContainingBodies">Should be cleared before input from Manager</param>
        public static void PartiallySortBodies(Ray Focus, BoundingFrustum VisibleArea, ref List<Body> Bodies
            , ref List<Body> VisibleBodies, ref List<Body> ContainingBodies)
        {
            List<Body> CalculatorBodies = new List<Body>();
            int calculators = (int)Math.Sqrt(Bodies.Count);
            for (int i = 0; i < calculators && Bodies.Count > 0; i++)
            {
                int index = MyGame.random.Next(Bodies.Count);
                CalculatorBodies.Add(Bodies[index]);
                Bodies.RemoveAt(index);
                Vector3 FocusToBody = CalculatorBodies[i].Transforms.Position - Focus.Position;
                CalculatorBodies[i].DistanceToCenter = (FocusToBody
                    - Focus.Direction * Vector3.Dot(FocusToBody, Focus.Direction)).Length();
            }
            Bodies.AddRange(CalculatorBodies);
            CalculatorBodies.Clear();
            //Merge Sort with two lists
            List<List<Body>> MergeBodies = new List<List<Body>>();
            for (int i = 0; i < Bodies.Count; i++)
            {
                MergeBodies.Add(new List<Body>());
                MergeBodies[i].Add(Bodies[i]);
            }
            while (MergeBodies.Count > 1)
            {
                for (int i = 0; i < MergeBodies.Count - 1; i++)
                {
                    for (int a = 0; a < MergeBodies[i].Count; a++)
                    {
                        for (int b = 0; b < MergeBodies[i + 1].Count; b++)
                        {
                            if (MergeBodies[i + 1][b].DistanceToCenter < MergeBodies[i][a].DistanceToCenter)
                            {
                                MergeBodies[i].Insert(a++, MergeBodies[i + 1][b]);
                                MergeBodies[i + 1].RemoveAt(b--);
                            }
                        }
                    }
                    MergeBodies[i].AddRange(MergeBodies[i + 1]);
                    MergeBodies.RemoveAt(i + 1);
                }
            }
            Bodies = MergeBodies[0];
            //Screen size calculation
            for (int i = 0; i < Bodies.Count; i++)
            {
                Bodies[i].ScreenPosition = Cursor.Project(VisibleArea
                    , Bodies[i].Bounds.Center);
                Bodies[i].ScreenScale =
                    (float)Math.Atan2(
                        Bodies[i].Bounds.Radius
                        , (Bodies[i].Bounds.Center - Manager.CameraFocus - Manager.CameraLocation).Length())
                    * Manager.GameWindow.Width / 2.0f;
                if (VisibleArea.Contains(Bodies[i].Bounds) != ContainmentType.Disjoint)
                {
                    if (Bodies[i].ScreenScale / 10 > 1.0f)
                    {
                        Bodies[i].InitializeNestedBodies();
                    }
                    else
                    {
                        Bodies[i].DisposeNextedBodies();
                    }
                    if (Bodies[i].ScreenScale > Manager.GameWindow.Width / 10.0f)
                    {
                        if (Bodies[i].NestedBodies.Count > 0)
                        {
                            PartiallySortBodies(Focus, VisibleArea, ref Bodies[i].NestedBodies, ref VisibleBodies, ref ContainingBodies);
                            float DisplayOpacity = Bodies[i].ScreenScale / Manager.GameWindow.Width * 10.0f - 1.0f;
                            if (DisplayOpacity > 1.0f)
                            {
                                DisplayOpacity = 1.0f;
                            }
                            for (int j = 0; j < Bodies[i].NestedBodies.Count; j++)
                            {
                                Bodies[i].NestedBodies[j].DisplayOpacity = DisplayOpacity;
                            }
                            Bodies[i].DisplayOpacity = 1.0f - DisplayOpacity;
                            ContainingBodies.Add(Bodies[i]);
                        }
                        else
                        {
                            VisibleBodies.Add(Bodies[i]);
                        }
                    }
                    else
                    {
                        VisibleBodies.Add(Bodies[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Draws every mesh within the model using ScalePositionRotation properties
        /// Draws all Nested Bodies with an Offset of Position
        /// </summary>
        public virtual void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            if (VisibleArea.Contains(Bounds) != ContainmentType.Disjoint)
            {
                if (ModelVertices != null)
                {
                    Matrix transform = transforms.Rotation
                        * Matrix.CreateScale(transforms.Scale)
                        * Matrix.CreateTranslation(transforms.Position + Position);
                    effect.Parameters["World"].SetValue(transform);
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
                for (int i = 0; i < NestedBodies.Count; i++)
                {
                    NestedBodies[i].Draw(VisibleArea, transforms.Position + Position);
                }
            }
        }

        /// <summary>
        /// Returns the the velocity of a perfectly circular orbit
        /// </summary>
        /// <param name="relativePosition">Relative to the orbital center</param>
        /// <param name="orbitalPlaneNormal">Null for random</param>
        /// <param name="gravityWell">Scale in Kilometers (SolarSystem.ScaleInKilometers)</param>
        /// <param name="scale">Scale in Kilometers (Planet radius * PlanetDatum.RadiusModifier)</param>
        public static Vector3 GetRandomInitialOrbitVelocity(Vector3 relativePosition, Vector3? orbitalPlaneNormal
            , float ParentMass, float ChildMass)
        {
            Vector3 direction;
            if (orbitalPlaneNormal.HasValue)
            {
                direction = Vector3.Normalize(Vector3.Cross(relativePosition, orbitalPlaneNormal.Value));
            }
            else
            {
                direction = Vector3.Normalize(Vector3.Cross(relativePosition, Manager.GetRandomNormal()));
            }
            float velocity = (float)Math.Pow(GetGravitationalForce(relativePosition, ParentMass, ChildMass) * relativePosition.Length(), 0.5);
            return velocity * direction;
        }

        /// <summary>
        /// Returns the the force of gravity
        /// </summary>
        /// <param name="relativePosition">Relative to the orbital center</param>
        /// <param name="gravityWell">Scale in Kilometers (SolarSystem.ScaleInKilometers)</param>
        /// <param name="scale">Scale in Kilometers (Planet radius * PlanetDatum.RadiusModifier)</param>
        public static float GetGravitationalForce(Vector3 relativePosition, float ParentMass, float ChildMass)
        {
            return 1.0e+7f * (float)(ParentMass * ChildMass / Math.Pow(relativePosition.Length(), 2));
        }

        /// <summary>
        /// Returns the normalized 2D coordinates from an inputted 3D vector
        /// With latitude determined by the Z axis
        /// </summary>
        public static Vector2 GetUVCoords(Vector3 normal)
        {
            normal.Normalize();
            Vector2 UVCoords = new Vector2();
            UVCoords.Y = 0.5f - normal.Z / 2.0f;
            if (normal.X == 0 && normal.Y == 0)
            {
                UVCoords.X = 0.0f;
            }
            else
            {
                UVCoords.X = (float)Math.Acos(Vector2.Dot(Vector2.UnitX, Vector2.Normalize(new Vector2(normal.X, normal.Y)))) / MathHelper.TwoPi;
            }
            return UVCoords;
        }
        /// <summary>
        /// Returns the normalized 3D normal from an inputted 2D vector
        /// </summary>
        public static Vector3 GetNormalCoords(Vector2 UVCoords)
        {
            Vector3 normal = new Vector3();
            normal.Y = 0.0f;
            normal.Z = 1.0f - 2.0f * UVCoords.Y;
            normal.X = (float)Math.Sqrt(1.0 - Math.Pow(normal.Z, 2.0));
            normal = Vector3.Transform(normal, Matrix.CreateRotationZ(-UVCoords.X * MathHelper.TwoPi));
            return normal;
        }
    }
}
