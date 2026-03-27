//using UnityEngine;
//using System.IO;
//using System.Text.RegularExpressions;
//using System.Collections.Generic;
//using System.Linq;

//public class CustomSpawner : MonoBehaviour
//{
//    [Header("文件设置")]
//    [SerializeField] private string baseFileName = "file";
//    [SerializeField] private string fileExtension = ".cop";

//    [Header("物理设置")]
//    [SerializeField] private bool addRigidbody2D = true;
//    [SerializeField] private float mass = 1f;
//    [SerializeField] private float gravityScale = 1f;
//    [SerializeField] private float drag = 0f;

//    private string _folderPath;
//    private float _checkInterval = 0.5f;
//    private float _timer = 0f;
//    private HashSet<string> _existingFiles = new HashSet<string>();
//    private LevelFileManager _manager;
//    private Dictionary<string, GameObject> _fileToObject = new Dictionary<string, GameObject>();
//    private string _originalFileName;
//    private bool _isCopySpawner = false;
//    private bool _isInitialized = false;

//    void Start()
//    {
//        if (_isInitialized) return;

//        LevelFileManager manager = FindObjectOfType<LevelFileManager>();
//        if (manager != null)
//        {
//            _manager = manager;
//            _folderPath = manager.GetFolderPath();
//            _checkInterval = manager.GetCheckInterval();
//        }
//        else
//        {
//            string gameRoot = Directory.GetParent(Application.dataPath).FullName;
//            _folderPath = Path.Combine(gameRoot, "level", "default");
//        }

//        if (!Directory.Exists(_folderPath))
//        {
//            Directory.CreateDirectory(_folderPath);
//        }

//        _originalFileName = $"{baseFileName}{fileExtension}";
//        _fileToObject[_originalFileName] = gameObject;

//        CreateDefaultFile();
//    }

//    void Update()
//    {
//        _timer += Time.deltaTime;
//        if (_timer >= _checkInterval)
//        {
//            _timer = 0f;
//            ScanFiles();
//        }
//    }

//    void ScanFiles()
//    {
//        if (_manager != null) return;

//        string[] allFiles = Directory.Exists(_folderPath)
//            ? Directory.GetFiles(_folderPath, $"*{fileExtension}")
//            : new string[0];

//        var currentFiles = new HashSet<string>();

//        foreach (string file in allFiles)
//        {
//            string fileName = Path.GetFileName(file);
//            currentFiles.Add(fileName);

//            if (IsCopyFile(fileName, out int copyNumber))
//            {
//                if (!_existingFiles.Contains(fileName))
//                {
//                    SpawnCopy(copyNumber, fileName);
//                }
//            }
//            else if (fileName == _originalFileName && !_existingFiles.Contains(fileName))
//            {
//                if (_fileToObject.ContainsKey(fileName) && _fileToObject[fileName] != null)
//                {
//                    _fileToObject[fileName].SetActive(true);
//                }
//            }
//        }

//        foreach (var kvp in _fileToObject.ToList())
//        {
//            if (kvp.Value == null)
//            {
//                _fileToObject.Remove(kvp.Key);
//                continue;
//            }

//            if (currentFiles.Contains(kvp.Key))
//            {
//                kvp.Value.SetActive(true);
//            }
//            else
//            {
//                kvp.Value.SetActive(false);

//                if (IsCopyFile(kvp.Key, out _))
//                {
//                    Destroy(kvp.Value);
//                    _fileToObject.Remove(kvp.Key);
//                }
//            }
//        }

//        _existingFiles = currentFiles;
//    }

//    bool IsCopyFile(string fileName, out int copyNumber)
//    {
//        copyNumber = 0;

//        if (!fileName.EndsWith(fileExtension))
//            return false;

//        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

//        string pattern = $@"^{Regex.Escape(baseFileName)} - 副本( \((\d+)\))?$";
//        Match match = Regex.Match(nameWithoutExt, pattern);

//        if (match.Success)
//        {
//            if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out int num))
//            {
//                copyNumber = num;
//            }
//            else
//            {
//                copyNumber = 1;
//            }
//            return true;
//        }

