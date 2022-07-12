using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class PlatformTilePathfinder
{
    private static readonly int FirstMiddleZoneRing = 3;
    private static readonly int LastMiddleZoneRing = 8;
    private static readonly Vector3 CitadelPosition = Vector3.zero;
    private static readonly float tileAngleCorrection = 22.5f / 2f;

    private static TileRing<PlatformTile>[] TileRings;

    private static int fromRing;
    private static int toRing;
    private static bool innerDirection;

    public static List<PlatformTile> GenerateTilePath(Vector3 objPosition, TileRing<PlatformTile>[] tileRings, bool innerDirection = true)
    {
        TileRings = tileRings;
        PlatformTilePathfinder.innerDirection = innerDirection;
        SetUpData(objPosition);

        var tilesPath = DefineTilePath(objPosition);
        TileRings = null;

        if (tilesPath == null || tilesPath.Count == 0 || tilesPath.Contains(null))
        {
            Debug.LogError($"I can not define tile path for position {objPosition}");
            return null;
        }

        return tilesPath;
    }

    private static void SetUpData(Vector3 objPosition)
    {
        toRing = innerDirection ? FirstMiddleZoneRing : LastMiddleZoneRing;
        fromRing = -1;

        float objectDistance = Count2DDistance(objPosition, CitadelPosition);

        if (innerDirection)
        {
            for (int i = LastMiddleZoneRing; i >= FirstMiddleZoneRing; i--)
            {
                if (Count2DDistance(TileRings[i].Tiles[0].transform.position, CitadelPosition) <= objectDistance)
                {
                    fromRing = i;
                    return;
                }
            }
        }
        else
        {
            for (int i = FirstMiddleZoneRing; i <= LastMiddleZoneRing; i++)
            {
                if (Count2DDistance(TileRings[i].Tiles[0].transform.position, CitadelPosition) >= objectDistance)
                {
                    fromRing = i;
                    return;
                }
            }
        }

        Assert.IsTrue(false);
    }

    private static List<PlatformTile> DefineTilePath(Vector3 objPosition)
    {
        float objectAngle = CountAngleFromPosition(objPosition);
        var closestTile = FindClosestTile(TileRings[fromRing].Tiles, objectAngle);

        var firstTiles = new List<PlatformTile>();
        firstTiles.Add(closestTile);

        for (int i = 0; i < 4; i++)
        {
            int index = FindNextIndex(TileRings[fromRing].Tiles.Length, closestTile.TileCell.x, (i % 2) + 1, i < 2);
            firstTiles.Add(TileRings[fromRing].Tiles[index]);
        }

        var allTilePaths = new List<List<PlatformTile>>();
        foreach (var firstTile in firstTiles)
        {
            if (!IsPlatformTileEmpty(firstTile))
            {
                continue;
            }

            var path = new List<PlatformTile>() { firstTile };
            bool result = FindPath(firstTile, path);

            if (result)
            {
                allTilePaths.Add(path);
            }
        }

        return FindTheBestPath(allTilePaths, objPosition);
    }

    private static bool FindPath(PlatformTile startTile, List<PlatformTile> path)
    {
        List<PlatformTile> partOfPath = null;

        var nextFreeForwardTile = GetFreeForwardTile(startTile);
        if (nextFreeForwardTile != null)
        {
            partOfPath = new List<PlatformTile>() { nextFreeForwardTile };
        }
        else
        {
            partOfPath = FindFreeForwardTile(startTile, false);

            if (partOfPath == null)
            {
                partOfPath = FindFreeForwardTile(startTile, true);

                if (partOfPath == null)
                {
                    return false;
                }
            }
            else
            {
                var secondSidePath = FindFreeForwardTile(startTile, true);

                if (secondSidePath != null)
                {
                    float firstSidePathLength = CountPathLength(partOfPath);
                    float secondSidePathLength = CountPathLength(secondSidePath);

                    if (secondSidePathLength < firstSidePathLength)
                    {
                        partOfPath = secondSidePath;
                    }
                }
            }

            nextFreeForwardTile = partOfPath[partOfPath.Count - 1];
        }

        foreach (var tile in partOfPath)
        {
            path.Add(tile);
        }

        if (InLastRing(nextFreeForwardTile))
        {
            return true;
        }

        return FindPath(nextFreeForwardTile, path);

    }

    private static PlatformTile GetFreeForwardTile(PlatformTile fromTile)
    {
        Assert.IsFalse(fromTile.TileCell.y == toRing);

        int nextRingIndex = fromTile.TileCell.y + (innerDirection ? -1 : 1);
        var nextForwardTile = TileRings[nextRingIndex].Tiles[fromTile.TileCell.x];

        if (IsPlatformTileEmpty(nextForwardTile))
        {
            return nextForwardTile;
        }

        return null;
    }

    private static List<PlatformTile> FindFreeForwardTile(PlatformTile startTile, bool clockwiseDirection)
    {
        var sidePath = new List<PlatformTile>();

        int startTileIndex = startTile.TileCell.x;
        var currentRing = TileRings[startTile.TileCell.y];
        var nextRing = TileRings[startTile.TileCell.y + (innerDirection ? -1 : 1)];

        Assert.IsTrue(currentRing.TileCount > 1);
        for (int i = 1; i <= currentRing.TileCount; i++)
        {
            int nextSideTileIndex = FindNextIndex(currentRing.Tiles.Length, startTileIndex, i, clockwiseDirection);
            var nextSideTile = currentRing.Tiles[nextSideTileIndex];

            if (!IsPlatformTileEmpty(nextSideTile))
            {
                return null;
            }

            sidePath.Add(nextSideTile);

            var nextForwardTile = nextRing.Tiles[nextSideTileIndex];
            if (IsPlatformTileEmpty(nextForwardTile))
            {
                sidePath.Add(nextForwardTile);
                return sidePath;
            }
        }

        Debug.LogError($"Tiles number:{currentRing.TileCount}, Start tile index: {startTileIndex}" +
            $", CurrentRing: {startTile.TileCell.y},Direction: {clockwiseDirection}");

        return null;
    }

    private static List<PlatformTile> FindTheBestPath(List<List<PlatformTile>> allTilePaths, Vector3 objPosition)
    {
        Assert.IsTrue(allTilePaths.Count > 0);

        if (allTilePaths.Count == 1)
        {
            return allTilePaths[0];
        }

        float bestPathLength = float.MaxValue;
        List<PlatformTile> bestPath = null;

        foreach (var tilePath in allTilePaths)
        {
            float currentPathLenght = Vector2.SqrMagnitude(ConvertTo2DDimension(objPosition) - ConvertTo2DDimension(tilePath[0].transform.position));
            currentPathLenght += CountPathLength(tilePath);

            if (currentPathLenght < bestPathLength)
            {
                bestPath = tilePath;
                bestPathLength = currentPathLenght;
            }
        }

        return bestPath;
    }

    private static float CountPathLength(List<PlatformTile> path)
    {
        Assert.IsTrue(path.Count > 1);
        float pathLenght = 0f;
        Vector3 prevPoint = path[0].transform.position;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 tilePosition = path[i].transform.position;
            pathLenght += Vector3.SqrMagnitude(prevPoint - tilePosition);
            prevPoint = tilePosition;
        }

        return pathLenght;
    }

    private static PlatformTile FindClosestTile(PlatformTile[] tiles, float objectAngle)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            float tileAngle = CountAngleFromPosition(tiles[i].transform.position);

            if ((tileAngle + tileAngleCorrection) >= objectAngle && (tileAngle - tileAngleCorrection) <= objectAngle)
            {
                return tiles[i];
            }
        }

        Assert.IsTrue(false);
        return null;
    }

    private static bool IsPlatformTileEmpty(PlatformTile platformTile)
    {
        return platformTile.IsEmpty /*&& platformTile.Deposit == null*/;
    }

    private static bool InLastRing(PlatformTile platformTile)
    {
        return platformTile.TileCell.y == toRing;
    }

    #region Helpers
    public static int FindNextIndex(int collectionLength, int sentIndex, int nextValue, bool forward, Predicate<int> CheckCorrectness = null)
    {
        int nextTileIndex = 0;
        for (int i = 0; i < 2; i++)
        {
            nextTileIndex = forward ? sentIndex + nextValue : sentIndex - nextValue;

            if (nextTileIndex < 0)
            {
                int diff = collectionLength - Mathf.Abs(nextTileIndex);
                nextTileIndex = diff;

            }
            else if (nextTileIndex >= collectionLength)
            {
                int diff = nextTileIndex - collectionLength;
                nextTileIndex = diff;
            }

            if (CheckCorrectness != null && !CheckCorrectness(nextTileIndex))
            {
                forward = !forward;
            }
            else
            {
                break;
            }
        }

        return nextTileIndex;
    }

    public static float CountAngleFromPosition(Vector3 pos)
    {
        Vector2 convertedPosition = ConvertTo2DDimension(pos);
        float angle = Vector2.Angle(Vector2.right, convertedPosition);
        return pos.z > 0f ? angle : 360f - angle;
    }

    public static float Count2DDistance(Vector3 from, Vector3 to)
    {
        Vector2 from2D = ConvertTo2DDimension(from);
        Vector2 to2D = ConvertTo2DDimension(to);
        return Vector2.Distance(to2D, from2D);
    }

    public static Vector3 CountPointFromAngle(float angle, float distance)
    {
        float x = Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = Mathf.Sin(angle * Mathf.Deg2Rad);

        return new Vector3(x * distance, 0f, y * distance);
    }

    public static bool ComparePositionsIn2DDimension(Vector3 pos1, Vector3 pos2)
    {
        return Vector2.Distance(ConvertTo2DDimension(pos1), ConvertTo2DDimension(pos2)) < 2f;
    }

    public static Vector2 ConvertTo2DDimension(Vector3 pos)
    {
        return new Vector2(pos.x, pos.z);
    }

    public static bool DefineClockwiseDirection(float objectAngle, float targetAngle)
    {
        int quarterOfObject = DefineQuarterForAngle(objectAngle);
        int quarterOfTarget = DefineQuarterForAngle(targetAngle);

        if (quarterOfObject == quarterOfTarget)
        {
            return objectAngle > targetAngle;
        }

        switch (quarterOfObject)
        {
            case 1:
                if (quarterOfTarget == 2)
                {
                    return false;
                }
                else if (quarterOfTarget == 4)
                {
                    return true;
                }

                if (objectAngle > 45f)
                {
                    return false;
                }
                else
                {
                    return true;
                }

            case 2:
                if (quarterOfTarget == 1)
                {
                    return true;
                }
                else if (quarterOfTarget == 3)
                {
                    return false;
                }

                if (objectAngle > 135f)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            case 3:
                if (quarterOfTarget == 2)
                {
                    return true;
                }
                else if (quarterOfTarget == 4)
                {
                    return false;
                }

                if (objectAngle > 225)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            case 4:
                if (quarterOfTarget == 1)
                {
                    return false;
                }
                else if (quarterOfTarget == 3)
                {
                    return true;
                }

                if (objectAngle > 315)
                {
                    return false;
                }
                else
                {
                    return true;
                }
        }

        return true;
    }

    private static int DefineQuarterForAngle(float angle)
    {
        if (angle <= 90f)
        {
            return 1;
        }

        if (angle <= 180f)
        {
            return 2;
        }

        if (angle <= 270f)
        {
            return 3;
        }

        if (angle <= 360f)
        {
            return 4;
        }

        Assert.IsTrue(false);
        return -1;
    }
    #endregion
}
