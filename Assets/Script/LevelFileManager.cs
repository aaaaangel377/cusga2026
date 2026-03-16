using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class LevelFileManager : MonoBehaviour
{
    [SerializeField] private int levelIndex = 1;

    [SerializeField] private float checkInterval = 0.5f;

    [SerializeField] private bool autoCreateFiles = true;

    private string _folderPath;
    private float _timer = 0f;
    [SerializeField] private List<BasicItem> items = new List<BasicItem>();//场景中所有BasicItem组件的列表
    private HashSet<string> _existingFiles = new HashSet<string>();

    /*public string FolderPath => _folderPath;

    public bool FileExists(string fileName)
    {
        return File.Exists(Path.Combine(_folderPath, fileName));
    }

    public string ReadFile(string fileName)
    {
        return File.ReadAllText(Path.Combine(_folderPath, fileName));
    }*/

    void Awake()
    {
        string gameRoot = Directory.GetParent(Application.dataPath).FullName;//根目录
        _folderPath = Path.Combine(gameRoot, "level", levelIndex.ToString());//文件夹路径

        if (!Directory.Exists(_folderPath))//如果文件夹不存在则创建
        {
            Directory.CreateDirectory(_folderPath);
        }

        items = FindObjectsOfType<BasicItem>().ToList();//找到场景中所有BasicItem组件并存储到列表中

        foreach (var item in items)//为每个BasicItem组件设置管理器引用
        {
            item.SetManager(this);
        }
    }

    void Start()//创建文件
    {
        if (autoCreateFiles)
        {
            CreateDefaultFiles();
        }

        ScanFiles();
    }

    void Update()//检查文件
    {
        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            ScanFiles();
        }
    }

    void CreateDefaultFiles()//创建单独文件
    {
        foreach (var item in items)
        {
            item.CreateDefaultFile(_folderPath);
        }
    }

    void ScanFiles()//扫描文件并处理新增或删除的文件
    {
        //获取文件夹中所有txt文件的路径，如果文件夹不存在则返回空数组
        string[] allFiles = Directory.Exists(_folderPath)
            ? Directory.GetFiles(_folderPath, "*.txt")
            : new string[0];

        var currentFiles = new HashSet<string>();

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            currentFiles.Add(fileName);
            //如果文件名包含" - 副本"，且之前不存在这个文件，则认为是新复制的文件
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
                }
            }
        }
        //检查之前存在但现在不存在的文件，认为是被删除了
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

        foreach (var item in items)
        {
            item.UpdateFromFile(_folderPath);
        }
    }

    public void ReloadScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
}
