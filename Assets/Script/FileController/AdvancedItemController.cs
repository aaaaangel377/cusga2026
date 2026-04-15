using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AdvancedItemController : MonoBehaviour
{
    [Header("文件设置")]
    [SerializeField] private string fileName;
    
    [Header("初始内容")]
    [TextArea]
    [SerializeField] private string customContent;
    
    // Public accessors
    public string CustomContent => customContent;
    
    [Header("管理对象")]
    [SerializeField] private GameObject targetObject;
    
    [Header("功能开关")]
    [SerializeField] private bool enablePosition = true;
    [SerializeField] private bool enableRotation = false;
    //[SerializeField] private bool enableSpawn = false;
    [SerializeField] private bool enableCritical = false;
    private bool enableVisibility = true;
    [SerializeField] private bool Cantbedeleted = false;
    
    [Header("碰撞箱更新设置")]
    [SerializeField] private bool enableColliderDisableOnUpdate = true;
    [SerializeField] private AudioClip colliderDisableSound;
    [SerializeField] private float colliderDisableDuration = 0.3f;
    
    [Header("初始状态")]
    [SerializeField] private bool startActive = true;
    
    private LevelFileManager _manager;
    private List<FeatureProcessor> _processors;
    
    public string FileName => fileName;
    public bool StartActive => startActive;
    
    void Awake()
    {
        if (targetObject == null)
        {
            targetObject = this.gameObject;
        }
        
        InitializeProcessors();
    }
    
    private void InitializeProcessors()
    {
        _processors = new List<FeatureProcessor>();
        
        if (enablePosition)
        {
            _processors.Add(new PositionProcessor());
        }
        
        if (enableRotation)
        {
            _processors.Add(new RotationProcessor());
        }
        
        // if (enableSpawn)
        // {
        //     _processors.Add(new SpawnProcessor());
        // }
        
        if (enableCritical)
        {
            _processors.Add(new CriticalProcessor());
        }
        
        if (enableVisibility)
        {
            _processors.Add(new VisibilityProcessor());
        }
        if (Cantbedeleted)
        {
            _processors.Add(new Cantbedeleted());
        }
    }
    
    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
    }
    
    public LevelFileManager GetManager()
    {
        return _manager;
    }
    
    public List<FeatureProcessor> GetProcessors()
    {
        return _processors;
    }
    
    public void SetFileName(string newFileName)
    {
        fileName = newFileName;
    }
    
    // public void DisableSpawn()
    // {
    //     //enableSpawn = false;
    //     var spawnProcessor = _processors.Find(p => p is SpawnProcessor);
    //     if (spawnProcessor != null)
    //     {
    //         _processors.Remove(spawnProcessor);
    //     }
    // }
    
    public void CreateDefaultFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.txt");
        
        if (File.Exists(fullPath)) return;
        
        if (!string.IsNullOrEmpty(customContent))
        {
            File.WriteAllText(fullPath, customContent);
        }
        
        foreach (var processor in _processors)
        {
            processor.OnFileCreated(folderPath, fileName, targetObject);
        }
    }
    
    public void UpdateFromFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.txt");
        bool exists = File.Exists(fullPath);
        
        targetObject.SetActive(exists);
        
        if (!exists)
        {
            foreach (var processor in _processors)
            {
                processor.OnFileDeleted(targetObject, this);
            }
            
            if (fileName.Contains(" - 副本"))
            {
                foreach (var processor in _processors)
                {
                    processor.OnCopyFileDeleted(targetObject, this);
                }
            }
            return;
        }
        
        string content = File.ReadAllText(fullPath);
        
        foreach (var processor in _processors)
        {
            processor.OnFileUpdated(content, targetObject);
        }
    }
    
    public void SetInitialInactive()
    {
        targetObject.SetActive(false);
    }
    
    // public void OnFileCopied(string newFileName, string content)
    // {
    //     foreach (var processor in _processors)
    //     {
    //         processor.OnFileCopied(newFileName, content, targetObject, this);
    //     }
    // }
    
    public bool EnableColliderDisableOnUpdate => enableColliderDisableOnUpdate;
    public AudioClip ColliderDisableSound => colliderDisableSound;
    public float ColliderDisableDuration => colliderDisableDuration;
}
