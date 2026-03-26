using UnityEngine;

public class VisibilityProcessor : FeatureProcessor
{
    public override string FeatureName => "Visibility";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
        target.SetActive(false);
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        return string.Empty;
    }
}
