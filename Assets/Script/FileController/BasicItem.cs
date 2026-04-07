using UnityEngine;
using System.IO;

public class BasicItem : MonoBehaviour
{
    [Header("文件设置")]
    [SerializeField] protected string fileName;
    [SerializeField] protected string fileContent;
    [SerializeField] protected string fileclass;

    [Header("管理对象")]
    [SerializeField] protected GameObject targetObject;

    protected LevelFileManager _manager;

    public string FileName => fileName;
    public string Fileclass => fileclass;
    public void SetFileName(string newFileName)
    {
        fileName = newFileName;
    }

    void Awake()
    {
        if (targetObject == null)//如果没有指定目标对象，则默认为当前游戏对象
        {
            targetObject = this.gameObject;
        }
    }

    //设置管理器引用
    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
    }

    public LevelFileManager GetManager()
    {
        return _manager;
    }

    public void CreateDefaultFile(string folderPath)
    {   
        string fullPath = Path.Combine(folderPath, $"{fileName}.{fileclass}");

        if (File.Exists(fullPath)) return;

        File.WriteAllText(fullPath, fileContent);
        OnFileCreated();
    }
    //根据文件内容更新对象状态
    public void UpdateFromFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.{fileclass}");
        bool exists = File.Exists(fullPath);

        if (!exists)
        {
            targetObject.SetActive(false);
            OnFileDeleted();
            return;
        }

        targetObject.SetActive(true);
        string content = File.ReadAllText(fullPath);
        OnFileUpdated(content);
    }

    protected virtual void OnFileCreated() { }

    protected virtual void OnFileUpdated(string content) { }

    protected virtual void OnFileDeleted() { }
}
