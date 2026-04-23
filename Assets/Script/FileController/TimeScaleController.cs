using UnityEngine;
using System.IO;

public class TimeScaleController : MonoBehaviour
{
    [SerializeField] private string fileName = "timescale.cfg";

    [Header("默认内容")]
    [SerializeField] private int defaultContent = 5; // 默认5对应0.5倍速

    [Header("映射范围")]
    [SerializeField] private int minFileValue = 0;   // 文件最小值
    [SerializeField] private int maxFileValue = 10;  // 文件最大值
    [SerializeField] private float minTimeScale = 0f; // 最小TimeScale
    [SerializeField] private float maxTimeScale = 1f; // 最大TimeScale

    private string _folderPath;
    private float _checkInterval = 0.5f;
    private int _lastContent = -1;
    private float _timer = 0f;
    private bool _isReady = false;

    public string FileName => fileName;
    public int DefaultContent => defaultContent;

    void Start()
    {
        LevelFileManager manager = FindObjectOfType<LevelFileManager>();
        if (manager == null)
        {
            Debug.LogError("TimeScaleController: 未找到 LevelFileManager");
            return;
        }

        _folderPath = manager.GetFolderPath();
        _checkInterval = manager.GetCheckInterval();

        if (string.IsNullOrEmpty(_folderPath))
        {
            Debug.LogError("TimeScaleController: 文件夹路径为空");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("TimeScaleController: fileName 为空，请检查 Inspector");
            return;
        }

        // 确保目录存在
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
            Debug.Log($"TimeScaleController: 创建目录 {_folderPath}");
        }

        CreateDefaultFile();
        CheckFile();
        _isReady = true;

        // 初始设置TimeScale
        if (_lastContent >= 0)
        {
            ApplyTimeScale(_lastContent);
        }
    }

    void Update()
    {
        if (!_isReady) return;

        _timer += Time.unscaledDeltaTime; // 使用不受TimeScale影响的真实时间
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
            Debug.LogError($"TimeScaleController: 无法创建文件 - folderPath={_folderPath}, fileName={fileName}");
            return;
        }

        // 检查是否在区域内（可选功能，根据你的需求）
        FileRegionManager[] regions = FindObjectsOfType<FileRegionManager>();
        bool isInRegionPreset = false;
        foreach (var region in regions)
        {
            // 这里需要根据你的FileRegionManager实现来判断
            // 如果不需要这个功能，可以注释掉
            isInRegionPreset = false;
            break;
        }

        if (isInRegionPreset) return;

        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");

        // 如果文件存在，读取现有内容
        if (File.Exists(fullPath))
        {
            Debug.Log($"TimeScaleController: 文件已存在 {fullPath}");
            try
            {
                string content = File.ReadAllText(fullPath, System.Text.Encoding.UTF8).Trim();
                if (int.TryParse(content, out int value))
                {
                    _lastContent = value;
                    Debug.Log($"TimeScaleController: 现有文件内容 = {value}");
                }
                else
                {
                    Debug.LogWarning($"TimeScaleController: 文件内容 '{content}' 不是有效数字，使用默认值");
                    _lastContent = defaultContent;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TimeScaleController: 读取现有文件失败 - {e.Message}");
                _lastContent = defaultContent;
            }
            return;
        }

        // 文件不存在，创建新文件
        try
        {
            File.WriteAllText(fullPath, defaultContent.ToString(), System.Text.Encoding.UTF8);
            _lastContent = defaultContent;
            Debug.Log($"TimeScaleController: 创建文件成功 {fullPath}, 内容={defaultContent}");

            if (File.Exists(fullPath))
            {
                Debug.Log($"TimeScaleController: 文件创建验证成功");
            }
            else
            {
                Debug.LogError($"TimeScaleController: 文件创建后不存在，可能是权限问题");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TimeScaleController: 创建文件失败 - {e.Message}");
        }
    }

    void CheckFile()
    {
        if (string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(fileName)) return;

        string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");

        try
        {
            if (!File.Exists(fullPath)) return;

            string content = File.ReadAllText(fullPath, System.Text.Encoding.UTF8).Trim();

            if (int.TryParse(content, out int currentValue))
            {
                // 限制数值范围
                currentValue = Mathf.Clamp(currentValue, minFileValue, maxFileValue);

                if (currentValue != _lastContent)
                {
                    Debug.Log($"TimeScaleController: TimeScale变化 [{_lastContent} -> {currentValue}]");
                    _lastContent = currentValue;
                    ApplyTimeScale(currentValue);
                }
            }
            else
            {
                Debug.LogWarning($"TimeScaleController: 文件内容 '{content}' 不是有效数字，已忽略");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TimeScaleController: 读取文件失败 - {e.Message}");
        }
    }

    void ApplyTimeScale(int fileValue)
    {
        // 将文件数值映射到TimeScale范围
        float t = (fileValue - minFileValue) / (float)(maxFileValue - minFileValue);
        float newTimeScale = Mathf.Lerp(minTimeScale, maxTimeScale, t);

        // 应用TimeScale
        Time.timeScale = newTimeScale;

        Debug.Log($"TimeScaleController: TimeScale已设置为 {newTimeScale:F2} (文件值: {fileValue})");

        // 可选：输出对应关系
        if (fileValue == minFileValue)
            Debug.Log($"游戏已完全暂停 (TimeScale = {newTimeScale})");
        else if (fileValue == maxFileValue)
            Debug.Log($"游戏正常速度 (TimeScale = {newTimeScale})");
        else if (newTimeScale > 0 && newTimeScale < 1)
            Debug.Log($"游戏慢动作模式 (TimeScale = {newTimeScale})");
    }

    // 公共方法：手动设置TimeScale（可选）
    public void SetTimeScaleDirectly(float timeScale)
    {
        timeScale = Mathf.Clamp01(timeScale);
        Time.timeScale = timeScale;

        // 可选：同步更新文件内容
        if (!string.IsNullOrEmpty(_folderPath) && !string.IsNullOrEmpty(fileName))
        {
            // 反向映射：将TimeScale转换回文件数值
            float t = (timeScale - minTimeScale) / (maxTimeScale - minTimeScale);
            int fileValue = Mathf.RoundToInt(Mathf.Lerp(minFileValue, maxFileValue, t));
            fileValue = Mathf.Clamp(fileValue, minFileValue, maxFileValue);

            string fullPath = Path.Combine(_folderPath, $"{fileName}.txt");
            try
            {
                File.WriteAllText(fullPath, fileValue.ToString(), System.Text.Encoding.UTF8);
                _lastContent = fileValue;
                Debug.Log($"TimeScaleController: 已同步更新文件为 {fileValue}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TimeScaleController: 同步更新文件失败 - {e.Message}");
            }
        }
    }

    // 获取当前TimeScale对应的文件值（用于调试）
    public int GetCurrentFileValue()
    {
        return _lastContent;
    }

    // 获取当前TimeScale
    public float GetCurrentTimeScale()
    {
        return Time.timeScale;
    }


    public void ResumeTimeScale()
    {
        Time.timeScale = 1.0f;
    }
}