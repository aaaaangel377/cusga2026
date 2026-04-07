using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class LevelFileManager: MonoBehaviour
{
    public static bool shouldResetFiles = false;

    [SerializeField] private string levelIndex = "level_name";

    [SerializeField] private float checkInterval = 0.5f;

    [SerializeField] private bool autoCreateFiles = true;

    private string _folderPath;
    private float _timer = 0f;
    private List<AdvancedItemController> advancedItems = new List<AdvancedItemController>();
    //private List<CollisionImageItem> collisionImageItems = new List<CollisionImageItem>();
    private List<ImageColliderFile> imageColliderFiles = new List<ImageColliderFile>();
    private List<CustomSpawner> customSpawners = new List<CustomSpawner>();
    private List<FileRegionManager> regionManagers = new List<FileRegionManager>();
    private Dictionary<string, string> _regionFolders = new Dictionary<string, string>();
    private HashSet<string> _existingFiles = new HashSet<string>();
    private HashSet<string> _existingCopFiles = new HashSet<string>();
    private HashSet<string> _itemsInRegions = new HashSet<string>();
    private Dictionary<GameObject, FileRegionManager> _objectsInRegions = new Dictionary<GameObject, FileRegionManager>();
    private bool _isInitialized = false;

    void Awake()
    {
        Physics2D.gravity = new Vector2(0, -9.81f);
        string gameRoot = Directory.GetParent(Application.dataPath).FullName;
        _folderPath = Path.Combine(gameRoot, "level", levelIndex.ToString());

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        advancedItems = FindObjectsOfType<AdvancedItemController>().ToList();
        imageColliderFiles = FindObjectsOfType<ImageColliderFile>().ToList();
        customSpawners = FindObjectsOfType<CustomSpawner>().ToList();
        regionManagers = FindObjectsOfType<FileRegionManager>().ToList();

        foreach (var item in advancedItems)
        {
            item.SetManager(this);
        }

        /*foreach (var item in collisionImageItems)
        {
            item.SetManager(this);
        }*/

        foreach (var item in imageColliderFiles)
        {
            item.SetManager(this);
        }

        foreach (var region in regionManagers)
        {
            if (region != null)
            {
                region.SetManager(this);
            }
        }

        DeleteLevelFiles();
    }

    void Start()
    {
        //if (shouldResetFiles)
        //{
        //    DeleteLevelFiles();
        //    shouldResetFiles = false;
        //}


        //ClearLevelFolder();

        if (autoCreateFiles)
        {
            CreateDefaultFiles();
        }

        /*foreach (var item in collisionImageItems)
        {
            item.Initialize();
        }*/

        foreach (var item in imageColliderFiles)
        {
            item.Initialize();
        }

        StartCoroutine(InitCustomSpawners());
    }
    
    System.Collections.IEnumerator InitCustomSpawners()
    {
        yield return new WaitForEndOfFrame();
        
        customSpawners = FindObjectsOfType<CustomSpawner>().ToList();
        foreach (var spawner in customSpawners)
        {
            spawner.SetManager(this);
            RegisterCustomSpawner(spawner);
        }
        
        ScanFiles();
        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized) return;
        
        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            ScanFiles();
        }
    }

    void DeleteLevelFiles()
    {
        if (!Directory.Exists(_folderPath)) return;
        
        SpawnItem.RemoveAllCopies();

        if (!Directory.Exists(_folderPath)) return;

        foreach (var kvp in _regionFolders)
        {
            if (Directory.Exists(kvp.Value))
            {
                foreach (string file in Directory.GetFiles(kvp.Value))
                {
                    File.Delete(file);
                }
            }
        }
        _regionFolders.Clear();

        foreach (string dir in Directory.GetDirectories(_folderPath))
        {
            Directory.Delete(dir, true);
        }

        foreach (string file in Directory.GetFiles(_folderPath))
        {
            File.Delete(file);
        }

        // 强制刷新文件夹（发送刷新通知给资源管理器）
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
    }
    
    /*void ClearLevelFolder()
    {
        if (!Directory.Exists(_folderPath)) return;
        
        SpawnItem.RemoveAllCopies();
        
        string[] allFiles = Directory.GetFiles(_folderPath);
        foreach (string file in allFiles)
        {
            File.Delete(file);
        }
    }*/


    public string GetParentFolderPath()
    {
        if (string.IsNullOrEmpty(_folderPath))
            return null;

        return Directory.GetParent(_folderPath).FullName;
    }
    void CreateDefaultFiles()
    {
        foreach (var item in advancedItems)
        {
            item.CreateDefaultFile(_folderPath);
        }
    }

    void ScanFiles()
    {
        // 只扫描顶层文件，不扫描子文件夹（区域文件夹）
        string[] allFiles = Directory.Exists(_folderPath)
            ? Directory.GetFiles(_folderPath, "*.*", SearchOption.TopDirectoryOnly)
            : new string[0];

        var currentFiles = new HashSet<string>();
        var currentCopFiles = new HashSet<string>();

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            
            if (fileName.EndsWith(".txt"))
            {
                currentFiles.Add(fileName);
            }
            else if (fileName.EndsWith(".cop"))
            {
                currentCopFiles.Add(fileName);
            }
        }

        ScanTxtFiles(currentFiles);
        ScanCopFiles(currentCopFiles);

        _existingFiles = currentFiles;
        _existingCopFiles = currentCopFiles;
    }

    void ScanTxtFiles(HashSet<string> currentFiles)
    {
        foreach (string fileName in currentFiles)
        {
            if (fileName.Contains(" - 副本") && fileName.EndsWith(".txt"))
            {
                if (!_existingFiles.Contains(fileName))
                {
                    string baseName = System.Text.RegularExpressions.Regex.Replace(fileName, @" - 副本( \(\d+\))?\.txt", "");

                    SpawnItem[] spawners = FindObjectsOfType<SpawnItem>();
                    foreach (var spawner in spawners)
                    {
                        if (spawner.FileName == baseName)
                        {
                            string fullPath = Path.Combine(_folderPath, fileName);
                            string content = File.ReadAllText(fullPath);
                            spawner.OnFileCopied(fileName, content);
                            break;
                        }
                    }

                    AdvancedItemController[] advancedControllers = FindObjectsOfType<AdvancedItemController>();
                    foreach (var controller in advancedControllers)
                    {
                        if (controller.FileName == baseName)
                        {
                            string fullPath = Path.Combine(_folderPath, fileName);
                            string content = File.ReadAllText(fullPath);
                            //controller.OnFileCopied(fileName, content);
                            break;
                        }
                    }
                }
            }
        }

        foreach (string oldFile in _existingFiles)
        {
            if (!currentFiles.Contains(oldFile))
            {
                string baseName = oldFile.Replace(".txt", "");

                CriticalItem[] criticalItems = FindObjectsOfType<CriticalItem>();
                foreach (var criticalItem in criticalItems)
                {
                    if (criticalItem.FileName == baseName)
                    {
                        ReloadScene();
                        return;
                    }
                }
                
                if (oldFile.Contains(" - 副本") && oldFile.EndsWith(".txt"))
                {
                    SpawnItem.RemoveCopyObject(oldFile);
                }
            }
        }

        foreach (var item in advancedItems)
        {
            if (item == null) continue;
            
            if (IsItemInRegion(item.FileName, "txt"))
            {
                continue;
            }
            item.UpdateFromFile(_folderPath);
        }

        /*foreach (var item in collisionImageItems)
        {
            item.CheckImageFile(_folderPath);
        }*/ 

        foreach (var item in imageColliderFiles)
        {
            item.CheckImageFile(_folderPath);
        }
    }

    void ScanCopFiles(HashSet<string> currentCopFiles)
    {
        var spawnersByBaseName = new Dictionary<string, List<CustomSpawner>>();
        
        foreach (var spawner in customSpawners.ToList())
        {
            if (spawner == null)
            {
                customSpawners.Remove(spawner);
                continue;
            }
            
            string baseName = spawner.GetBaseFileName();
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "file";
            }
            
            if (!spawnersByBaseName.ContainsKey(baseName))
            {
                spawnersByBaseName[baseName] = new List<CustomSpawner>();
            }
            spawnersByBaseName[baseName].Add(spawner);
        }
        
        foreach (var kvp in spawnersByBaseName)
        {
            string baseName = kvp.Key;
            var spawners = kvp.Value;
            
            string originalFileName = $"{baseName}.cop";
            bool originalFileExists = currentCopFiles.Contains(originalFileName);
            
            foreach (var spawner in spawners)
            {
                if (spawner == null)
                {
                    continue;
                }
                
                string spawnerFileName = spawner.GetOriginalFileName();
                
                if (string.IsNullOrEmpty(spawnerFileName))
                {
                    spawner.SetFileActive(false);
                    continue;
                }
                
                bool isCopySpawner = spawner.IsCopySpawner();
                
                if (isCopySpawner)
                {
                    if (currentCopFiles.Contains(spawnerFileName))
                    {
                        spawner.SetFileActive(true);
                    }
                    else
                    {
                        if (_existingCopFiles.Contains(spawnerFileName))
                        {
                            spawner.SetFileActive(false);
                        }
                        else
                        {
                            customSpawners.Remove(spawner);
                            Destroy(spawner.gameObject);
                        }
                    }
                }
                else
                {
                    if (currentCopFiles.Contains(spawnerFileName))
                    {
                        spawner.SetFileActive(true);
                    }
                    else
                    {
                        spawner.SetFileActive(false);
                    }
                }
            }
        }
        
        foreach (string fileName in currentCopFiles)
        {
            if (fileName.Contains(" - 副本") && !_existingCopFiles.Contains(fileName))
            {
                string baseName = System.Text.RegularExpressions.Regex.Replace(fileName, @" - 副本( \(\d+\))?\.cop", "");
                
                if (spawnersByBaseName.ContainsKey(baseName))
                {
                    foreach (var spawner in spawnersByBaseName[baseName])
                    {
                        //if (!spawner.IsCopySpawner())
                        {
                            spawner.SpawnCopyFromFile(fileName);
                            break;
                        }
                    }
                }
            }
        }
    }

    public void ReloadScene()
    {
        shouldResetFiles = true;
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
    
    public void RegisterAdvancedItem(AdvancedItemController item)
    {
        if (!advancedItems.Contains(item))
        {
            advancedItems.Add(item);
            item.SetManager(this);
        }
    }
    
    public void UnregisterAdvancedItem(AdvancedItemController item)
    {
        if (advancedItems.Contains(item))
        {
            advancedItems.Remove(item);
        }
    }

    /*public void RegisterCollisionImage(CollisionImageItem item)
    {
        if (!collisionImageItems.Contains(item))
        {
            collisionImageItems.Add(item);
            item.SetManager(this);
            item.Initialize();
        }
    }*/

    public void RegisterImageColliderFile(ImageColliderFile item)
    {
        if (!imageColliderFiles.Contains(item))
        {
            imageColliderFiles.Add(item);
            item.SetManager(this);
            item.Initialize();
        }
    }

    public void UnregisterImageColliderFile(ImageColliderFile item)
    {
        if (imageColliderFiles.Contains(item))
        {
            imageColliderFiles.Remove(item);
        }
    }
    
    public void RegisterCustomSpawner(CustomSpawner spawner)
    {
        if (!customSpawners.Contains(spawner))
        {
            customSpawners.Add(spawner);
        }
    }

    public string GetLevelIndex()
    {
        return levelIndex;
    }
    
    public string GetFolderPath()
    {
        return _folderPath;
    }
    
    public string CreateRegionFolder(string regionName)
    {
        if (_regionFolders.ContainsKey(regionName))
        {
            return _regionFolders[regionName];
        }
        
        string regionPath = Path.Combine(_folderPath, regionName);
        
        if (!Directory.Exists(regionPath))
        {
            Directory.CreateDirectory(regionPath);
            Debug.Log($"[LevelFileManager] 创建区域文件夹：{regionPath}");
        }
        
        _regionFolders[regionName] = regionPath;
        return regionPath;
    }
    
    public float GetCheckInterval()
    {
        return checkInterval;
    }
    
    public void RegisterItemInRegion(string fileName, string fileExtension = "txt")
    {
        _itemsInRegions.Add($"{fileName}.{fileExtension}");
    }
    
    public void UnregisterItemInRegion(string fileName, string fileExtension = "txt")
    {
        _itemsInRegions.Remove($"{fileName}.{fileExtension}");
    }
    
    public bool IsItemInRegion(string fileName, string fileExtension = "txt")
    {
        return _itemsInRegions.Contains($"{fileName}.{fileExtension}");
    }

    public void RegisterRegionObject(GameObject obj, FileRegionManager region)
    {
        if (!_objectsInRegions.ContainsKey(obj))
        {
            _objectsInRegions[obj] = region;
            Debug.Log($"[LevelFileManager] {obj.name} 进入区域 {region.GetRegionFolderName()}");
        }
    }

    public void UnregisterRegionObject(GameObject obj)
    {
        if (_objectsInRegions.ContainsKey(obj))
        {
            FileRegionManager region = _objectsInRegions[obj];
            Debug.Log($"[LevelFileManager] {obj.name} 离开区域 {region.GetRegionFolderName()}");
            _objectsInRegions.Remove(obj);
        }
    }

    public bool IsObjectInRegion(GameObject obj, out FileRegionManager region)
    {
        return _objectsInRegions.TryGetValue(obj, out region);
    }

    public FileRegionManager GetRegionForObject(GameObject obj)
    {
        FileRegionManager region;
        _objectsInRegions.TryGetValue(obj, out region);
        return region;
    }

    public List<MonoBehaviour> GetTrackableItems()
    {
        var items = new List<MonoBehaviour>();
        
        foreach (var item in advancedItems)
        {
            if (item != null) items.Add(item);
        }
        
        return items;
    }
    
    public List<AdvancedItemController> GetAdvancedItems()
    {
        var items = new List<AdvancedItemController>();
        
        foreach (var item in advancedItems)
        {
            if (item != null) items.Add(item);
        }
        
        return items;
    }
}
