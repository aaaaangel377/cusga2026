using System.IO;
using UnityEngine;

public class ResourceMove : MonoBehaviour
{
    private LevelFileManager _manager;
    
    void Start()
    {
        _manager = FindObjectOfType<LevelFileManager>();
    }
    
    public void MoveResource(string fileNameWithExtension)
    {
        if (_manager == null)
        {
            Debug.LogError("[ResourceMove] Manager not set, cannot move resource");
            return;
        }
        
        string resourceFullPath = Path.Combine(Application.dataPath, "Resources", fileNameWithExtension);
        string destinationFullPath = Path.Combine(_manager.GetFolderPath(), fileNameWithExtension);
        
        if (File.Exists(destinationFullPath))
        {
            Debug.Log($"[ResourceMove] 目标文件已存在，跳过：{destinationFullPath}");
            return;
        }
        
        if (!File.Exists(resourceFullPath))
        {
            Debug.LogError($"[ResourceMove] 资源文件未找到：{resourceFullPath}");
            return;
        }
        
        File.Copy(resourceFullPath, destinationFullPath);
        Debug.Log($"[ResourceMove] 资源已复制：{resourceFullPath} -> {destinationFullPath}");
    }
}
