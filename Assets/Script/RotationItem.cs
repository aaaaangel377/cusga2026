using UnityEngine;
using System.Text.RegularExpressions;

public class RotationItem : BasicItem
{
    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;

    protected override void OnFileUpdated(string content)
    {
        if (!enableRotation) return;
        if (string.IsNullOrEmpty(content)) return;

        float? rotation = ParseRotation(content);
        if (rotation.HasValue)
        {
            targetObject.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
        }
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
