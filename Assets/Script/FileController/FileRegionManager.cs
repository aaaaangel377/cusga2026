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

    [Header("预设对象")]
    [SerializeField] private AdvancedItemController[] presetItems;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;

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
    private List<AdvancedItemController> _advancedItemsInRegion = new List<AdvancedItemController>();
    private HashSet<GameObject> _objectsInRegion = new HashSet<GameObject>();
    private Dictionary<GameObject, Vector3> _objectPositions = new Dictionary<GameObject, Vector3>();
    
    // 新增：记录由本区域管理的物体及其初始状态
    private Dictionary<GameObject, string> _objectInitialContents = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, Vector3> _objectInitialPositions = new Dictionary<GameObject, Vector3>();
    private HashSet<GameObject> _managedObjects = new HashSet<GameObject>();
    
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
            _originalGravityScales.Remove(rb);
            Debug.Log($"[FileRegionManager] 物体离开区域：{other.gameObject.name}, 列表数={_rigidbodiesInRegion.Count}");
        }
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
            
            ReadGravityConfig();

            InitializePresetItems();

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

    void InitializePresetItems()
    {
        if (presetItems == null || presetItems.Length == 0)
        {
            Debug.Log($"[FileRegionManager] 预设对象数组为空或长度为 0，跳过初始化");
            return;
        }
        
        foreach (var item in presetItems)
        {
            if (item == null) continue;
            
            GameObject obj = item.gameObject;
            string fileName = item.FileName;
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning($"[FileRegionManager] 预设对象 {obj.name} 的文件名为空，跳过");
                continue;
            }
            
            string regionFilePath = Path.Combine(_regionFolderPath, $"{fileName}.txt");
            string parentFilePath = Path.Combine(_parentFolderPath, $"{fileName}.txt");
            
            string content = !string.IsNullOrEmpty(item.CustomContent) ? item.CustomContent : "0\n0";
            
            try
            {
                if (File.Exists(parentFilePath))
                {
                    File.Delete(parentFilePath);
                }
                File.WriteAllText(regionFilePath, content);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 创建预设对象区域文件失败：{e.Message}");
                continue;
            }
            
            item.SetManager(_manager);
            
            _objectInitialContents[obj] = content;
            _objectInitialPositions[obj] = obj.transform.position;
            
            obj.transform.position = transform.position;
            
            _managedObjects.Add(obj);
            
            _manager?.RegisterItemInRegion(fileName, "txt");
            if (!_advancedItemsInRegion.Contains(item))
            {
                _advancedItemsInRegion.Add(item);
            }
            
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (!_originalGravityScales.ContainsKey(rb))
                {
                    _originalGravityScales[rb] = rb.gravityScale;
                }
                if (!_rigidbodiesInRegion.Contains(rb))
                {
                    _rigidbodiesInRegion.Add(rb);
                }
            }
            
            Debug.Log($"[FileRegionManager] 预设对象初始化完成：{obj.name}");
        }
    }

    void ProcessObjectEnter(GameObject obj)
    {
        AdvancedItemController advancedItem = obj.GetComponent<AdvancedItemController>();
        if (advancedItem == null) return;
        
        string fileName = advancedItem.FileName;
        string regionFilePath = Path.Combine(_regionFolderPath, $"{fileName}.txt");
        string parentFilePath = Path.Combine(_parentFolderPath, $"{fileName}.txt");
        
        if (!_objectInitialContents.ContainsKey(obj))
        {
            try
            {
                if (File.Exists(parentFilePath))
                {
                    _objectInitialContents[obj] = File.ReadAllText(parentFilePath);
                }
                else if (File.Exists(regionFilePath))
                {
                    _objectInitialContents[obj] = File.ReadAllText(regionFilePath);
                }
                else
                {
                    _objectInitialContents[obj] = advancedItem.CustomContent ?? "";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FileRegionManager] 读取初始文件内容失败：{e.Message}");
                _objectInitialContents[obj] = advancedItem.CustomContent ?? "";
            }
            
            _objectInitialPositions[obj] = obj.transform.position;
        }
        
        string newContent = "0\n0";
        try
        {
            if (File.Exists(parentFilePath))
            {
                File.WriteAllText(parentFilePath, newContent);
            }
            else if (File.Exists(regionFilePath))
            {
                File.WriteAllText(regionFilePath, newContent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FileRegionManager] 修改文件内容失败：{e.Message}");
        }
        
        obj.transform.position = transform.position;
        
        _managedObjects.Add(obj);
        
        _manager?.RegisterItemInRegion(fileName, "txt");
        if (!_advancedItemsInRegion.Contains(advancedItem))
        {
            _advancedItemsInRegion.Add(advancedItem);
        }
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (!_originalGravityScales.ContainsKey(rb))
            {
                _originalGravityScales[rb] = rb.gravityScale;
            }
            if (!_rigidbodiesInRegion.Contains(rb))
            {
                _rigidbodiesInRegion.Add(rb);
            }
        }
    }

    void ProcessObjectExit(GameObject obj)
    {
        AdvancedItemController advancedItem = obj.GetComponent<AdvancedItemController>();
        if (advancedItem == null) return;
        
        string fileName = advancedItem.FileName;
        string parentFilePath = Path.Combine(_parentFolderPath, $"{fileName}.txt");
        
        if (_objectInitialContents.ContainsKey(obj))
        {
            string initialContent = _objectInitialContents[obj];
            
            if (File.Exists(parentFilePath))
            {
                File.WriteAllText(parentFilePath, initialContent);
            }
            
            _objectInitialContents.Remove(obj);
        }
        
        if (_objectInitialPositions.ContainsKey(obj))
        {
            obj.transform.position = _objectInitialPositions[obj];
            _objectInitialPositions.Remove(obj);
        }
        
        _managedObjects.Remove(obj);
        
        _manager?.UnregisterItemInRegion(fileName, "txt");
        _advancedItemsInRegion.Remove(advancedItem);
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            _rigidbodiesInRegion.Remove(rb);
            if (_originalGravityScales.ContainsKey(rb))
            {
                _originalGravityScales.Remove(rb);
            }
        }
        
        Debug.Log($"[FileRegionManager] 物体离开区域完成：{obj.name}");
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

    public bool IsObjectInRegion(GameObject obj) => _objectsInRegion.Contains(obj);

    void MoveFileToRegion(string fileName, string fileClass)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        if (string.IsNullOrEmpty(_parentFolderPath))
        {
            Debug.LogError($"[FileRegionManager] MoveFileToRegion: _parentFolderPath 为空！");
            return;
        }
        if (string.IsNullOrEmpty(_regionFolderPath))
        {
            Debug.LogError($"[FileRegionManager] MoveFileToRegion: _regionFolderPath 为空！");
            return;
        }

        string sourcePath = Path.Combine(_parentFolderPath, $"{fileName}.{fileClass}");
        string destPath = Path.Combine(_regionFolderPath, $"{fileName}.{fileClass}");

        Debug.Log($"[FileRegionManager] MoveFileToRegion: 尝试移动 {fileName}.{fileClass}");
        Debug.Log($"[FileRegionManager] 源路径：{sourcePath}");
        Debug.Log($"[FileRegionManager] 目标路径：{destPath}");
        Debug.Log($"[FileRegionManager] 源文件存在：{File.Exists(sourcePath)}");
        Debug.Log($"[FileRegionManager] 目标文件存在：{File.Exists(destPath)}");

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"[FileRegionManager] 源文件不存在，无法移动：{sourcePath}");
            if (File.Exists(destPath))
            {
                Debug.Log($"[FileRegionManager] 但目标文件已存在，物体已在区域内：{destPath}");
                _filesInRegion.Add(fileName);
            }
            return;
        }

        if (File.Exists(destPath))
        {
            Debug.LogWarning($"[FileRegionManager] 目标文件已存在，删除后重新移动：{destPath}");
            try
            {
                File.Delete(destPath);
                Debug.Log($"[FileRegionManager] 已删除旧文件：{destPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 删除旧文件失败：{e.Message}");
                return;
            }
        }

        try
        {
            File.Move(sourcePath, destPath);
            _filesInRegion.Add(fileName);
            Debug.Log($"[FileRegionManager] 文件移入区域成功：{fileName}.{fileClass}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FileRegionManager] 移动文件失败：{e.Message}\n{e.StackTrace}");
        }
    }

    void MoveFileToParent(string fileName, string fileClass)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        if (string.IsNullOrEmpty(_regionFolderPath))
        {
            Debug.LogError($"[FileRegionManager] MoveFileToParent: _regionFolderPath 为空！");
            return;
        }
        if (string.IsNullOrEmpty(_parentFolderPath))
        {
            Debug.LogError($"[FileRegionManager] MoveFileToParent: _parentFolderPath 为空！");
            return;
        }

        string sourcePath = Path.Combine(_regionFolderPath, $"{fileName}.{fileClass}");
        string destPath = Path.Combine(_parentFolderPath, $"{fileName}.{fileClass}");

        Debug.Log($"[FileRegionManager] MoveFileToParent: 尝试移动 {fileName}.{fileClass}");
        Debug.Log($"[FileRegionManager] 源路径：{sourcePath}");
        Debug.Log($"[FileRegionManager] 目标路径：{destPath}");
        Debug.Log($"[FileRegionManager] 源文件存在：{File.Exists(sourcePath)}");
        Debug.Log($"[FileRegionManager] 目标文件存在：{File.Exists(destPath)}");

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"[FileRegionManager] 源文件不存在，无法移动：{sourcePath}");
            _filesInRegion.Remove(fileName);
            return;
        }

        if (File.Exists(destPath))
        {
            Debug.LogWarning($"[FileRegionManager] 目标文件已存在，删除后重新移动：{destPath}");
            try
            {
                File.Delete(destPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FileRegionManager] 删除旧文件失败：{e.Message}");
                return;
            }
        }

        try
        {
            File.Move(sourcePath, destPath);
            _filesInRegion.Remove(fileName);
            Debug.Log($"[FileRegionManager] 文件移出区域成功：{fileName}.{fileClass}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FileRegionManager] 移动文件失败：{e.Message}\n{e.StackTrace}");
        }
    }



    void ScanRegion()
    {
        ReadGravityConfig();
        ScanRigidbodiesInRegion();
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
        float magnitude = Physics2D.gravity.magnitude;
        switch (direction)
        {
            case "上":
                return new Vector2(0, 16 * magnitude);
            case "下":
                return new Vector2(0, -16 * magnitude);
            case "左":
                return new Vector2(-16 * magnitude, 0);
            case "右":
                return new Vector2(16 * magnitude, 0);
            default:
                Debug.LogWarning($"[FileRegionManager] 未知重力方向：'{direction}'，使用默认向下重力");
                return new Vector2(0, -16 * magnitude);
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
                if (rb != null && _originalGravityScales.ContainsKey(rb))
                {
                    _originalGravityScales.Remove(rb);
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
        
        // 不再自动检测物体进入/离开区域
        // 改为由 LevelFileManager 检测文件移动后通知
        
        // 更新已管理物体的位置记录（用于调试）
        foreach (var obj in _managedObjects.ToList())
        {
            if (obj == null)
            {
                _managedObjects.Remove(obj);
                continue;
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
    
    /// <summary>
    /// 检测是否有物体被拖入区域文件夹
    /// 由 LevelFileManager 调用
    /// </summary>
    public void CheckFileDraggedIn(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        
        // 查找对应的物体
        var allAdvancedItems = FindObjectsOfType<AdvancedItemController>();
        foreach (var advancedItem in allAdvancedItems)
        {
            if (advancedItem.FileName == fileName && !_managedObjects.Contains(advancedItem.gameObject))
            {
                Debug.Log($"[FileRegionManager] 检测到文件被拖入区域：{fileName}");
                ProcessObjectEnter(advancedItem.gameObject);
                break;
            }
        }
    }
    
    /// <summary>
    /// 检测是否有物体被拖出区域文件夹
    /// 由 LevelFileManager 调用
    /// </summary>
    public void CheckFileDraggedOut(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        
        var allAdvancedItems = FindObjectsOfType<AdvancedItemController>();
        foreach (var advancedItem in allAdvancedItems)
        {
            if (advancedItem.FileName == fileName && _managedObjects.Contains(advancedItem.gameObject))
            {
                Debug.Log($"[FileRegionManager] 检测到文件被拖出区域：{fileName}");
                ProcessObjectExit(advancedItem.gameObject);
                break;
            }
        }
    }
    
    /// <summary>
    /// 判断物体是否由本区域管理
    /// </summary>
    public bool IsObjectManaged(GameObject obj)
    {
        return _managedObjects.Contains(obj);
    }
    
    public bool IsPresetItem(AdvancedItemController item)
    {
        if (presetItems == null || presetItems.Length == 0) return false;
        
        foreach (var preset in presetItems)
        {
            if (preset == item) return true;
        }
        return false;
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
