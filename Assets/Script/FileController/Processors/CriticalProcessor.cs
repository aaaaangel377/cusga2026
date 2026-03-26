using UnityEngine;

public class CriticalProcessor : FeatureProcessor
{
    public override string FeatureName => "Critical";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
        LevelFileManager manager = controller.GetManager();
        if (manager != null)
        {
            manager.ReloadScene();
        }
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        return string.Empty;
    }
}
