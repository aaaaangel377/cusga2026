using UnityEngine;
using System.IO;

public class AudioConfig : MonoBehaviour
{
    [Header("扫描设置")]
    [SerializeField] private float checkInterval = 0.5f;
    
    // 静态变量，供其他脚本直接访问
    public static bool CanJump { get; private set; } = true;
    public static bool CanWalk { get; private set; } = true;
    public static bool CanDeath { get; private set; } = true;
    public static bool BgmEnabledStatic { get; private set; } = true;
    
    // 实例属性（保留用于内部逻辑）
    public bool BgmEnabled { get; private set; } = true;
    public bool WalkSoundEnabled { get; private set; } = true;
    public bool JumpSoundEnabled { get; private set; } = true;
    public bool DeathSoundEnabled { get; private set; } = true;
    
    private string _folderPath;
    private string _configPath;
    private float _timer = 0f;
    private string _lastConfigContent;
    private LevelFileManager _levelFileManager;
    
    void Start()
    {
        _levelFileManager = FindObjectOfType<LevelFileManager>();
        
        string gameRoot = Directory.GetParent(Application.dataPath).FullName;
        
        if (_levelFileManager != null)
        {
            _folderPath = _levelFileManager.GetFolderPath();
        }
        
        // 如果 _folderPath 为空，使用备用方案
        if (string.IsNullOrEmpty(_folderPath))
        {
            string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _folderPath = Path.Combine(gameRoot, "level", levelName);
        }
        
        _configPath = Path.Combine(_folderPath, "Audio.cfg.txt");
        
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }
        
        if (!File.Exists(_configPath))
        {
            string defaultContent = "bgm：开\n走路音效：开\n跳跃音效：开\n死亡音效：开";
            File.WriteAllText(_configPath, defaultContent);
            Debug.Log("[AudioConfig] 创建默认配置文件：" + _configPath);
        }
        
        LoadConfig();
    }
    
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            ScanConfig();
        }
    }
    
    void ScanConfig()
    {
        if (!File.Exists(_configPath)) return;
        
        string currentContent = File.ReadAllText(_configPath);
        
        if (currentContent != _lastConfigContent)
        {
            _lastConfigContent = currentContent;
            LoadConfig();
            
            if (!BgmEnabled && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
            }
        }
    }
    
    void LoadConfig()
    {
        BgmEnabled = true;
        WalkSoundEnabled = true;
        JumpSoundEnabled = true;
        DeathSoundEnabled = true;
        
        if (!File.Exists(_configPath)) return;
        
        string content = File.ReadAllText(_configPath);
        string[] lines = content.Split('\n');
        
        BgmEnabled = false;
        WalkSoundEnabled = false;
        JumpSoundEnabled = false;
        DeathSoundEnabled = false;
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (trimmedLine == "bgm：开") BgmEnabled = true;
            else if (trimmedLine == "走路音效：开") WalkSoundEnabled = true;
            else if (trimmedLine == "跳跃音效：开") JumpSoundEnabled = true;
            else if (trimmedLine == "死亡音效：开") DeathSoundEnabled = true;
        }
        
        // 更新静态变量
        CanJump = JumpSoundEnabled;
        CanWalk = WalkSoundEnabled;
        CanDeath = DeathSoundEnabled;
        BgmEnabledStatic = BgmEnabled;
        
        Debug.Log($"[AudioConfig] 配置加载 - BGM: {BgmEnabled}, 走路：{WalkSoundEnabled}, 跳跃：{JumpSoundEnabled}, 死亡：{DeathSoundEnabled}");
    }
}
