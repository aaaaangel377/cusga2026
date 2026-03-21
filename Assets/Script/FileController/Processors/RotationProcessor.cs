using UnityEngine;
using System.Text.RegularExpressions;

public class RotationProcessor : FeatureProcessor
{
    public override string FeatureName => "Rotation";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
        float rotation = target.transform.rotation.eulerAngles.z;
        string content = $"旋转：{rotation}";
        
        string fullPath = System.IO.Path.Combine(folderPath, $"{fileName}.txt");
        if (!System.IO.File.Exists(fullPath))
        {
            System.IO.File.WriteAllText(fullPath, content);
        }
        else
        {
            string existing = System.IO.File.ReadAllText(fullPath);
            if (!existing.Contains("旋转："))
            {
                System.IO.File.AppendAllText(fullPath, "\n" + content);
            }
        }
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
        if (string.IsNullOrEmpty(content)) return;
        
        float? rotation = ParseRotation(content);
        if (rotation.HasValue)
        {
            target.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
        }
    }
    
    public override void OnFileDeleted(GameObject target)
    {
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        float? rotation = ParseRotation(content);
        if (rotation.HasValue)
        {
            target.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
        }
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        float rotation = target.transform.rotation.eulerAngles.z;
        return $"旋转：{rotation}";
    }
    
    private float? ParseRotation(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        
        Match match = Regex.Match(content, @"旋转：(-?\d+\.?\d*)");
        
        if (match.Success)
        {
            return float.Parse(match.Groups[1].Value);
        }
        
        return null;
    }
}
