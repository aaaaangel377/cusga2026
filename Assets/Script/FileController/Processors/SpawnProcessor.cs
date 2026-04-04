/*using UnityEngine;

public class SpawnProcessor : FeatureProcessor
{
    public override string FeatureName => "Spawn";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
    }
    
    public override void OnCopyFileDeleted(GameObject target, AdvancedItemController controller)
    {
        LevelFileManager manager = controller.GetManager();
        if (manager != null)
        {
            manager.UnregisterAdvancedItem(controller);
        }
        Object.Destroy(target);
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        GameObject newObj = Object.Instantiate(target, target.transform.parent);
        newObj.name = newFileName.Replace(".txt", "");
        
        AdvancedItemController newItem = newObj.GetComponent<AdvancedItemController>();
        if (newItem == null)
        {
            newItem = newObj.AddComponent<AdvancedItemController>();
        }
        
        newItem.SetFileName(newFileName.Replace(".txt", ""));
        newItem.SetManager(controller.GetManager());
        newItem.DisableSpawn();
        
        if (newObj.GetComponent<Rigidbody2D>() == null)
        {
            newObj.AddComponent<Rigidbody2D>();
        }
        
        LevelFileManager manager = controller.GetManager();
        if (manager != null)
        {
            manager.RegisterAdvancedItem(newItem);
        }
        
        foreach (var processor in newItem.GetProcessors())
        {
            processor.OnFileCopied(newFileName, content, newObj, newItem);
        }
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        return string.Empty;
    }
}*/
