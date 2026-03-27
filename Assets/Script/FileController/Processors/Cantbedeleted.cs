using UnityEngine;

public class Cantbedeleted: FeatureProcessor
{
    public override string FeatureName => "Cantbedeleted";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
    }
    
    public override void OnFileDeleted(GameObject target, AdvancedItemController controller)
    {
        target.SetActive(true);
        LevelFileManager manager = controller.GetManager();
        if (manager != null)
        {
            Debug.Log("Cantbedeleted: 文件被删除，正在恢复...");
            controller.CreateDefaultFile(manager.GetFolderPath());
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
