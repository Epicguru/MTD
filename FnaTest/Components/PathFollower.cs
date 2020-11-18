using System;
using Microsoft.Xna.Framework;
using MTD.World;
using MTD.World.Pathfinding;
using Nez;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Nez.Sprites;

namespace MTD.Components
{
    public class PathFollower : Component, IUpdatable
    {
        const float TELEPORT_DISTANCE = 2 * Tile.SIZE;

        /// <summary>
        /// The movement speed, in tiles per second.
        /// </summary>
        public float MovementSpeed = 5;

        /// <summary>
        /// If true, this entity's X scale will be changed to make the
        /// entity visually face the direction of travel.
        /// This is true by default.
        /// </summary>
        public bool FaceMovementDirection = true;

        public Point CurrentTilePos { get; private set; }
        public Point NextTilePos { get; private set; }
        public float LerpToNextTile{ get; private set; }
        public bool IsCalculatingPath { get; private set; }
        public bool IsWalkingPath { get; private set; }
        public bool IsMovingRight { get; private set; }

        private List<Point> currentPath;

        public void Update()
        {
            if (Input.RightMouseButtonPressed)
            {
                var targetPos = Map.Current.WorldToTileCoordinates(Input.WorldMousePos);
                StartPath(targetPos);
            }

            if (Input.IsKeyDown(Keys.M))
            {
                if (!IsWalkingPath)
                    TryStartWander(20);
            }

            var currentPos = Entity.Position;
            var currentTileWorldPos = Map.Current.TileToWorldPosition(CurrentTilePos);
            var nextTileWorldPos = Map.Current.TileToWorldPosition(NextTilePos);

            float sqrDstToCurrent = (currentPos - currentTileWorldPos).LengthSquared();
            if (sqrDstToCurrent > TELEPORT_DISTANCE * TELEPORT_DISTANCE)
            {
                // Teleport! Instantly move the current and next position to wherever the entity is located, visually.
                var newPos = Map.Current.WorldToTileCoordinates(currentPos);
                CurrentTilePos = newPos;
                NextTilePos = newPos;
                LerpToNextTile = 0f;
                ResetPath();
                //Debug.Warn("PathFollower teleported.");
                return;
            }

            if (CurrentTilePos != NextTilePos)
            {
                // Interpolate...
                bool isDiagonal = CurrentTilePos.X != NextTilePos.X && CurrentTilePos.Y != NextTilePos.Y;
                const float SQRT_2 = 1.4142135f;
                const float REDUCTION = 1f / SQRT_2;
                LerpToNextTile += Time.DeltaTime * MovementSpeed * (isDiagonal ? REDUCTION : 1f);
                bool changedNext = false;
                while (CurrentTilePos != NextTilePos && LerpToNextTile >= 1f)
                {
                    LerpToNextTile -= 1f;
                    CurrentTilePos = NextTilePos;
                    NextTilePos = GetNewNextTilePos();
                    changedNext = true;
                }

                if (changedNext)
                {
                    currentTileWorldPos = Map.Current.TileToWorldPosition(CurrentTilePos);
                    nextTileWorldPos = Map.Current.TileToWorldPosition(CurrentTilePos);
                }
                var visualPos = Vector2.Lerp(currentTileWorldPos, nextTileWorldPos, LerpToNextTile);
                if (isDiagonal)
                    visualPos.Y -= Mathf.Sin(MathF.PI * LerpToNextTile) * Tile.SIZE * 0.5f;
                Entity.Position = visualPos;
            }
            else
            {
                Entity.Position = currentTileWorldPos;
            }

            // Face entity towards direction of movement.
            if (IsWalkingPath)
            {
                if (NextTilePos.X > CurrentTilePos.X)
                    IsMovingRight = true;
                else if (NextTilePos.X < CurrentTilePos.X)
                    IsMovingRight = false;

                if (FaceMovementDirection && !Entity.IsNullOrDestroyed())
                {
                    var spr = Entity.GetComponentInChildren<SpriteRenderer>();
                    if(spr != null)
                        spr.FlipX = !IsMovingRight;
                }
            }
        }

        /// <summary>
        /// Clears the current path.
        /// </summary>
        public void ResetPath()
        {
            NextTilePos = CurrentTilePos;
            LerpToNextTile = 0f;
            if (currentPath != null)
            {
                Pathfinder.FreePath(currentPath);
                currentPath = null;
            }
            IsWalkingPath = false;
        }

        public void StartPath(Point targetPosition)
        {
            if (targetPosition == CurrentTilePos)
                return;
            if (IsCalculatingPath)
            {
                Debug.Warn("Already calculating path, cannot be cancelled.");
                return; // TODO should queue new path, then discard old path once new one arrives...
            }

            IsCalculatingPath = true;
            Main.Pathfinder.FindPath(CurrentTilePos, targetPosition, UponPathComplete);
        }

        protected virtual void UponPathComplete(PathResult result, List<Point> path, object userObj)
        {
            IsCalculatingPath = false;

            if (result != PathResult.SUCCESS)
                return;

            if (path.Count < 2)
                return; // Why?

            // Clear any previous path.
            ResetPath();

            // Start a new path.
            LerpToNextTile = 0f;
            currentPath = path;

            // Set current and next positions.
            CurrentTilePos = path[0];
            NextTilePos = path[1];
            path.RemoveAt(0);
            path.RemoveAt(0);

            // Path length was only ever 2.
            if (path.Count == 0)
            {
                // As soon as we have started walking, we also end.
                Pathfinder.FreePath(currentPath);
                currentPath = null;
                IsWalkingPath = false;
            }
            else
            {
                IsWalkingPath = true;
            }
        }

        public void TryStartWander(int radius)
        {
            if (radius <= 0)
                return;

            int rx;
            int ry;
            do
            {
                rx = Nez.Random.Range(CurrentTilePos.X - radius, CurrentTilePos.X + radius + 1);
                ry = Nez.Random.Range(CurrentTilePos.Y - radius, CurrentTilePos.Y + radius + 1);
            } while (!Map.Current.CanStand(rx, ry));

            StartPath(new Point(rx, ry));
        }

        private Point GetNewNextTilePos()
        {
            if (currentPath == null || currentPath.Count == 0)
                return CurrentTilePos;

            var node = currentPath[0];
            currentPath.RemoveAt(0);

            if (currentPath.Count == 0)
            {
                IsWalkingPath = false;
                Pathfinder.FreePath(currentPath);
                currentPath = null;
            }

            // TODO check the new tile is valid (has not changed)

            return node;
        }

        public override void DebugRender(Batcher batcher)
        {
            base.DebugRender(batcher);

            var currentTileWorldPos = Map.Current.TileToWorldPosition(CurrentTilePos);
            var nextTileWorldPos = Map.Current.TileToWorldPosition(CurrentTilePos);

            batcher.DrawCircle(currentTileWorldPos, 1f, Color.Red, 2, 16);
            batcher.DrawCircle(nextTileWorldPos, 0.6f, Color.Green, 2, 14);

            if (currentPath != null)
            {
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    var current = Map.Current.TileToWorldPosition(currentPath[i]);
                    var next = Map.Current.TileToWorldPosition(currentPath[i + 1]);

                    batcher.DrawLine(current, next, Color.OrangeRed);
                }
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            CurrentTilePos = Map.Current.WorldToTileCoordinates(Entity.Position);
            NextTilePos = CurrentTilePos;
        }
    }
}
