using UnityEngine;
using System.Text.RegularExpressions;

public class RotationProcessor : FeatureProcessor
{
    public override string FeatureName => "Rotation";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    bool Fal=false;
    private float? _lastRotationValue;
    public override void OnFileUpdated(string content, GameObject target)
    {
        if (string.IsNullOrEmpty(content))
        {
            if (FailSoundThrottle.CanPlay()&&Fal==!true)
            {
                AudioManager.Instance.PlayOneShotEffect("9 - SadRobot", AudioManager.Instance.FileFailVolume,true);
                FailSoundThrottle.MarkPlayed();
                Fal = true;
            }
            return;
        }
        
        float? rotation = ParseRotation(content);
        if (!rotation.HasValue&&Fal==!true)
        {
            if (FailSoundThrottle.CanPlay())
            {
                AudioManager.Instance.PlayOneShotEffect("9 - SadRobot", AudioManager.Instance.FileFailVolume,true);
                FailSoundThrottle.MarkPlayed();
                Fal = true;
            }
            return;
        }
        
        FailSoundThrottle.Reset();
        if (!_lastRotationValue.HasValue || _lastRotationValue.Value != rotation.Value)
        {
            target.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
            AudioManager.Instance.PlayOneShotEffect("correct", AudioManager.Instance.FileSuccessVolume);
            _lastRotationValue = rotation.Value;
            Fal=false;
            Debug.Log($"Rotation updated to {rotation.Value} degrees");
        }
        else
        {
            target.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
        }
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
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
        return string.Empty;
    }
    
    private float? ParseRotation(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        
        Match match = Regex.Match(content, @"(-?\d+\.?\d*)");
        
        if (match.Success)
        {
            return float.Parse(match.Groups[1].Value);
        }
        
        return null;
    }
}
