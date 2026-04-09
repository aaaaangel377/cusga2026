using System.IO;
using UnityEngine;
using UnityEngine.Events;
public class ResourceMove : MonoBehaviour
{
    public UnityEvent moveResourceEvent;
    private LevelFileManager _manager;
    
    void Start()
    {
        _manager = FindObjectOfType<LevelFileManager>();
        if (moveResourceEvent != null)
        {
            moveResourceEvent.Invoke();
        }
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

    public void MoveResourceFolder(string folderName)
    {
        if (_manager == null)
        {
            Debug.LogError("[ResourceMove] Manager not set, cannot move resource folder");
            return;
        }
        
        string resourceFullPath = Path.Combine(Application.dataPath, "Resources", folderName);
        string destinationFullPath = Path.Combine(_manager.GetFolderPath(), folderName);
        
        if (Directory.Exists(destinationFullPath))
        {
            Debug.Log($"[ResourceMove] 目标文件夹已存在，跳过：{destinationFullPath}");
            return;
        }
        
        if (!Directory.Exists(resourceFullPath))
        {
            Debug.LogError($"[ResourceMove] 资源文件夹未找到：{resourceFullPath}");
            return;
        }
        
        CopyDirectory(resourceFullPath, destinationFullPath);
        Debug.Log($"[ResourceMove] 文件夹已复制：{resourceFullPath} -> {destinationFullPath}");
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    public void MoveResourceWithRelativePath(string pathAndFile)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        
        string[] parts = pathAndFile.Split('|', 2);
        if (parts.Length != 2)
        {
            Debug.LogError($"[ResourceMove] 参数格式错误，应为 'relativePath|fileName'：{pathAndFile}");
            return;
        }
        
        string relativePath = parts[0];
        string fileName = parts[1];
        
        string resourceFullPath = Path.Combine(Application.dataPath, "Resources", fileName);
        string destinationFullPath = Path.Combine(projectRoot, relativePath, fileName);
        
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
        Debug.Log($"[ResourceMove] 资源已复制到相对路径：{resourceFullPath} -> {destinationFullPath}");
    }

    public void MoveResourceFolderWithRelativePath(string pathAndFolder)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        
        string[] parts = pathAndFolder.Split('|', 2);
        if (parts.Length != 2)
        {
            Debug.LogError($"[ResourceMove] 参数格式错误，应为 'relativePath|folderName'：{pathAndFolder}");
            return;
        }
        
        string relativePath = parts[0];
        string folderName = parts[1];
        
        string resourceFullPath = Path.Combine(Application.dataPath, "Resources", folderName);
        string destinationFullPath = Path.Combine(projectRoot, relativePath, folderName);
        
        if (Directory.Exists(destinationFullPath))
        {
            Debug.Log($"[ResourceMove] 目标文件夹已存在，跳过：{destinationFullPath}");
            return;
        }
        
        if (!Directory.Exists(resourceFullPath))
        {
            Debug.LogError($"[ResourceMove] 资源文件夹未找到：{resourceFullPath}");
            return;
        }
        
        CopyDirectory(resourceFullPath, destinationFullPath);
        Debug.Log($"[ResourceMove] 文件夹已复制到相对路径：{resourceFullPath} -> {destinationFullPath}");
    }
}
