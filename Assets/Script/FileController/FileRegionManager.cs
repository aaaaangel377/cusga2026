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

    [Header("碰撞箱设置")]
    [SerializeField] private Transform colliderChild;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;

    private const float GRAVITY_MAGNITUDE = 25f;

    private LevelFileManager _manager;
    private string _regionFolderPath;
    private string _parentFolderPath;
    private Collider2D _childCollider;
    private float _timer = 0f;
    private Vector2 _currentGravity = new Vector2(0, -9.81f);
    private bool _isInitialized = false;
    private bool _hasGravityConfig = false;
    private Vector2Int _parentGridPos;
    private Vector2Int _regionSize;

    private List<Rigidbody2D> _rigidbodiesInRegion = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, float> _originalGravityScales = new Dictionary<Rigidbody2D, float>();
    private HashSet<string> _filesInRegion = new HashSet<string>();
    private List<BasicItem> _basicItemsInRegion = new List<BasicItem>();
    private List<AdvancedItemController> _advancedItemsInRegion = new List<AdvancedItemController>();
    private HashSet<GameObject> _objectsInRegion = new HashSet<GameObject>();
    private Dictionary<GameObject, Vector3> _objectPositions = new Dictionary<GameObject, Vector3>();

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

        _childCollider.isTrigger = true;
    }

    void Start()
    {
        if (_childCollider == null) return;

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
            
            Debug.Log($"[FileRegionManager] 父文件夹路径：{_parentFolderPath}");
            Debug.Log($"[FileRegionManager] 区域文件夹路径：{_regionFolderPath}");
            Debug.Log($"[FileRegionManager] 父物体网格坐标：{_parentGridPos}");
            Debug.Log($"[FileRegionManager] 区域大小：{_regionSize}");

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



    public void OnObjectEnterRegion(Collider2D other)
    {
        if (!_isInitialized) return;

        Debug.Log($"[FileRegionManager] 物体进入区域：{other.gameObject.name}");

        ProcessObjectEnter(other);
        
        _objectsInRegion.Add(other.gameObject);
        _manager?.RegisterRegionObject(other.gameObject, this);
    }

    public void OnObjectExitRegion(Collider2D other)
    {
        if (!_isInitialized) return;

        Debug.Log($"[FileRegionManager] 物体离开区域：{other.gameObject.name}");

        ProcessObjectExit(other);
        
        _objectsInRegion.Remove(other.gameObject);
        _manager?.UnregisterRegionObject(other.gameObject);
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
            _manager?.RegisterItemInRegion(advancedItem.FileName, "txt");
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
            _manager?.UnregisterItemInRegion(advancedItem.FileName, "txt");
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
        
        Debug.Log($"[FileRegionManager] IsPointInRegion: pos=({worldPos.x},{worldPos.y}), bounds=[({min.x},{min.y})-({max.x},{max.y})], result={result}");
        return result;
    }

    public Vector2Int GetRegionSize() => _regionSize;

    public Vector2Int GetParentGridPos() => _parentGridPos;

    public bool IsObjectInRegion(GameObject obj) => _objectsInRegion.Contains(obj);

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

    void MoveObjectFileToRegion(GameObject obj)
    {
        AdvancedItemController advancedItem = obj.GetComponent<AdvancedItemController>();
        if (advancedItem != null)
        {
            MoveFileToRegion(advancedItem.FileName, "txt");
            _manager?.RegisterItemInRegion(advancedItem.FileName, "txt");
            if (!_advancedItemsInRegion.Contains(advancedItem))
            {
                _advancedItemsInRegion.Add(advancedItem);
            }
            return;
        }
        
        BasicItem basicItem = obj.GetComponent<BasicItem>();
        if (basicItem != null)
        {
            MoveFileToRegion(basicItem.FileName, basicItem.Fileclass);
            if (!_basicItemsInRegion.Contains(basicItem))
            {
                _basicItemsInRegion.Add(basicItem);
            }
        }
    }

    void MoveObjectFileToParent(GameObject obj)
    {
        AdvancedItemController advancedItem = obj.GetComponent<AdvancedItemController>();
        if (advancedItem != null)
        {
            MoveFileToParent(advancedItem.FileName, "txt");
            _manager?.UnregisterItemInRegion(advancedItem.FileName, "txt");
            _advancedItemsInRegion.Remove(advancedItem);
            return;
        }
        
        BasicItem basicItem = obj.GetComponent<BasicItem>();
        if (basicItem != null)
        {
            MoveFileToParent(basicItem.FileName, basicItem.Fileclass);
            _basicItemsInRegion.Remove(basicItem);
        }
    }

    void ScanRegion()
    {
        ReadGravityConfig();
        ApplyGravityToRigidbodies();

        ScanFilesInRegion();
        CheckObjectsInRegion();
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
        // foreach (var basicItem in _basicItemsInRegion.ToList())
        // {
        //     if (basicItem != null)
        //     {
        //         basicItem.UpdateFromFile(_regionFolderPath);
        //     }
        //     else
        //     {
        //         _basicItemsInRegion.Remove(basicItem);
        //     }
        // }

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

    void CheckObjectsInRegion()
    {
        if (_childCollider == null || _manager == null) return;
        
        var allItems = _manager.GetTrackableItems();
        
        foreach (var item in allItems)
        {
            if (item == null) continue;
            
            GameObject obj = item.gameObject;
            Vector3 currentPos = obj.transform.position;
            
            bool positionChanged = !_objectPositions.ContainsKey(obj) || 
                                   Vector3.Distance(_objectPositions[obj], currentPos) > 0.01f;
            
            if (!positionChanged) continue;
            
            _objectPositions[obj] = currentPos;
            
            bool isInBounds = IsPointInRegion(currentPos);
            bool isRegistered = _manager.IsObjectInRegion(obj, out _);
            
            Debug.Log($"[FileRegionManager] CheckObjectsInRegion: {obj.name}, pos={currentPos}, isInBounds={isInBounds}, isRegistered={isRegistered}");
            
            if (isInBounds && !isRegistered)
            {
                _objectsInRegion.Add(obj);
                _manager.RegisterRegionObject(obj, this);
                
                // 移动文件到区域文件夹
                MoveObjectFileToRegion(obj);
                
                Debug.Log($"[FileRegionManager] 自动注册区域内物体：{obj.name}");
            }
            else if (!isInBounds && isRegistered)
            {
                _objectsInRegion.Remove(obj);
                _manager.UnregisterRegionObject(obj);
                
                // 移动文件回父文件夹
                MoveObjectFileToParent(obj);
                
                Debug.Log($"[FileRegionManager] 自动注销区域外物体：{obj.name}");
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

    public string GetRegionFolderName()
    {
        return regionFolderName;
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
        if (!Application.isPlaying)
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
            
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
    }

}
