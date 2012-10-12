using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AstronomicalSimulator.Drawing
{
    class Cursor
    {
        Texture2D pointer;
        public Vector2 MousePoint = Vector2.Zero;
        public Ray MouseRay = new Ray();

        public Cursor()
        {
            pointer = MyGame.content.Load<Texture2D>("Textures\\Cursor");
        }

        public void Update()
        {
            //Get the mouse position
            MouseState mouse = Mouse.GetState();
            MousePoint = new Vector2((float)mouse.X, (float)mouse.Y);
            MouseRay = Unproject(
                new Vector2((float)mouse.X / Manager.GameWindow.Width
                    , (float)mouse.Y / Manager.GameWindow.Height));
            MouseRay.Position += MouseRay.Direction;
        }

        public void Draw()
        {
            //Draw the cursor
            Manager.spriteBatch.Begin();
            Manager.spriteBatch.Draw(pointer
                , MousePoint
                , null
                , Color.Aquamarine
                , 0.0f
                , Vector2.One * pointer.Width / 2
                , 0.5f
                , SpriteEffects.None
                , 1.0f);
            Manager.spriteBatch.End();
        }

        /// <summary>
        /// Unproject a screen coordinate into a ray
        /// </summary>
        public static Ray Unproject(Vector2 Point)
        {
            //Acquires the frustum of the area of the screen in view
            //Then it stores the corners of the area
            BoundingFrustum VisibleArea = new BoundingFrustum(Manager.View * Manager.Projection);
            Vector3[] corners = VisibleArea.GetCorners();
            Vector3 Position = new Vector3(Point, 0.0f);
            Ray ray = new Ray();

            //Point on the near plane of the visible area
            ray.Position =
                corners[0] * (1 - Position.X) * (1 - Position.Y)
                + corners[1] * Position.X * (1 - Position.Y)
                + corners[2] * Position.X * Position.Y
                + corners[3] * (1 - Position.X) * Position.Y;
            Position =
                corners[4] * (1 - Position.X) * (1 - Position.Y)
                + corners[5] * Position.X * (1 - Position.Y)
                + corners[6] * Position.X * Position.Y
                + corners[7] * (1 - Position.X) * Position.Y;
            //Direction between the two points
            ray.Direction = Vector3.Normalize(Position - ray.Position);

            return ray;
        }

        /// <summary>
        /// Project a point into 2D space
        /// </summary>
        public static Vector2 Project(BoundingFrustum VisibleArea, Vector3 Point)
        {
            //Acquires the frustum of the area of the screen in view.
            //Then it stores the corners of the area.
            Vector3[] corners = VisibleArea.GetCorners();
            Ray ray = new Ray(Point, Point - Manager.CameraFocus - Manager.CameraLocation);

            float? DistanceToFar = ray.Intersects(VisibleArea.Far);
            float? DistanceToNear = ray.Intersects(VisibleArea.Near);
            Vector3 ScreenCoord;
            if (DistanceToFar.HasValue)
            {
                ScreenCoord = ray.Position + ray.Direction * DistanceToFar.Value;
                ScreenCoord = new Vector3(
                    Vector3.Dot(
                        Vector3.Normalize(corners[5] - corners[4])
                        , ScreenCoord - corners[4])
                    / (corners[5] - corners[4]).Length()
                    , Vector3.Dot(
                        Vector3.Normalize(corners[7] - corners[4])
                        , ScreenCoord - corners[4])
                    / (corners[7] - corners[4]).Length()
                    , 0);
            }
            else
            {
                //Make sure this is off the screen
                return Vector2.One * (Manager.GameWindow.Width + Manager.GameWindow.Height);
            }
            return new Vector2(ScreenCoord.X * Manager.GameWindow.Width, ScreenCoord.Y * Manager.GameWindow.Height);
        }
    }
}
