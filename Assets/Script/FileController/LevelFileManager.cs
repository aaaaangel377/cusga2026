using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class LevelFileManager : MonoBehaviour
{
    public static bool shouldResetFiles = false;

    [SerializeField] private int levelIndex = 1;

    [SerializeField] private float checkInterval = 0.5f;

    [SerializeField] private bool autoCreateFiles = true;

    private string _folderPath;
    private float _timer = 0f;
    [SerializeField] private List<BasicItem> basicItems = new List<BasicItem>();
    private List<AdvancedItemController> advancedItems = new List<AdvancedItemController>();
    private List<CollisionImageItem> collisionImageItems = new List<CollisionImageItem>();
    private HashSet<string> _existingFiles = new HashSet<string>();

    void Awake()
    {
        string gameRoot = Directory.GetParent(Application.dataPath).FullName;
        _folderPath = Path.Combine(gameRoot, "level", levelIndex.ToString());

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        basicItems = FindObjectsOfType<BasicItem>().ToList();
        advancedItems = FindObjectsOfType<AdvancedItemController>().ToList();
        collisionImageItems = FindObjectsOfType<CollisionImageItem>().ToList();

        foreach (var item in basicItems)
        {
            item.SetManager(this);
        }

        foreach (var item in advancedItems)
        {
            item.SetManager(this);
        }

        foreach (var item in collisionImageItems)
        {
            item.SetManager(this);
        }
    }

    void Start()
    {
        if (shouldResetFiles)
        {
            DeleteLevelFiles();
            shouldResetFiles = false;
        }

        if (autoCreateFiles)
        {
            CreateDefaultFiles();
        }

        foreach (var item in collisionImageItems)
        {
            item.Initialize();
        }

        ScanFiles();
    }

    void Update()
    {
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
        
        string[] txtFiles = Directory.GetFiles(_folderPath, "*.txt");
        foreach (string file in txtFiles)
        {
            File.Delete(file);
        }
        
        string[] pngFiles = Directory.GetFiles(_folderPath, "*.png");
        foreach (string file in pngFiles)
        {
            File.Delete(file);
        }
    }

    void CreateDefaultFiles()
    {
        foreach (var item in basicItems)
        {
            item.CreateDefaultFile(_folderPath);
        }

        foreach (var item in advancedItems)
        {
            item.CreateDefaultFile(_folderPath);
        }
    }

    void ScanFiles()
    {
        string[] allFiles = Directory.Exists(_folderPath)
            ? Directory.GetFiles(_folderPath, "*.txt")
            : new string[0];

        var currentFiles = new HashSet<string>();

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            currentFiles.Add(fileName);

            if (fileName.Contains(" - 副本"))
            {
                if (!_existingFiles.Contains(fileName))
                {
                    string baseName = fileName.Replace(" - 副本.txt", "");

                    SpawnItem[] spawners = FindObjectsOfType<SpawnItem>();
                    foreach (var spawner in spawners)
                    {
                        if (spawner.FileName == baseName)
                        {
                            string content = File.ReadAllText(file);
                            spawner.OnFileCopied(fileName, content);
                            break;
                        }
                    }

                    AdvancedItemController[] advancedControllers = FindObjectsOfType<AdvancedItemController>();
                    foreach (var controller in advancedControllers)
                    {
                        if (controller.FileName == baseName)
                        {
                            string content = File.ReadAllText(file);
                            controller.OnFileCopied(fileName, content);
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
            }
        }

        _existingFiles = currentFiles;

        foreach (var item in basicItems)
        {
            item.UpdateFromFile(_folderPath);
        }

        foreach (var item in advancedItems)
        {
            item.UpdateFromFile(_folderPath);
        }

        foreach (var item in collisionImageItems)
        {
            item.CheckImageFile(_folderPath);
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

    public void RegisterCollisionImage(CollisionImageItem item)
    {
        if (!collisionImageItems.Contains(item))
        {
            collisionImageItems.Add(item);
            item.SetManager(this);
            item.Initialize();
        }
    }

    public int GetLevelIndex()
    {
        return levelIndex;
    }
    
    public string GetFolderPath()
    {
        return _folderPath;
    }
    
    public float GetCheckInterval()
    {
        return checkInterval;
    }
}
