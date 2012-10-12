using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AstronomicalSimulator.Drawing
{
    class Asteroid : Body
    {
        public static List<Body> CreateBelt(float Radius, float RadiusModulus
            , float MaximumScale
            , Vector3 OrbitalPlaneNormal
            , int Quantity
            , float ParentMass)
        {
            List<Body> asteroids = new List<Body>();
            for (int i = 0; i < Quantity; i++)
            {
                asteroids.Add(new Asteroid(
                    Vector3.Normalize(
                        Vector3.Cross(OrbitalPlaneNormal
                            , Manager.GetRandomNormal()))
                        * Radius
                        + Manager.GetRandomNormal()
                            * RadiusModulus * 4
                            * (float)MyGame.random.NextDouble()
                    , MaximumScale * (0.1f + 0.9f * (float)MyGame.random.NextDouble())
                    , OrbitalPlaneNormal
                    , ParentMass));
            }
            return asteroids;
        }

        public struct VertexPositionNormalColor
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Color VertexColor;
            public VertexPositionNormalColor(Vector3 Position, Vector3 Normal, Color Color)
            {
                this.Position = Position;
                this.Normal = Normal;
                this.VertexColor = Color;
            }

            public static int SizeInBytes { get { return 10 * sizeof(float); } }
            public static readonly VertexElement[] VertexElements = 
            {
                new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0)
                , new VertexElement(0, 3 * sizeof(float), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0)
                , new VertexElement(0, 6 * sizeof(float), VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0)
            };
        }

        class Triangle
        {
            public Edge[] Edges = new Edge[3];
            public Vertex[] Vertices = new Vertex[3];
            public Vector3 Normal;
            /// <summary>
            /// Assumes that the three Edges makes a Triangle
            /// Otherwise, wierd stuff will happen when converting to a model
            /// </summary>
            public Triangle(Edge Edge1, Edge Edge2, Edge Edge3)
            {
                Edges[0] = Edge1;
                Vertices[0] = Edge1.Vertices[0];
                Vertices[1] = Edge1.Vertices[1];
                Edges[1] = Edge2;
                if (Edge2.Vertices[0].Index == Vertices[0].Index
                    || Edge2.Vertices[0].Index == Vertices[1].Index)
                {
                    Vertices[2] = Edge2.Vertices[1];
                }
                else if (Edge2.Vertices[1].Index == Vertices[0].Index
                    || Edge2.Vertices[1].Index == Vertices[1].Index)
                {
                    Vertices[2] = Edge2.Vertices[0];
                }
                Edges[2] = Edge3;
                Normal = -Vector3.Cross(Vertices[0].Position - Vertices[1].Position
                    , Vertices[0].Position - Vertices[2].Position);
                Normal.Normalize();

                Vector3 Center = (Vertices[0].Position + Vertices[1].Position + Vertices[2].Position) / 3.0f;
                bool ReverseOrder = false;
                if (Center.X > 0)
                {
                    if (Normal.X < 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.X = (float)Math.Abs(Normal.X);
                }
                else
                {
                    if (Normal.X > 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.X = -(float)Math.Abs(Normal.X);
                }
                if (Center.Y > 0)
                {
                    if (Normal.Y < 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.Y = (float)Math.Abs(Normal.Y);
                }
                else
                {
                    if (Normal.Y > 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.Y = -(float)Math.Abs(Normal.Y);
                }
                if (Center.Z > 0)
                {
                    if (Normal.Z < 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.Z = (float)Math.Abs(Normal.Z);
                }
                else
                {
                    if (Normal.Z > 0)
                    {
                        ReverseOrder = true;
                    }
                    Normal.Z = -(float)Math.Abs(Normal.Z);
                }

                if (ReverseOrder)
                {
                    Vertex temp = Vertices[2];
                    Vertices[2] = Vertices[1];
                    Vertices[1] = temp;
                }
            }
        }
        class Edge
        {
            public Vertex[] Vertices = new Vertex[2];
            public Vertex Divisor = null;
            public Edge[] TwoEdges = new Edge[2];
            public Edge(Vertex Vertex1, Vertex Vertex2)
            {
                Vertices[0] = Vertex1;
                Vertices[1] = Vertex2;
            }
        }
        class Vertex
        {
            public Vector3 Position;
            public int Index;
            public Vertex(Vector3 Position, int Index)
            {
                this.Position = Position;
                this.Index = Index;
            }
        }
        List<Vertex> Vertices = new List<Vertex>();
        List<Edge> Edges = new List<Edge>();
        List<Triangle> Triangles = new List<Triangle>();
        void IncreaseVertexCount()
        {
            List<Edge> NewEdges = new List<Edge>();
            List<Triangle> NewTriangles = new List<Triangle>();
            for (int i = 0; i < Triangles.Count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    //Create the new edges
                    if (Triangles[i].Edges[j].Divisor == null)
                    {
                        Triangles[i].Edges[j].Divisor =
                            new Vertex((float)(0.35 + 0.3 * MyGame.random.NextDouble())
                                    * (Triangles[i].Edges[j].Vertices[0].Position + Triangles[i].Edges[j].Vertices[1].Position)
                                , Vertices.Count);
                        Vertices.Add(Triangles[i].Edges[j].Divisor);
                        Triangles[i].Edges[j].TwoEdges[0] = new Edge(Triangles[i].Edges[j].Vertices[0], Triangles[i].Edges[j].Divisor);
                        Triangles[i].Edges[j].TwoEdges[1] = new Edge(Triangles[i].Edges[j].Vertices[1], Triangles[i].Edges[j].Divisor);
                        NewEdges.Add(Triangles[i].Edges[j].TwoEdges[0]);
                        NewEdges.Add(Triangles[i].Edges[j].TwoEdges[1]);
                    }
                }
                Edge Edge01 = new Edge(Triangles[i].Edges[0].Divisor, Triangles[i].Edges[1].Divisor);
                Edge Edge12 = new Edge(Triangles[i].Edges[1].Divisor, Triangles[i].Edges[2].Divisor);
                Edge Edge20 = new Edge(Triangles[i].Edges[2].Divisor, Triangles[i].Edges[0].Divisor);
                NewEdges.Add(Edge01);
                NewEdges.Add(Edge12);
                NewEdges.Add(Edge20);
                //Create the new triangles
                NewTriangles.Add(new Triangle(Edge01, Edge12, Edge20));
                if (Triangles[i].Edges[0].Vertices[0].Index == Triangles[i].Edges[1].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[0].TwoEdges[0]
                        , Triangles[i].Edges[1].TwoEdges[0]
                        , Edge01));
                }
                else if (Triangles[i].Edges[0].Vertices[0].Index == Triangles[i].Edges[1].Vertices[1].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[0].TwoEdges[0]
                        , Triangles[i].Edges[1].TwoEdges[1]
                        , Edge01));
                }
                else if (Triangles[i].Edges[0].Vertices[1].Index == Triangles[i].Edges[1].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[0].TwoEdges[1]
                        , Triangles[i].Edges[1].TwoEdges[0]
                        , Edge01));
                }
                else
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[0].TwoEdges[1]
                        , Triangles[i].Edges[1].TwoEdges[1]
                        , Edge01));
                }
                if (Triangles[i].Edges[1].Vertices[0].Index == Triangles[i].Edges[2].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[1].TwoEdges[0]
                        , Triangles[i].Edges[2].TwoEdges[0]
                        , Edge12));
                }
                else if (Triangles[i].Edges[1].Vertices[0].Index == Triangles[i].Edges[2].Vertices[1].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[1].TwoEdges[0]
                        , Triangles[i].Edges[2].TwoEdges[1]
                        , Edge12));
                }
                else if (Triangles[i].Edges[1].Vertices[1].Index == Triangles[i].Edges[2].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[1].TwoEdges[1]
                        , Triangles[i].Edges[2].TwoEdges[0]
                        , Edge12));
                }
                else
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[1].TwoEdges[1]
                        , Triangles[i].Edges[2].TwoEdges[1]
                        , Edge12));
                }
                if (Triangles[i].Edges[2].Vertices[0].Index == Triangles[i].Edges[0].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[2].TwoEdges[0]
                        , Triangles[i].Edges[0].TwoEdges[0]
                        , Edge20));
                }
                else if (Triangles[i].Edges[2].Vertices[0].Index == Triangles[i].Edges[0].Vertices[1].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[2].TwoEdges[0]
                        , Triangles[i].Edges[0].TwoEdges[1]
                        , Edge20));
                }
                else if (Triangles[i].Edges[2].Vertices[1].Index == Triangles[i].Edges[0].Vertices[0].Index)
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[2].TwoEdges[1]
                        , Triangles[i].Edges[0].TwoEdges[0]
                        , Edge20));
                }
                else
                {
                    NewTriangles.Add(new Triangle(
                        Triangles[i].Edges[2].TwoEdges[1]
                        , Triangles[i].Edges[0].TwoEdges[1]
                        , Edge20));
                }
            }
            //Replace the old Edges and Triangles
            Edges = NewEdges;
            Triangles = NewTriangles;
        }
        VertexDeclaration ModelDeclaration;
        VertexPositionNormalColor[] AsteroidVertices;
        int[] AsteroidIndices;
        void ConvertPartsToPrimitive(Color DefaultColor)
        {
            //Convert Vertex to VertexPositionNormalColor, with no Normal
            AsteroidVertices = new VertexPositionNormalColor[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                AsteroidVertices[i] = new VertexPositionNormalColor(Vertices[i].Position, Vector3.Zero, DefaultColor);
            }
            //Convert Triangles into Indices
            AsteroidIndices = new int[Triangles.Count * 3];
            for (int i = 0; i < Triangles.Count; i++)
            {
                AsteroidIndices[i * 3] = Triangles[i].Vertices[0].Index;
                AsteroidIndices[i * 3 + 1] = Triangles[i].Vertices[1].Index;
                AsteroidIndices[i * 3 + 2] = Triangles[i].Vertices[2].Index;
            }
            //Determine Normals by summing the Normals of all Triangles
            for (int i = 0; i < Triangles.Count; i++)
            {
                AsteroidVertices[Triangles[i].Vertices[0].Index].Normal += Triangles[i].Normal;
                AsteroidVertices[Triangles[i].Vertices[1].Index].Normal += Triangles[i].Normal;
                AsteroidVertices[Triangles[i].Vertices[2].Index].Normal += Triangles[i].Normal;
            }
            //Then normalize the Normals again
            for (int i = 0; i < AsteroidVertices.Length; i++)
            {
                AsteroidVertices[i].Normal.Normalize();
            }
        }

        Vector3 Velocity;
        Vector3 RotationAxis;
        float RotationTime;

        private Asteroid(Vector3 Position, float Scale, Vector3 OrbitalPlaneNormal, float ParentMass)
            : base()
        {
            AsteroidVertices = null;
            
            //Create a unique asteroid Model
            //First establish the 6 Vertices of the tetrahedron
            Vertices.Add(new Vertex(new Vector3(0, 0.5f + (float)MyGame.random.NextDouble(), 0), 0));
            Vertices.Add(new Vertex(new Vector3(0.5f + (float)MyGame.random.NextDouble(), 0, 0), 1));
            Vertices.Add(new Vertex(new Vector3(0, 0, 0.5f + (float)MyGame.random.NextDouble()), 2));
            Vertices.Add(new Vertex(new Vector3(-0.5f - (float)MyGame.random.NextDouble(), 0, 0), 3));
            Vertices.Add(new Vertex(new Vector3(0, 0, -0.5f - (float)MyGame.random.NextDouble()), 4));
            Vertices.Add(new Vertex(new Vector3(0, -0.5f -(float)MyGame.random.NextDouble(), 0), 5));
            //Next establish the 12 Edges of the tetrahedron
            Edges.Add(new Edge(Vertices[0], Vertices[1]));
            Edges.Add(new Edge(Vertices[0], Vertices[2]));
            Edges.Add(new Edge(Vertices[0], Vertices[3]));
            Edges.Add(new Edge(Vertices[0], Vertices[4]));
            Edges.Add(new Edge(Vertices[1], Vertices[2]));
            Edges.Add(new Edge(Vertices[2], Vertices[3]));
            Edges.Add(new Edge(Vertices[3], Vertices[4]));
            Edges.Add(new Edge(Vertices[4], Vertices[1]));
            Edges.Add(new Edge(Vertices[1], Vertices[5]));
            Edges.Add(new Edge(Vertices[2], Vertices[5]));
            Edges.Add(new Edge(Vertices[3], Vertices[5]));
            Edges.Add(new Edge(Vertices[4], Vertices[5]));
            //Next establish the 8 Faces/Triangles of the tetrahedron
            Triangles.Add(new Triangle(Edges[0], Edges[1], Edges[4]));
            Triangles.Add(new Triangle(Edges[1], Edges[2], Edges[5]));
            Triangles.Add(new Triangle(Edges[2], Edges[3], Edges[6]));
            Triangles.Add(new Triangle(Edges[3], Edges[0], Edges[7]));
            Triangles.Add(new Triangle(Edges[8], Edges[9], Edges[4]));
            Triangles.Add(new Triangle(Edges[9], Edges[10], Edges[5]));
            Triangles.Add(new Triangle(Edges[10], Edges[11], Edges[6]));
            Triangles.Add(new Triangle(Edges[11], Edges[8], Edges[7]));

            int DetailLevel = MyGame.random.Next(4, 5);
            for (int i = 0; i < DetailLevel; i++)
            {
                IncreaseVertexCount();
            }

            ConvertPartsToPrimitive(Color.Brown);
            ModelDeclaration = new VertexDeclaration(MyGame.graphics.GraphicsDevice, VertexPositionNormalColor.VertexElements);

            RotationAxis = Manager.GetRandomNormal();
            RotationTime = (float)MyGame.random.NextDouble();
            Bounds = new BoundingSphere(Position, Scale);
            this.Transforms = new ScalePositionRotation(
                Scale
                , Position
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));
            Mass = (float)(4.0 / 3.0 * Math.PI * Math.Pow(Transforms.Scale, 3.0));

            this.Velocity = Body.GetRandomInitialOrbitVelocity(Position, OrbitalPlaneNormal, ParentMass, Mass);
        }
        public override void Update(GameTime gameTime, Vector3 Position, float ParentMass)
        {
            //Orbit around the star
            Velocity -= Vector3.Normalize(Transforms.Position)
                * Body.GetGravitationalForce(Transforms.Position, ParentMass, Mass)
                * (float)gameTime.ElapsedGameTime.TotalMinutes;

            //Rotate along a random axis
            RotationTime += 0.25f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Transforms = new ScalePositionRotation(
                Transforms.Scale
                , Transforms.Position + Velocity * (float)gameTime.ElapsedGameTime.TotalMinutes
                , Matrix.CreateFromAxisAngle(RotationAxis, RotationTime));

            base.Update(gameTime, Position + Transforms.Position, ParentMass);
        }

        public override void Draw(BoundingFrustum VisibleArea, Vector3 Position)
        {
            Manager.ResetFor3D();
            Manager.DiffuseEffect.CurrentTechnique = Manager.DiffuseEffect.Techniques["DiffuseFast"];
            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            MyGame.graphics.GraphicsDevice.VertexDeclaration = ModelDeclaration;
            //Set the direction of the light to be away from Vector3.Zero
            Manager.DiffuseEffect.Parameters["AmbientIntensity"].SetValue(0.25f);
            Manager.DiffuseEffect.Parameters["LightDirection"].SetValue(
                Vector3.Transform(
                    Vector3.Normalize(-Transforms.Position)
                    , Matrix.Invert(Transforms.Rotation)));
            Matrix transform = Transforms.Rotation
                    * Matrix.CreateScale(Transforms.Scale)
                    * Matrix.CreateTranslation(Transforms.Position + Position);
            Manager.DiffuseEffect.Parameters["World"].SetValue(transform);
            Manager.DiffuseEffect.Begin();
                Manager.DiffuseEffect.CurrentTechnique.Passes.First<EffectPass>().Begin();
                    MyGame.graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalColor>(
                        PrimitiveType.TriangleList
                        , AsteroidVertices
                        , 0
                        , AsteroidVertices.Length
                        , AsteroidIndices
                        , 0
                        , AsteroidIndices.Length / 3);
                Manager.DiffuseEffect.CurrentTechnique.Passes.First<EffectPass>().End();
            Manager.DiffuseEffect.End();
            base.Draw(VisibleArea, Position);
            MyGame.graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        }
    }
}
