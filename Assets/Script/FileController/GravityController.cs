using UnityEngine;
using System.IO;

public class GravityController : MonoBehaviour
{
    [SerializeField] private string fileName = "g.cfg";
    
    private string _folderPath;
    private float _checkInterval = 0.5f;
    private string _lastContent;
    private float _timer = 0f;
    private float _gravityMagnitude;
    
    void Start()
    {
        LevelFileManager manager = FindObjectOfType<LevelFileManager>();
        if (manager == null)
        {
            Debug.LogError("GravityController: 未找到 LevelFileManager");
            return;
        }
        
        _folderPath = manager.GetFolderPath();
        _checkInterval = manager.GetCheckInterval();
        
        if (string.IsNullOrEmpty(_folderPath))
        {
            Debug.LogError("GravityController: 文件夹路径为空");
            return;
        }
        
        Debug.Log($"GravityController: 文件夹路径 = {_folderPath}");
        
        _gravityMagnitude = Physics2D.gravity.magnitude;
        CreateDefaultFile();
        CheckFile();
    }
    
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            CheckFile();
        }
    }
    
    void CreateDefaultFile()
    {
        if (string.IsNullOrEmpty(_folderPath)) return;
        
        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");
        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, "下");
            _lastContent = "下";
        }
    }
    
    void CheckFile()
    {
        if (string.IsNullOrEmpty(_folderPath)) return;
        
        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");
        if (!File.Exists(fullPath)) return;
        
        string content = File.ReadAllText(fullPath).Trim();
        if (content != _lastContent)
        {
            _lastContent = content;
            ApplyGravity(content);
        }
    }
    
    void ApplyGravity(string direction)
    {
        Vector2 newGravity;
        switch (direction)
        {
            case "上": newGravity = new Vector2(0, _gravityMagnitude); break;
            case "下": newGravity = new Vector2(0, -_gravityMagnitude); break;
            case "左": newGravity = new Vector2(-_gravityMagnitude, 0); break;
            case "右": newGravity = new Vector2(_gravityMagnitude, 0); break;
            default: return;
        }
        Physics2D.gravity = newGravity;
    }
}