//        return false;
//    }

//    public void SpawnCopyFromFile(string fileName)
//    {
//        if (_isCopySpawner) return;
//        if (_isInitialized) return;

//        int copyNumber = 1;
//        string numberStr = "";

//        Match match = Regex.Match(fileName, @" - 副本( \((\d+)\))?\.cop$");
//        if (match.Success && match.Groups[2].Success)
//        {
//            int.TryParse(match.Groups[2].Value, out copyNumber);
//            if (copyNumber > 1)
//            {
//                numberStr = $" ({copyNumber})";
//            }
//        }

//        SpawnCopy(copyNumber, fileName);
//    }

//    void SpawnCopy(int copyNumber, string fileName)
//    {
//        GameObject newObj = Instantiate(gameObject, transform.parent);

//        string numberStr = copyNumber > 1 ? $" ({copyNumber})" : "";
//        newObj.name = $"{baseFileName} - 副本{numberStr}";

//        if (addRigidbody2D)
//        {
//            Rigidbody2D rb = newObj.AddComponent<Rigidbody2D>();
//            rb.mass = mass;
//            rb.gravityScale = gravityScale;
//            rb.drag = drag;
//        }

//        CustomSpawner newSpawner = newObj.GetComponent<CustomSpawner>();
//        if (newSpawner != null)
//        {
//            newSpawner.InitializeForCopy(fileName, baseFileName, _manager, _folderPath, _checkInterval);
//        }

//        if (_manager != null)
//        {
//            AdvancedItemController controller = gameObject.GetComponent<AdvancedItemController>();
//            if (controller != null)
//            {
//                AdvancedItemController newController = newObj.GetComponent<AdvancedItemController>();
//                if (newController == null)
//                {
//                    newController = newObj.AddComponent<AdvancedItemController>();
//                }
//                newController.SetManager(_manager);
//                _manager.RegisterAdvancedItem(newController);
//            }

//            _manager.RegisterCustomSpawner(newSpawner);
//        }

//        _fileToObject[fileName] = newObj;
//        Debug.Log($"CustomSpawner: 生成复制品 {newObj.name}");
//    }

//    public void SetBaseFileName(string name)
//    {
//        baseFileName = name;
//    }

//    public string GetBaseFileName()
//    {
//        return baseFileName;
//    }

//    public void SetFileExtension(string ext)
//    {
//        fileExtension = ext;
//    }

//    public string GetOriginalFileName()
//    {
//        return _originalFileName;
//    }

//    public bool IsCopySpawner()
//    {
//        return _isCopySpawner;
//    }

//    public void SetManager(LevelFileManager manager)
//    {
//        _manager = manager;
//        _folderPath = manager.GetFolderPath();
//        _checkInterval = manager.GetCheckInterval();
//    }

//    public void SetSpawnerActive(bool active)
//    {
//        gameObject.SetActive(active);
//    }

//    public void SetFileActive(bool active)
//    {
//        if (_fileToObject == null)
//        {
//            _fileToObject = new Dictionary<string, GameObject>();
//        }

//        if (!string.IsNullOrEmpty(_originalFileName) && _fileToObject.ContainsKey(_originalFileName) && _fileToObject[_originalFileName] != null)
//        {
//            _fileToObject[_originalFileName].SetActive(active);
//        }
//        else
//        {
//            gameObject.SetActive(active);
//        }
//    }

//    public void InitializeForCopy(string fileName, string originalBaseName, LevelFileManager manager, string folderPath, float checkInterval)
//    {
//        _manager = manager;
//        _folderPath = folderPath;
//        _checkInterval = checkInterval;
//        _originalFileName = fileName;
//        baseFileName = originalBaseName;
//        _fileToObject = new Dictionary<string, GameObject>();
//        _fileToObject[fileName] = gameObject;
//        _isCopySpawner = true;
//        _isInitialized = true;
//    }

