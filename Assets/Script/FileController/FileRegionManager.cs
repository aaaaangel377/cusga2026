using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FileRegionManager : MonoBehaviour
{
    [Header("区域设置")]
    [SerializeField] private string regionFolderName = "region1";

    [Header("扫描设置")]
    [SerializeField] private float checkInterval = 0.3f;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;

    private const float GRAVITY_MAGNITUDE = 25f;

    private LevelFileManager _manager;
    private string _regionFolderPath;
    private string _parentFolderPath;
    private BoxCollider2D _regionCollider;
    private float _timer = 0f;
    private Vector2 _currentGravity = new Vector2(0, -9.81f);
    private bool _isInitialized = false;
    private bool _hasGravityConfig = false;

    private List<Rigidbody2D> _rigidbodiesInRegion = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, float> _originalGravityScales = new Dictionary<Rigidbody2D, float>();
    private HashSet<string> _filesInRegion = new HashSet<string>();
    private List<BasicItem> _basicItemsInRegion = new List<BasicItem>();
    private List<AdvancedItemController> _advancedItemsInRegion = new List<AdvancedItemController>();

    void Awake()
    {
        _regionCollider = GetComponent<BoxCollider2D>();
        if (_regionCollider == null)
        {
            Debug.LogError($"[FileRegionManager] {gameObject.name}: 未找到 BoxCollider2D 组件，组件将不工作");
            enabled = false;
            return;
        }

        _regionCollider.isTrigger = true;
    }

    void Start()
    {
        if (_regionCollider == null) return;

        if (_manager == null)
        {
            _manager = FindObjectOfType<LevelFileManager>();
        }

        if (_manager != null)
        {
            _regionFolderPath = _manager.CreateRegionFolder(regionFolderName);
            _parentFolderPath = _manager.GetFolderPath();
            
            Debug.Log($"[FileRegionManager] 父文件夹路径：{_parentFolderPath}");
            Debug.Log($"[FileRegionManager] 区域文件夹路径：{_regionFolderPath}");

            ReadGravityConfig();

            _isInitialized = true;
            Debug.Log($"[FileRegionManager] {gameObject.name} 初始化完成，区域：{regionFolderName}");
        }
        else
        {
            Debug.LogError($"[FileRegionManager] {gameObject.name}: 未找到 LevelFileManager");
            enabled = false;
        }
    }

    void Update()
    {
        if (!_isInitialized) return;

        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            ScanRegion();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isInitialized) return;

        Debug.Log($"[FileRegionManager] 物体进入区域：{other.gameObject.name}");

        ProcessObjectEnter(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!_isInitialized) return;

        Debug.Log($"[FileRegionManager] 物体离开区域：{other.gameObject.name}");

        ProcessObjectExit(other);
    }

    void ProcessObjectEnter(Collider2D other)
    {
        BasicItem basicItem = other.GetComponent<BasicItem>();
        AdvancedItemController advancedItem = other.GetComponent<AdvancedItemController>();

        if (basicItem != null)
        {
            MoveFileToRegion(basicItem.FileName, basicItem.Fileclass);
            if (!_basicItemsInRegion.Contains(basicItem))
            {
                _basicItemsInRegion.Add(basicItem);
            }
        }

        if (advancedItem != null)
        {
            MoveFileToRegion(advancedItem.FileName, "txt");
            if (!_advancedItemsInRegion.Contains(advancedItem))
            {
                _advancedItemsInRegion.Add(advancedItem);
            }
        }

        if (_hasGravityConfig)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (!_originalGravityScales.ContainsKey(rb))
                {
                    _originalGravityScales[rb] = rb.gravityScale;
                }
                rb.gravityScale = 0f;

                if (!_rigidbodiesInRegion.Contains(rb))
                {
                    _rigidbodiesInRegion.Add(rb);
                }
            }
        }
    }

    void ProcessObjectExit(Collider2D other)
    {
        BasicItem basicItem = other.GetComponent<BasicItem>();
        AdvancedItemController advancedItem = other.GetComponent<AdvancedItemController>();

        if (basicItem != null)
        {
            MoveFileToParent(basicItem.FileName, basicItem.Fileclass);
            _basicItemsInRegion.Remove(basicItem);
        }

        if (advancedItem != null)
        {
            MoveFileToParent(advancedItem.FileName, "txt");
            _advancedItemsInRegion.Remove(advancedItem);
        }

        if (_hasGravityConfig)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (_originalGravityScales.ContainsKey(rb))
                {
                    rb.gravityScale = _originalGravityScales[rb];
                    _originalGravityScales.Remove(rb);
                }

                _rigidbodiesInRegion.Remove(rb);
            }
        }
    }

    void MoveFileToRegion(string fileName, string fileClass)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        string sourcePath = Path.Combine(_parentFolderPath, $"{fileName}.{fileClass}");
        string destPath = Path.Combine(_regionFolderPath, $"{fileName}.{fileClass}");

        if (File.Exists(sourcePath) && !File.Exists(destPath))
        {
            try
            {
                File.Move(sourcePath, destPath);
                _filesInRegion.Add(fileName);
                Debug.Log($"[FileRegionManager] 文件移入区域：{fileName}.{fileClass}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 移动文件失败：{e.Message}");
            }
        }
    }

    void MoveFileToParent(string fileName, string fileClass)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        string sourcePath = Path.Combine(_regionFolderPath, $"{fileName}.{fileClass}");
        string destPath = Path.Combine(_parentFolderPath, $"{fileName}.{fileClass}");

        if (File.Exists(sourcePath))
        {
            try
            {
                File.Move(sourcePath, destPath);
                _filesInRegion.Remove(fileName);
                Debug.Log($"[FileRegionManager] 文件移出区域：{fileName}.{fileClass}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 移动文件失败：{e.Message}");
            }
        }
    }

    void ScanRegion()
    {
        ReadGravityConfig();
        ApplyGravityToRigidbodies();

        ScanFilesInRegion();
    }

    void ReadGravityConfig()
    {
        if (string.IsNullOrEmpty(_regionFolderPath)) return;
        
        string gravityPath = Path.Combine(_regionFolderPath, "g.cfg.txt");

        if (File.Exists(gravityPath))
        {
            try
            {
                string content = File.ReadAllText(gravityPath).Trim();
                _currentGravity = ParseGravityDirection(content);
                _hasGravityConfig = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 读取重力配置失败：{e.Message}");
                _hasGravityConfig = false;
            }
        }
        else
        {
            _hasGravityConfig = false;
        }
    }

    Vector2 ParseGravityDirection(string direction)
    {
        switch (direction)
        {
            case "上":
                return new Vector2(0, 16*GRAVITY_MAGNITUDE);
            case "下":
                return new Vector2(0, -16*GRAVITY_MAGNITUDE);
            case "左":
                return new Vector2(-16 * 16*GRAVITY_MAGNITUDE, 0);
            case "右":
                return new Vector2(16 * 16*GRAVITY_MAGNITUDE, 0);
            default:
                Debug.LogWarning($"[FileRegionManager] 未知重力方向：'{direction}'，使用默认向下重力");
                return new Vector2(0, -16*GRAVITY_MAGNITUDE);
        }
    }

    void ApplyGravityToRigidbodies()
    {
        if (!_hasGravityConfig) return;
        
        for (int i = _rigidbodiesInRegion.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = _rigidbodiesInRegion[i];
            if (rb != null)
            {
                Vector2 force = _currentGravity * rb.mass;
                rb.AddForce(force, ForceMode2D.Force);
            }
            else
            {
                _rigidbodiesInRegion.RemoveAt(i);
            }
        }
    }

    void ScanFilesInRegion()
    {
        if (!Directory.Exists(_regionFolderPath)) return;

        // 扫描区域文件夹中的文件并更新区域内物体状态
        foreach (var basicItem in _basicItemsInRegion.ToList())
        {
            if (basicItem != null)
            {
                basicItem.UpdateFromFile(_regionFolderPath);
            }
            else
            {
                _basicItemsInRegion.Remove(basicItem);
            }
        }

        foreach (var advancedItem in _advancedItemsInRegion.ToList())
        {
            if (advancedItem != null)
            {
                advancedItem.UpdateFromFile(_regionFolderPath);
            }
            else
            {
                _advancedItemsInRegion.Remove(advancedItem);
            }
        }
    }

    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
    }

    public void DeleteRegionFiles()
    {
        if (!string.IsNullOrEmpty(_regionFolderPath) && Directory.Exists(_regionFolderPath))
        {
            try
            {
                foreach (string file in Directory.GetFiles(_regionFolderPath))
                {
                    File.Delete(file);
                }

                foreach (string dir in Directory.GetDirectories(_regionFolderPath))
                {
                    Directory.Delete(dir, true);
                }

                Debug.Log($"[FileRegionManager] 清理区域文件夹：{_regionFolderPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 清理区域文件夹失败：{e.Message}");
            }
        }
    }

    public string GetRegionFolderPath()
    {
        return _regionFolderPath;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.5f);

        Vector2 center = collider.offset;
        Vector2 size = collider.size;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.green;
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.green;
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
    }
}
