using UnityEngine;

public static class GridUtils
{
    public const int GRID_MAX_X = 11;
    public const int GRID_MAX_Y = 8;

    public static Vector2Int ConvertToGridPosition(Vector3 actualPos)
    {
        int gridX = Mathf.RoundToInt((actualPos.x / 1.1f) + 5.5f);
        int gridY = Mathf.RoundToInt((actualPos.y / 1.1f) + 4f);
        return new Vector2Int(gridX, gridY);
    }

    public static Vector3 ConvertToActualPosition(Vector2Int gridPos)
    {
        float actualX = (gridPos.x - 5.5f) * 1.1f;
        float actualY = (gridPos.y - 4f) * 1.1f;
        return new Vector3(actualX, actualY, 0);
    }

    public static bool IsValidGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x <= GRID_MAX_X &&
               pos.y >= 0 && pos.y <= GRID_MAX_Y;
    }
}
