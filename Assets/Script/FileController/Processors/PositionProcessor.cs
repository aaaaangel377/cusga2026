using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections;

public class PositionProcessor : FeatureProcessor
{
    public override string FeatureName => "Position";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
        if (string.IsNullOrEmpty(content))
        {
            if (FailSoundThrottle.CanPlay())
            {
                AudioManager.Instance.PlayOneShotEffect("9 - SadRobot", AudioManager.Instance.FileFailVolume,true);
                FailSoundThrottle.MarkPlayed();
            }
            return;
        }
        
        Vector2Int? gridPos = ParsePosition(content);
        if (!gridPos.HasValue || !IsValidPosition(gridPos.Value, target))
        {
            if (FailSoundThrottle.CanPlay())
            {
                AudioManager.Instance.PlayOneShotEffect("9 - SadRobot", AudioManager.Instance.FileFailVolume,true);
                FailSoundThrottle.MarkPlayed();
            }
            return;
        }
        
        Vector3 actualPos = GetActualPosition(gridPos.Value, target);
        
        if (target.transform.position != actualPos)
        {
            if(target.transform.position.y < actualPos.y)
            {
                DisableColliderWithSound(target);
            }
            target.transform.position = actualPos;
            AudioManager.Instance.PlayOneShotEffect("correct", AudioManager.Instance.FileSuccessVolume);
        }
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        Vector2Int? gridPos = ParsePosition(content);
        if (!gridPos.HasValue || !IsValidPosition(gridPos.Value, target)) return;
        
        Vector3 actualPos = GetActualPosition(gridPos.Value, target);
        
        if (target.transform.position != actualPos)
        {
            if(target.transform.position.y< actualPos.y)
            {
                DisableColliderWithSound(target);
            }
            target.transform.position = actualPos;
        }
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        return string.Empty;
    }
    
    private void DisableColliderWithSound(GameObject target)
    {
        AdvancedItemController controller = target.GetComponent<AdvancedItemController>();
        if (controller == null || !controller.EnableColliderDisableOnUpdate) return;
        
        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
            controller.StartCoroutine(ReenableColliderAfterDelay(collider, controller.ColliderDisableDuration));
        }
        
        if (controller.ColliderDisableSound != null)
        {
            AudioSource audioSource = GameManager.Instance?.audioSource;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(controller.ColliderDisableSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(controller.ColliderDisableSound, target.transform.position);
            }
        }
    }
    
    private IEnumerator ReenableColliderAfterDelay(Collider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        collider.enabled = true;
    }
    
    private Vector2Int? ParsePosition(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        
        Match match = Regex.Match(content, @"(-?\d+\.?\d*)[\r\n]+((-?\d+\.?\d*))");
        
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

    private Vector3 GetActualPosition(Vector2Int gridPos, GameObject target)
    {
        LevelFileManager manager = GetManagerFromTarget(target);
        FileRegionManager region = FindRegionForObject(target, manager);
        
        if (region != null)
        {
            Vector3 pos = region.ConvertRegionGridToWorld(gridPos);
            Debug.Log($"[PositionProcessor] {target.name} 在区域内，网格 ({gridPos.x},{gridPos.y}) → 世界 ({pos.x},{pos.y})");
            return pos;
        }
        
        Vector3 normalPos = GridUtils.ConvertToActualPosition(gridPos);
//        Debug.Log($"[PositionProcessor] {target.name} 在区域外，网格 ({gridPos.x},{gridPos.y}) → 世界 ({normalPos.x},{normalPos.y})");
        return normalPos;
    }

    private bool IsValidPosition(Vector2Int gridPos, GameObject target)
    {
        LevelFileManager manager = GetManagerFromTarget(target);
        FileRegionManager region = FindRegionForObject(target, manager);
        
        if (region != null)
        {
            Vector2Int regionSize = region.GetRegionSize();
            return gridPos.x >= 0 && gridPos.x < regionSize.x 
                && gridPos.y >= 0 && gridPos.y < regionSize.y;
        }
        
        return GridUtils.IsValidGrid(gridPos);
    }

    private FileRegionManager FindRegionForObject(GameObject target, LevelFileManager manager)
    {
        if (manager == null) return null;
        
        // 1. 先检查是否被 FileRegionManager 管理
        FileRegionManager[] allRegions = Object.FindObjectsOfType<FileRegionManager>();
        foreach (var r in allRegions)
        {
            if (r.IsObjectManaged(target))
            {
                Debug.Log($"[PositionProcessor] {target.name} 被区域 {r.GetRegionFolderName()} 管理");
                return r;
            }
        }
        
        // 2. 检查注册列表（向后兼容）
        if (manager.IsObjectInRegion(target, out FileRegionManager region))
        {
            Debug.Log($"[PositionProcessor] {target.name} 已在注册列表中，区域：{region.GetRegionFolderName()}");
            return region;
        }
        
        // 3. 如果没有被管理，返回 null（使用全局坐标）
        return null;
    }

    private LevelFileManager GetManagerFromTarget(GameObject target)
    {
        AdvancedItemController advancedItem = target.GetComponent<AdvancedItemController>();
        if (advancedItem != null)
        {
            return advancedItem.GetManager();
        }
        
        BasicItem basicItem = target.GetComponent<BasicItem>();
        if (basicItem != null)
        {
            return basicItem.GetManager();
        }
        
        return null;
    }
}
