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
        if (string.IsNullOrEmpty(content)) return;
        
        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            if (target.transform.position != actualPos)
            {
                target.transform.position = actualPos;
                DisableColliderWithSound(target);
            }
        }
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && GridUtils.IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = GridUtils.ConvertToActualPosition(gridPos.Value);
            if (target.transform.position != actualPos)
            {
                target.transform.position = actualPos;
                DisableColliderWithSound(target);
            }
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
}
