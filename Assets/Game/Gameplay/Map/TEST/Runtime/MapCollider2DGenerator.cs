using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay.Map
{
    public readonly struct MapCollider2DGenerationSettings
    {
        public MapCollider2DGenerationSettings(
            int sourceSampleStep,
            float simplificationTolerance,
            int minimumPathPointCount)
        {
            SourceSampleStep = Mathf.Max(1, sourceSampleStep);
            SimplificationTolerance = Mathf.Max(0f, simplificationTolerance);
            MinimumPathPointCount = Mathf.Max(3, minimumPathPointCount);
        }

        public int SourceSampleStep { get; }
        public float SimplificationTolerance { get; }
        public int MinimumPathPointCount { get; }
    }

    public sealed class MapCollider2DGenerationResult
    {
        public MapCollider2DGenerationResult(
            Vector2[][] paths,
            int sourceWidth,
            int sourceHeight,
            int sampledWidth,
            int sampledHeight,
            int blockedCellCount,
            int rawEdgeCount,
            int rawPathPointCount,
            int simplifiedPathPointCount)
        {
            Paths = paths;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            SampledWidth = sampledWidth;
            SampledHeight = sampledHeight;
            BlockedCellCount = blockedCellCount;
            RawEdgeCount = rawEdgeCount;
            RawPathPointCount = rawPathPointCount;
            SimplifiedPathPointCount = simplifiedPathPointCount;
        }

        public Vector2[][] Paths { get; }
        public int SourceWidth { get; }
        public int SourceHeight { get; }
        public int SampledWidth { get; }
        public int SampledHeight { get; }
        public int BlockedCellCount { get; }
        public int RawEdgeCount { get; }
        public int RawPathPointCount { get; }
        public int SimplifiedPathPointCount { get; }
    }

    public static class MapCollider2DGenerator
    {
        public static MapCollider2DGenerationResult Generate(
            MapDefinition definition,
            MapCollider2DGenerationSettings settings)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Texture2D source = definition.TraversableMask;
            if (source == null)
            {
                throw new ArgumentException("Map definition needs a traversable mask.", nameof(definition));
            }

            if (definition.WorldPlane != MapWorldPlane.XY)
            {
                throw new InvalidOperationException("Collider2D baking only supports maps configured on the XY plane.");
            }

            Color32[] pixels = ReadPixels(source);
            int sampleStep = Mathf.Max(1, settings.SourceSampleStep);
            int sampledWidth = Mathf.CeilToInt(source.width / (float)sampleStep);
            int sampledHeight = Mathf.CeilToInt(source.height / (float)sampleStep);
            bool[] blockedCells = BuildBlockedCells(definition, pixels, source.width, source.height, sampledWidth, sampledHeight, sampleStep, out int blockedCellCount);

            List<DirectedEdge> edges = BuildBoundaryEdges(blockedCells, sampledWidth, sampledHeight);
            List<List<GridPoint>> loops = TraceLoops(edges);
            List<Vector2[]> paths = new List<Vector2[]>(loops.Count);

            int rawPathPointCount = 0;
            int simplifiedPathPointCount = 0;

            foreach (List<GridPoint> loop in loops)
            {
                rawPathPointCount += loop.Count;

                List<Vector2> worldLoop = ConvertLoopToWorld(loop, sampledWidth, sampledHeight, definition);
                List<Vector2> simplifiedLoop = SimplifyClosedPath(worldLoop, settings.SimplificationTolerance);

                if (simplifiedLoop.Count < settings.MinimumPathPointCount)
                {
                    continue;
                }

                Vector2[] closedPath = new Vector2[simplifiedLoop.Count + 1];
                for (int i = 0; i < simplifiedLoop.Count; i++)
                {
                    closedPath[i] = simplifiedLoop[i];
                }

                closedPath[closedPath.Length - 1] = simplifiedLoop[0];
                simplifiedPathPointCount += closedPath.Length;
                paths.Add(closedPath);
            }

            return new MapCollider2DGenerationResult(
                paths.ToArray(),
                source.width,
                source.height,
                sampledWidth,
                sampledHeight,
                blockedCellCount,
                edges.Count,
                rawPathPointCount,
                simplifiedPathPointCount);
        }

        private static bool[] BuildBlockedCells(
            MapDefinition definition,
            Color32[] pixels,
            int sourceWidth,
            int sourceHeight,
            int sampledWidth,
            int sampledHeight,
            int sampleStep,
            out int blockedCellCount)
        {
            bool[] blockedCells = new bool[sampledWidth * sampledHeight];
            blockedCellCount = 0;

            for (int y = 0; y < sampledHeight; y++)
            {
                int sourceY = Mathf.Min(sourceHeight - 1, y * sampleStep + sampleStep / 2);
                for (int x = 0; x < sampledWidth; x++)
                {
                    int sourceX = Mathf.Min(sourceWidth - 1, x * sampleStep + sampleStep / 2);
                    Color32 pixel = pixels[sourceY * sourceWidth + sourceX];
                    bool blocked = !IsWalkable(pixel, definition.MaskChannel, definition.WalkableThreshold);
                    blockedCells[y * sampledWidth + x] = blocked;

                    if (blocked)
                    {
                        blockedCellCount++;
                    }
                }
            }

            return blockedCells;
        }

        private static List<DirectedEdge> BuildBoundaryEdges(bool[] blockedCells, int width, int height)
        {
            List<DirectedEdge> edges = new List<DirectedEdge>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!blockedCells[y * width + x])
                    {
                        continue;
                    }

                    if (y == height - 1 || !blockedCells[(y + 1) * width + x])
                    {
                        edges.Add(new DirectedEdge(new GridPoint(x, y + 1), new GridPoint(x + 1, y + 1)));
                    }

                    if (x == width - 1 || !blockedCells[y * width + x + 1])
                    {
                        edges.Add(new DirectedEdge(new GridPoint(x + 1, y + 1), new GridPoint(x + 1, y)));
                    }

                    if (y == 0 || !blockedCells[(y - 1) * width + x])
                    {
                        edges.Add(new DirectedEdge(new GridPoint(x + 1, y), new GridPoint(x, y)));
                    }

                    if (x == 0 || !blockedCells[y * width + x - 1])
                    {
                        edges.Add(new DirectedEdge(new GridPoint(x, y), new GridPoint(x, y + 1)));
                    }
                }
            }

            return edges;
        }

        private static List<List<GridPoint>> TraceLoops(List<DirectedEdge> edges)
        {
            Dictionary<GridPoint, List<int>> edgesByStart = BuildStartLookup(edges);
            bool[] usedEdges = new bool[edges.Count];
            List<List<GridPoint>> loops = new List<List<GridPoint>>();

            for (int i = 0; i < edges.Count; i++)
            {
                if (usedEdges[i])
                {
                    continue;
                }

                GridPoint start = edges[i].Start;
                List<GridPoint> loop = new List<GridPoint> { start };
                int currentEdgeIndex = i;
                bool closed = false;

                for (int guard = 0; guard < edges.Count; guard++)
                {
                    usedEdges[currentEdgeIndex] = true;
                    DirectedEdge currentEdge = edges[currentEdgeIndex];
                    loop.Add(currentEdge.End);

                    if (currentEdge.End.Equals(start))
                    {
                        closed = true;
                        break;
                    }

                    if (!TryFindNextEdge(currentEdge, edges, edgesByStart, usedEdges, out currentEdgeIndex))
                    {
                        break;
                    }
                }

                if (closed && loop.Count > 3)
                {
                    loops.Add(loop);
                }
            }

            return loops;
        }

        private static Dictionary<GridPoint, List<int>> BuildStartLookup(List<DirectedEdge> edges)
        {
            Dictionary<GridPoint, List<int>> lookup = new Dictionary<GridPoint, List<int>>();

            for (int i = 0; i < edges.Count; i++)
            {
                GridPoint start = edges[i].Start;
                if (!lookup.TryGetValue(start, out List<int> startEdges))
                {
                    startEdges = new List<int>();
                    lookup.Add(start, startEdges);
                }

                startEdges.Add(i);
            }

            return lookup;
        }

        private static bool TryFindNextEdge(
            DirectedEdge currentEdge,
            List<DirectedEdge> edges,
            Dictionary<GridPoint, List<int>> edgesByStart,
            bool[] usedEdges,
            out int nextEdgeIndex)
        {
            nextEdgeIndex = -1;

            if (!edgesByStart.TryGetValue(currentEdge.End, out List<int> candidates))
            {
                return false;
            }

            int bestPriority = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                int candidateIndex = candidates[i];
                if (usedEdges[candidateIndex])
                {
                    continue;
                }

                int priority = GetTurnPriority(currentEdge.Direction, edges[candidateIndex].Direction);
                if (priority >= bestPriority)
                {
                    continue;
                }

                bestPriority = priority;
                nextEdgeIndex = candidateIndex;
            }

            return nextEdgeIndex >= 0;
        }

        private static int GetTurnPriority(GridPoint fromDirection, GridPoint toDirection)
        {
            int cross = fromDirection.X * toDirection.Y - fromDirection.Y * toDirection.X;
            int dot = fromDirection.X * toDirection.X + fromDirection.Y * toDirection.Y;

            if (cross < 0)
            {
                return 0;
            }

            if (dot > 0)
            {
                return 1;
            }

            if (cross > 0)
            {
                return 2;
            }

            return 3;
        }

        private static List<Vector2> ConvertLoopToWorld(
            List<GridPoint> loop,
            int sampledWidth,
            int sampledHeight,
            MapDefinition definition)
        {
            int count = loop.Count;
            if (count > 1 && loop[count - 1].Equals(loop[0]))
            {
                count--;
            }

            List<Vector2> points = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                points.Add(GridPointToWorld(loop[i], sampledWidth, sampledHeight, definition));
            }

            return points;
        }

        private static Vector2 GridPointToWorld(
            GridPoint point,
            int sampledWidth,
            int sampledHeight,
            MapDefinition definition)
        {
            float maskU = point.X / (float)sampledWidth;
            float maskV = point.Y / (float)sampledHeight;

            float worldU = definition.FlipWorldX ? 1f - maskU : maskU;
            float worldV = definition.FlipWorldY ? 1f - maskV : maskV;

            return new Vector2(
                (worldU - 0.5f) * definition.WorldSize.x,
                (worldV - 0.5f) * definition.WorldSize.y);
        }

        private static List<Vector2> SimplifyClosedPath(List<Vector2> points, float tolerance)
        {
            if (points.Count <= 3)
            {
                return points;
            }

            int startIndex = FindFarthestPointFromCentroid(points);
            List<Vector2> reordered = new List<Vector2>(points.Count + 1);

            for (int i = 0; i < points.Count; i++)
            {
                reordered.Add(points[(startIndex + i) % points.Count]);
            }

            reordered.Add(reordered[0]);
            List<Vector2> simplified = SimplifyOpenPath(reordered, tolerance);

            if (simplified.Count > 1 && Approximately(simplified[simplified.Count - 1], simplified[0]))
            {
                simplified.RemoveAt(simplified.Count - 1);
            }

            return simplified;
        }

        private static int FindFarthestPointFromCentroid(List<Vector2> points)
        {
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                centroid += points[i];
            }

            centroid /= points.Count;

            int farthestIndex = 0;
            float farthestDistance = -1f;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = (points[i] - centroid).sqrMagnitude;
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndex = i;
                }
            }

            return farthestIndex;
        }

        private static List<Vector2> SimplifyOpenPath(List<Vector2> points, float tolerance)
        {
            int count = points.Count;
            if (count <= 2)
            {
                return new List<Vector2>(points);
            }

            float toleranceSquared = tolerance * tolerance;
            bool[] keep = new bool[count];
            keep[0] = true;
            keep[count - 1] = true;

            Stack<SimplifyRange> ranges = new Stack<SimplifyRange>();
            ranges.Push(new SimplifyRange(0, count - 1));

            while (ranges.Count > 0)
            {
                SimplifyRange range = ranges.Pop();
                float farthestDistance = -1f;
                int farthestIndex = -1;

                for (int i = range.Start + 1; i < range.End; i++)
                {
                    float distance = DistanceToSegmentSquared(points[i], points[range.Start], points[range.End]);
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthestIndex = i;
                    }
                }

                if (farthestIndex < 0 || farthestDistance <= toleranceSquared)
                {
                    continue;
                }

                keep[farthestIndex] = true;
                ranges.Push(new SimplifyRange(range.Start, farthestIndex));
                ranges.Push(new SimplifyRange(farthestIndex, range.End));
            }

            List<Vector2> simplified = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                if (keep[i])
                {
                    simplified.Add(points[i]);
                }
            }

            return simplified;
        }

        private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            Vector2 projection = start + segment * t;
            return (point - projection).sqrMagnitude;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude <= 0.000001f;
        }

        private static bool IsWalkable(Color32 pixel, MapMaskChannel channel, float threshold)
        {
            float value = channel switch
            {
                MapMaskChannel.Red => pixel.r / 255f,
                MapMaskChannel.Alpha => pixel.a / 255f,
                _ => (pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f) / 255f
            };

            return value >= threshold;
        }

        private static Color32[] ReadPixels(Texture2D source)
        {
            if (source.isReadable)
            {
                return source.GetPixels32();
            }

            RenderTexture temporary = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;

            Texture2D readableCopy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            readableCopy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readableCopy.Apply();
            Color32[] pixels = readableCopy.GetPixels32();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            DestroyRuntimeObject(readableCopy);

            return pixels;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private readonly struct DirectedEdge
        {
            public DirectedEdge(GridPoint start, GridPoint end)
            {
                Start = start;
                End = end;
                Direction = new GridPoint(end.X - start.X, end.Y - start.Y);
            }

            public GridPoint Start { get; }
            public GridPoint End { get; }
            public GridPoint Direction { get; }
        }

        private readonly struct GridPoint : IEquatable<GridPoint>
        {
            public GridPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            public bool Equals(GridPoint other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is GridPoint other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Y;
                }
            }
        }

        private readonly struct SimplifyRange
        {
            public SimplifyRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }
        }
    }
}
