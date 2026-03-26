using UnityEngine;
using System.Text.RegularExpressions;

public class PositionProcessor : FeatureProcessor
{
    public override string FeatureName => "Position";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
        if (string.IsNullOrEmpty(content)) return;
        
        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            target.transform.position = actualPos;
        }
    }
    
    public override void OnFileDeleted(GameObject target)
    {
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            target.transform.position = actualPos;
        }
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        return string.Empty;
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
