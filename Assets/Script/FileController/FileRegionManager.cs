using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FileRegionManager : MonoBehaviour
{
    [Header("区域设置")]
    [SerializeField] private string regionFolderName = "region1";
    [Header("重力系数")]
    [SerializeField] private float Up=1.0f;
    [SerializeField] private float Do=1.0f;
    [SerializeField] private float Le=1.0f;
    [SerializeField] private float Ri=1.0f;
    [Header("扫描设置")]
    [SerializeField] private float checkInterval = 0.3f;

    [Header("碰撞箱设置")]
    [SerializeField] private Transform colliderChild;

    [Header("预设对象")]
    [Tooltip("默认生成在区域文件夹中的物体，这些物体的文件会创建在区域文件夹内")]
    [SerializeField] private AdvancedItemController[] presetItems;

    [Header("区域重力预设")]
    [Tooltip("如果数组不为空，会在区域文件夹内创建重力配置文件")]
    [SerializeField] private GravityController[] gravityPresets;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;

    private LevelFileManager _manager;
    private string _regionFolderPath;
    private string _parentFolderPath;
    private Collider2D _childCollider;
    private float _timer = 0f;
    private Vector2 _currentGravity = new Vector2(0, -9.81f);
    private float _gravityMagnitude = 9.81f;
    private bool _isInitialized = false;
    private bool _hasGravityConfig = false;
    private Vector2Int _parentGridPos;
    private Vector2Int _regionSize;

    private List<Rigidbody2D> _rigidbodiesInRegion = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, float> _originalGravityScales = new Dictionary<Rigidbody2D, float>();
    private HashSet<string> _filesInRegion = new HashSet<string>();
    
    // 重力配置
    private string _lastGravityContent;

    void Awake()
    {
        if (colliderChild != null)
        {
            _childCollider = colliderChild.GetComponent<Collider2D>();
        }
        else
        {
            _childCollider = GetComponentInChildren<Collider2D>();
        }

        if (_childCollider == null)
        {
            Debug.LogError($"[FileRegionManager] {gameObject.name}: 未找到子物体碰撞箱，组件将不工作");
            enabled = false;
            return;
        }
        
        Debug.Log($"[FileRegionManager] Collider 设置：{_childCollider.gameObject.name}, IsTrigger={_childCollider.isTrigger}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[FileRegionManager] OnTriggerEnter2D 被调用：{other.gameObject.name}, _isInitialized={_isInitialized}");
        
        if (!_isInitialized)
        {
            Debug.LogWarning($"[FileRegionManager] 尚未初始化，忽略触发器");
            return;
        }
        
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"[FileRegionManager] 检测到 Rigidbody: {other.gameObject.name}, gravityScale={rb.gravityScale}, 已在列表中={_rigidbodiesInRegion.Contains(rb)}");
            if (!_rigidbodiesInRegion.Contains(rb))
            {
                _rigidbodiesInRegion.Add(rb);
                if (!_originalGravityScales.ContainsKey(rb))
                {
                    _originalGravityScales[rb] = rb.gravityScale;
                }
                Debug.Log($"[FileRegionManager] 物体进入区域：{other.gameObject.name}, 列表数={_rigidbodiesInRegion.Count}");
            }
        }
        else
        {
            Debug.LogWarning($"[FileRegionManager] 物体 {other.gameObject.name} 没有 Rigidbody2D 组件");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!_isInitialized) return;
        
        Debug.Log($"[FileRegionManager] OnTriggerExit2D: {other.gameObject.name}");
        
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null && _rigidbodiesInRegion.Contains(rb))
        {
            _rigidbodiesInRegion.Remove(rb);
            RestoreGravityScale(rb);
            Debug.Log($"[FileRegionManager] 物体离开区域：{other.gameObject.name}, 列表数={_rigidbodiesInRegion.Count}");
        }
    }

    void Start()
    {
        if (_childCollider == null) return;
        _gravityMagnitude = Physics2D.gravity.magnitude;

        if (_manager == null)
        {
            _manager = FindObjectOfType<LevelFileManager>();
        }

        if (_manager != null)
        {
            _regionFolderPath = _manager.CreateRegionFolder(regionFolderName);
            _parentFolderPath = _manager.GetFolderPath();
            
            CalculateParentGridPosition();
            CalculateRegionSize();
            
            ReadGravityConfig();

            InitializePresetItems();

            InitializeRegionGravity();

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
            Debug.Log($"[FileRegionManager] ScanRegion: 区域={regionFolderName}, Rigidbody 数={_rigidbodiesInRegion.Count}, 重力={_hasGravityConfig}");
            ScanRegion();
        }
    }

    void FixedUpdate()
    {
        if (!_isInitialized || !_hasGravityConfig) return;
        ApplyGravityToRigidbodies();
    }

    void InitializePresetItems()
    {
        if (presetItems == null || presetItems.Length == 0)
        {
            return;
        }
        
        foreach (var item in presetItems)
        {
            if (item == null) continue;
            
            if (!item.StartActive)
            {
                // Debug.Log($"[FileRegionManager] 预设对象 {item.gameObject.name} startActive=false，跳过文件创建");
                continue;
            }
            
            string fileName = item.FileName;
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning($"[FileRegionManager] 预设对象 {item.gameObject.name} 的文件名为空，跳过");
                continue;
            }
            
            string regionFilePath = Path.Combine(_regionFolderPath, $"{fileName}.txt");
            
            string content = !string.IsNullOrEmpty(item.CustomContent) ? item.CustomContent : "0\n0";
            
            try
            {
                if (!File.Exists(regionFilePath))
                {
                    File.WriteAllText(regionFilePath, content);
                    Debug.Log($"[FileRegionManager] 预设对象文件已创建：{regionFilePath}, 内容={content}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 创建预设对象文件失败：{e.Message}");
            }
        }
    }

    void InitializeRegionGravity()
    {
        if (gravityPresets == null || gravityPresets.Length == 0) return;
        
        if (string.IsNullOrEmpty(_regionFolderPath))
        {
            Debug.LogWarning("[FileRegionManager] 区域文件夹路径为空，跳过重力初始化");
            return;
        }
        
        foreach (var gravityController in gravityPresets)
        {
            if (gravityController == null) continue;
            
            string fileName = gravityController.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning($"[FileRegionManager] 重力预设对象 {gravityController.gameObject.name} 的文件名为空，跳过");
                continue;
            }
            
            string gravityPath = Path.Combine(_regionFolderPath, $"{fileName}.txt");
            string content = gravityController.DefaultContent;
            
            try
            {
                if (!File.Exists(gravityPath))
                {
                    File.WriteAllText(gravityPath, content);
                    Debug.Log($"[FileRegionManager] 区域重力文件已创建：{gravityPath}, 内容={content}");
                }
                else
                {
                    Debug.Log($"[FileRegionManager] 区域重力文件已存在：{gravityPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 创建区域重力文件失败：{e.Message}");
            }
        }
    }

    void CalculateParentGridPosition()
    {
        _parentGridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x - 0.5f),
            Mathf.RoundToInt(8.5f - transform.position.y)
        );
    }

    void CalculateRegionSize()
    {
        if (_childCollider == null) return;
        _regionSize = new Vector2Int(
            Mathf.RoundToInt(_childCollider.bounds.size.x),
            Mathf.RoundToInt(_childCollider.bounds.size.y)
        );
    }

    public Vector3 ConvertRegionGridToWorld(Vector2Int fileGridPos)
    {
        Vector2Int actualGridPos = _parentGridPos + fileGridPos;
        return GridUtils.ConvertToActualPosition(actualGridPos);
    }

    public bool IsPointInRegion(Vector3 worldPos)
    {
        if (_childCollider == null) return false;
        
        Bounds bounds = _childCollider.bounds;
        
        // 缩小边界，避免边沿触发（X 和 Y 方向各缩小 0.2 单位）
        float margin = 0.2f;
        Vector3 min = bounds.min + new Vector3(margin, margin, 0);
        Vector3 max = bounds.max - new Vector3(margin, margin, 0);
        
        bool result = worldPos.x >= min.x && worldPos.x <= max.x &&
                      worldPos.y >= min.y && worldPos.y <= max.y;
        
        //Debug.Log($"[FileRegionManager] IsPointInRegion: pos=({worldPos.x},{worldPos.y}), bounds=[({min.x},{min.y})-({max.x},{max.y})], result={result}");
        return result;
    }

    public Vector2Int GetRegionSize() => _regionSize;

    public Vector2Int GetParentGridPos() => _parentGridPos;

    void ScanRegion()
    {
        ReadGravityConfig();
        ScanRigidbodiesInRegion();
        ApplyGravityToRigidbodies();
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
                
                if (content != _lastGravityContent)
                {
                    _currentGravity = ParseGravityDirection(content);
                    _hasGravityConfig = true;
                    _lastGravityContent = content;
                    Debug.Log($"[FileRegionManager] 区域重力变化：{regionFolderName}，方向={content}");
                }
                else
                {
                    _hasGravityConfig = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 读取重力配置失败：{e.Message}");
                _hasGravityConfig = false;
            }
        }
        else
        {
            if (_hasGravityConfig)
            {
                Debug.Log($"[FileRegionManager] g.cfg.txt 离开区域 {regionFolderName}，重力已恢复");
            }
            _hasGravityConfig = false;
            _lastGravityContent = null;
        }
    }

    Vector2 ParseGravityDirection(string direction)
    {
        switch (direction)
        {
            case "上":
                return new Vector2(0, Up*_gravityMagnitude);
            case "下":
                return new Vector2(0, -Do*_gravityMagnitude);
            case "左":
                return new Vector2(-16 * Le*_gravityMagnitude, 0);
            case "右":
                return new Vector2(16 * Ri*_gravityMagnitude, 0);
            default:
                Debug.LogWarning($"[FileRegionManager] 未知重力方向：'{direction}'，使用默认向下重力");
                return new Vector2(0, -_gravityMagnitude);
        }
    }

    void ScanRigidbodiesInRegion()
    {
        if (_childCollider == null) return;
        
        Collider2D[] colliders = new Collider2D[100];
        int count = Physics2D.OverlapCollider(_childCollider, new ContactFilter2D(), colliders);
        
        HashSet<Rigidbody2D> currentRigidbodies = new HashSet<Rigidbody2D>();
        
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = colliders[i].GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                currentRigidbodies.Add(rb);
                
                if (!_rigidbodiesInRegion.Contains(rb))
                {
                    _rigidbodiesInRegion.Add(rb);
                    if (!_originalGravityScales.ContainsKey(rb))
                    {
                        _originalGravityScales[rb] = rb.gravityScale;
                    }
                    Debug.Log($"[FileRegionManager] 检测到区域内物体：{colliders[i].gameObject.name}, 列表数={_rigidbodiesInRegion.Count}");
                }
            }
        }
        
        for (int i = _rigidbodiesInRegion.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = _rigidbodiesInRegion[i];
            if (rb == null || !currentRigidbodies.Contains(rb))
            {
                _rigidbodiesInRegion.RemoveAt(i);
                if (rb != null)
                {
                    RestoreGravityScale(rb);
                }
                Debug.Log($"[FileRegionManager] 物体离开区域，列表数={_rigidbodiesInRegion.Count}");
            }
        }
    }

    void ApplyGravityToRigidbodies()
    {
        if (!_hasGravityConfig) return;
        
        if (_rigidbodiesInRegion.Count == 0) return;
        
        for (int i = _rigidbodiesInRegion.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = _rigidbodiesInRegion[i];
            if (rb != null)
            {
                rb.gravityScale = 0f;
                Vector2 force = _currentGravity * rb.mass;
                rb.AddForce(force, ForceMode2D.Force);
            }
            else
            {
                _rigidbodiesInRegion.RemoveAt(i);
            }
        }
    }

    void RestoreGravityScale(Rigidbody2D rb)
    {
        if (rb == null) return;

        if (_originalGravityScales.TryGetValue(rb, out float originalGravityScale))
        {
            rb.gravityScale = originalGravityScale;
            _originalGravityScales.Remove(rb);
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    void ScanFilesInRegion()
    {
        // 不再扫描文件，由 LevelFileManager 统一管理
    }

    void CheckObjectsInRegion()
    {
        // 不再自动检测物体进入/离开区域
        // 改为由 LevelFileManager 检测文件移动后通知
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

    public string GetRegionFolderName()
    {
        return regionFolderName;
    }
    
    public bool IsGravityPreset(GravityController gravityController)
    {
        if (gravityPresets == null || gravityPresets.Length == 0) return false;
        
        foreach (var preset in gravityPresets)
        {
            if (preset == gravityController) return true;
        }
        return false;
    }
    
    public bool IsPresetItem(AdvancedItemController item)
    {
        if (presetItems == null || presetItems.Length == 0) return false;
        
        foreach (var preset in presetItems)
        {
            if (preset == null) continue;
            
            if (preset == item) return true;
        }
        return false;
    }
    
    public string GetPresetItemRegionPath(AdvancedItemController item)
    {
        if (!IsPresetItem(item)) return null;
        return _regionFolderPath;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Collider2D collider = null;
        
        if (colliderChild != null)
        {
            collider = colliderChild.GetComponent<Collider2D>();
        }
        else
        {
            collider = GetComponentInChildren<Collider2D>();
        }
        
        if (collider == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);

        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x - 0.5f),
            Mathf.RoundToInt(8.5f - transform.position.y)
        );
        //Debug.Log($"[FileRegionManager] {gameObject.name}: 父物体网格坐标 ({gridPos.x}, {gridPos.y})");
    }

    void OnValidate()
    {
        if (!Application.isPlaying && showGizmos)
        {
            Collider2D collider = null;
            
            if (colliderChild != null)
            {
                collider = colliderChild.GetComponent<Collider2D>();
            }
            else
            {
                collider = GetComponentInChildren<Collider2D>();
            }
            
            // if (collider != null)
            // {
            //     Gizmos.color = new Color(0, 1, 0, 0.5f);
            //     Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            // }
        }
    }

}
