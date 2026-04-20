using UnityEngine;
using System.IO;

public class HiddenItem : BasicItem
{
    [Header("隐藏文件设置")]
    [SerializeField] private bool createHiddenOnStart = true;

    void Start()
    {
        if (createHiddenOnStart && _manager != null)
        {
            CreateHiddenFile();
        }
    }

    void CreateHiddenFile()
    {
        if (_manager == null)
        {
            Debug.LogError($"[HiddenItem] {gameObject.name}: LevelFileManager 为空");
            return;
        }

        string folderPath = _manager.GetFolderPath();
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError($"[HiddenItem] {gameObject.name}: 文件夹路径为空");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError($"[HiddenItem] {gameObject.name}: fileName 为空");
            return;
        }

        string fullPath = Path.Combine(folderPath, $"{fileName}.{fileclass}");

        try
        {
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, fileContent);
                File.SetAttributes(fullPath, FileAttributes.Hidden);
                Debug.Log($"[HiddenItem] 隐藏文件已创建：{fullPath}");
            }
            else
            {
                File.SetAttributes(fullPath, FileAttributes.Hidden);
                Debug.Log($"[HiddenItem] 隐藏文件已存在并设置为隐藏：{fullPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HiddenItem] 创建隐藏文件失败：{e.Message}");
        }
    }

    public new void UpdateFromFile(string folderPath)
    {
        base.UpdateFromFile(folderPath);
    }
}
