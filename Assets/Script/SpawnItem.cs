using UnityEngine;
using System.Text.RegularExpressions;

public class SpawnItem : BasicItem
{
    [Header("Spawn Settings")]
    [SerializeField] private bool enableSpawn = true;

    public void OnFileCopied(string newFileName, string content)
    {
        if (!enableSpawn) return;

        GameObject newObj = Instantiate(targetObject, targetObject.transform.parent);
        newObj.name = fileName + " - 副本";

        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            newObj.transform.position = actualPos;
        }

        BasicItem[] newItemComponents = newObj.GetComponents<BasicItem>();
        foreach (var item in newItemComponents)
        {
            item.SetFileName(newFileName.Replace(".txt", ""));
            item.SetManager(_manager);
        }
    }

    private Vector2Int? ParsePosition(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;

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