//    void CreateDefaultFile()
//    {
//        string fullPath = Path.Combine(_folderPath, $"{baseFileName}{fileExtension}");
//        if (!File.Exists(fullPath))
//        {
//            File.WriteAllText(fullPath, string.Empty);
//        }
//    }
//}
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class CustomSpawner : MonoBehaviour
{
    [Header("文件设置")]
    [SerializeField] private string baseFileName = "file";
    [SerializeField] private string fileExtension = ".cop";

    [Header("物理设置")]
    [SerializeField] private bool addRigidbody2D = true;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float drag = 0f;

    [Header("生成设置")]
    [SerializeField] private bool addRandomOffset = false;
    [SerializeField] private float maxOffsetDistance = 0.5f;
    [SerializeField] private bool useInitialPosition = true;  // 使用初始位置而非当前位置

    private string _folderPath;
    private float _checkInterval = 0.5f;
    private float _timer = 0f;
    private HashSet<string> _existingFiles = new HashSet<string>();
    private LevelFileManager _manager;
    private Dictionary<string, GameObject> _fileToObject = new Dictionary<string, GameObject>();
    private string _originalFileName;
    private bool _isCopySpawner = false;
    private bool _isInitialized = false;
    private Vector3 _initialPosition;  // 保存初始位置

    void Awake()
    {
        // 保存初始位置
        _initialPosition = transform.position;
    }

    void Start()
    {
        if (_isInitialized) return;

        LevelFileManager manager = FindObjectOfType<LevelFileManager>();
        if (manager != null)
        {
            _manager = manager;
            _folderPath = manager.GetFolderPath();
            _checkInterval = manager.GetCheckInterval();
        }
        else
        {
            string gameRoot = Directory.GetParent(Application.dataPath).FullName;
            _folderPath = Path.Combine(gameRoot, "level", "default");
        }

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        _originalFileName = $"{baseFileName}{fileExtension}";
        _fileToObject[_originalFileName] = gameObject;

        CreateDefaultFile();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            ScanFiles();
        }
    }

    void ScanFiles()
    {
        if (_manager != null) return;

        string[] allFiles = Directory.Exists(_folderPath)
            ? Directory.GetFiles(_folderPath, $"*{fileExtension}")
            : new string[0];

        var currentFiles = new HashSet<string>();

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            currentFiles.Add(fileName);

            if (IsCopyFile(fileName, out int copyNumber))
            {
                if (!_existingFiles.Contains(fileName))
                {
                    SpawnCopy(copyNumber, fileName);
                }
            }
            else if (fileName == _originalFileName && !_existingFiles.Contains(fileName))
            {
                if (_fileToObject.ContainsKey(fileName) && _fileToObject[fileName] != null)
                {
                    _fileToObject[fileName].SetActive(true);
                }
            }
        }

        foreach (var kvp in _fileToObject.ToList())
        {
            if (kvp.Value == null)
            {
                _fileToObject.Remove(kvp.Key);
                continue;
            }

            if (currentFiles.Contains(kvp.Key))
            {
                kvp.Value.SetActive(true);
            }
            else
            {
                kvp.Value.SetActive(false);

                if (IsCopyFile(kvp.Key, out _))
                {
                    Destroy(kvp.Value);
                    _fileToObject.Remove(kvp.Key);
                }
            }
        }

        _existingFiles = currentFiles;
    }

    bool IsCopyFile(string fileName, out int copyNumber)
    {
        copyNumber = 0;

        if (!fileName.EndsWith(fileExtension))
            return false;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        string pattern = $@"^{Regex.Escape(baseFileName)} - 副本( \((\d+)\))?$";
        Match match = Regex.Match(nameWithoutExt, pattern);

        if (match.Success)
        {
            if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out int num))
            {
                copyNumber = num;
            }
            else
            {
                copyNumber = 1;
            }
            return true;
        }

        return false;
    }

    public void SpawnCopyFromFile(string fileName)
    {
        if (_isCopySpawner) return;
        if (_isInitialized) return;

        int copyNumber = 1;
        string numberStr = "";

        Match match = Regex.Match(fileName, @" - 副本( \((\d+)\))?\.cop$");
        if (match.Success && match.Groups[2].Success)
        {
            int.TryParse(match.Groups[2].Value, out copyNumber);
            if (copyNumber > 1)
            {
                numberStr = $" ({copyNumber})";
            }
        }

        SpawnCopy(copyNumber, fileName);
    }

    void SpawnCopy(int copyNumber, string fileName)
    {
        // 确定生成位置
        Vector3 spawnPosition;

        if (useInitialPosition)
        {
            // 使用初始位置
            spawnPosition = _initialPosition;
        }
        else
        {
            // 使用当前位置
            spawnPosition = transform.position;
        }

        // 可选：添加随机偏移避免完全重叠
        if (addRandomOffset)
        {
            spawnPosition += new Vector3(
                Random.Range(-maxOffsetDistance, maxOffsetDistance),
                Random.Range(-maxOffsetDistance, maxOffsetDistance),
                0
            );
        }

        // 生成新物品
        GameObject newObj = Instantiate(gameObject, spawnPosition, transform.rotation, transform.parent);

        string numberStr = copyNumber > 1 ? $" ({copyNumber})" : "";
        newObj.name = $"{baseFileName} - 副本{numberStr}";

        if (addRigidbody2D)
        {
            Rigidbody2D rb = newObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = newObj.AddComponent<Rigidbody2D>();
            }
            rb.mass = mass;
            rb.gravityScale = gravityScale;
            rb.drag = drag;
        }

        CustomSpawner newSpawner = newObj.GetComponent<CustomSpawner>();
        if (newSpawner != null)
        {
            newSpawner.InitializeForCopy(fileName, baseFileName, _manager, _folderPath, _checkInterval);
        }

        if (_manager != null)
        {
            AdvancedItemController controller = gameObject.GetComponent<AdvancedItemController>();
            if (controller != null)
            {
                AdvancedItemController newController = newObj.GetComponent<AdvancedItemController>();
                if (newController == null)
                {
                    newController = newObj.AddComponent<AdvancedItemController>();
                }
                newController.SetManager(_manager);
                _manager.RegisterAdvancedItem(newController);
            }

            _manager.RegisterCustomSpawner(newSpawner);
        }

        _fileToObject[fileName] = newObj;
        Debug.Log($"CustomSpawner: 生成复制品 {newObj.name} 在位置 {spawnPosition}");
    }

    public void SetBaseFileName(string name)
    {
        baseFileName = name;
    }

    public string GetBaseFileName()
    {
        return baseFileName;
    }

    public void SetFileExtension(string ext)
    {
        fileExtension = ext;
    }

    public string GetOriginalFileName()
    {
        return _originalFileName;
    }

    public bool IsCopySpawner()
    {
        return _isCopySpawner;
    }

    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
        _folderPath = manager.GetFolderPath();
        _checkInterval = manager.GetCheckInterval();
    }

    public void SetSpawnerActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void SetFileActive(bool active)
    {
        if (_fileToObject == null)
        {
            _fileToObject = new Dictionary<string, GameObject>();
        }

        if (!string.IsNullOrEmpty(_originalFileName) && _fileToObject.ContainsKey(_originalFileName) && _fileToObject[_originalFileName] != null)
        {
            _fileToObject[_originalFileName].SetActive(active);
        }
        else
        {
            gameObject.SetActive(active);
        }
    }

    public void InitializeForCopy(string fileName, string originalBaseName, LevelFileManager manager, string folderPath, float checkInterval)
    {
        _manager = manager;
        _folderPath = folderPath;
        _checkInterval = checkInterval;
        _originalFileName = fileName;
        baseFileName = originalBaseName;
        _fileToObject = new Dictionary<string, GameObject>();
        _fileToObject[fileName] = gameObject;
        _isCopySpawner = true;
        _isInitialized = true;

        // 为副本保存初始位置
        _initialPosition = transform.position;
    }

    void CreateDefaultFile()
    {
        string fullPath = Path.Combine(_folderPath, $"{baseFileName}{fileExtension}");
        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, string.Empty);
        }
    }

    // 公共方法：重置初始位置（如果需要动态更新）
    public void ResetInitialPosition()
    {
        _initialPosition = transform.position;
    }

    // 公共方法：获取初始位置
    public Vector3 GetInitialPosition()
    {
        return _initialPosition;
    }
}