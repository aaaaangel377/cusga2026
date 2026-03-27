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
    private bool _isReady = false;

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

        // 关键：检查 fileName 是否为空
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("GravityController: fileName 为空，请检查 Inspector");
            return;
        }

        Debug.Log($"GravityController: 文件夹路径 = {_folderPath}");
        Debug.Log($"GravityController: 文件名 = {fileName}");
        Debug.Log($"GravityController: 完整路径 = {Path.Combine(_folderPath, $"{fileName}.txt")}");

        _gravityMagnitude = Physics2D.gravity.magnitude;

        // 参考成功脚本：先确保目录存在
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
            Debug.Log($"GravityController: 创建目录 {_folderPath}");
        }

        CreateDefaultFile();
        CheckFile();
        _isReady = true;
    }

    void Update()
    {
        if (!_isReady) return;

        _timer += Time.deltaTime;
        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            CheckFile();
        }
    }

    void CreateDefaultFile()
    {
        if (string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(fileName))
        {
            Debug.LogError($"GravityController: 无法创建文件 - folderPath={_folderPath}, fileName={fileName}");
            return;
        }

        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");

        // 参考成功脚本：如果文件存在，直接返回
        if (File.Exists(fullPath))
        {
            Debug.Log($"GravityController: 文件已存在 {fullPath}");
            // 读取现有内容
            try
            {
                _lastContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8).Trim();
                Debug.Log($"GravityController: 现有文件内容 = '{_lastContent}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GravityController: 读取现有文件失败 - {e.Message}");
            }
            return;
        }

        // 文件不存在，创建新文件
        try
        {
            // 参考成功脚本：直接写入，不做复杂验证
            File.WriteAllText(fullPath, "下", System.Text.Encoding.UTF8);
            _lastContent = "下";
            Debug.Log($"GravityController: 创建文件成功 {fullPath}");

            // 简单验证（可选）
            if (File.Exists(fullPath))
            {
                Debug.Log($"GravityController: 文件创建验证成功");
            }
            else
            {
                Debug.LogError($"GravityController: 文件创建后不存在，可能是权限问题");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GravityController: 创建文件失败 - {e.Message}");
        }
        Debug.Log($"GravityController: 已创建默认文件 {fullPath}");
    }

    void CheckFile()
    {
        if (string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(fileName)) return;

        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");

        try
        {
            if (!File.Exists(fullPath)) return;

            string content = File.ReadAllText(fullPath, System.Text.Encoding.UTF8).Trim();
            if (content != _lastContent)
            {
                Debug.Log($"GravityController: 重力变化 [{_lastContent} -> {content}]");
                _lastContent = content;
                ApplyGravity(content);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GravityController: 读取文件失败 - {e.Message}");
        }
    }

    void ApplyGravity(string direction)
    {
        Vector2 newGravity;
        switch (direction)
        {
            case "上": newGravity = new Vector2(0, _gravityMagnitude); break;
            case "下": newGravity = new Vector2(0, -_gravityMagnitude); break;
            case "左": newGravity = new Vector2(-16*_gravityMagnitude, 0); break;
            case "右": newGravity = new Vector2(16*_gravityMagnitude, 0); break;
            default:
                Debug.LogWarning($"GravityController: 未知方向 '{direction}'");
                return;
        }
        Physics2D.gravity = newGravity;
        Debug.Log($"GravityController: 重力已应用为 {direction} -> {Physics2D.gravity}");
    }
}