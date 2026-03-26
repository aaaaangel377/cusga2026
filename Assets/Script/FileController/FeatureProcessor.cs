using UnityEngine;

public abstract class FeatureProcessor
{
    public abstract string FeatureName { get; }
    
    public abstract void OnFileCreated(string folderPath, string fileName, GameObject target);
    
    public abstract void OnFileUpdated(string content, GameObject target);
    
    public virtual void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
        OnFileDeleted(target);
    }
    
    protected virtual void OnFileDeleted(GameObject target)
    {
    }
    
    public virtual void OnCopyFileDeleted(GameObject target, AdvancedItemController controller)
    {
        Object.Destroy(target);
    }
    
    public abstract void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller);
    
    public abstract string CreateDefaultContent(GameObject target);
}
