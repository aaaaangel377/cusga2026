using UnityEngine;
using System.Text.RegularExpressions;

public class PositionItem : BasicItem
{
    [Header("Position Settings")]
    [SerializeField] private bool enablePosition = true;

    protected override void OnFileUpdated(string content)
    {
        if (!enablePosition) return;
        if (string.IsNullOrEmpty(content)) return;

        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            targetObject.transform.position = actualPos;
        }
    }

    private Vector2Int? ParsePosition(string content)
    {
        Match match = Regex.Match(content, @"位置：(-?\d+\.?\d*),(-?\d+\.?\d*)");

        if (match.Success)
        {
            float x = float.Parse(match.Groups[1].Value);
            float y = float.Parse(match.Groups[2].Value);

            if (IsInteger(x) && IsInteger(y))
            {
                return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            }
        }

        return null;
    }

    private bool IsInteger(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value));
    }
}
