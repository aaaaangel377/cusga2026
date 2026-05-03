using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Diagnostics;
using System.Collections;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;

public class EndLevelController : MonoBehaviour
{
    [SerializeField] private string processName = "au3";
    [SerializeField] private string protectedFileName = "Aμ3.ai";
    [SerializeField] private string inputFileName = "input.txt";
    [SerializeField] private string targetContent = "end";
    [SerializeField] private string windowTitle = "Aμ3 AI System - Running";  // 控制台窗口标题

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;  // 黑屏渐变动画时长
    [SerializeField] private CanvasGroup fadeCanvasGroup;  // 用于黑屏的CanvasGroup
    [SerializeField] private GameObject fadePanel;  // 黑屏面板（可选，如果没有CanvasGroup则使用此对象）

    // Unity 事件
    public UnityEvent OnFileDeleted;      // 玩家删除文件时触发
    public UnityEvent<string> OnInputMatched;  // input.txt 内容匹配时触发
    public UnityEvent OnFadeComplete;     // 黑屏渐变完成时触发

    private string _folderPath;
    private float _checkInterval = 0.5f;
    private float _timer = 0f;
    private bool _isReady = false;
    private bool _isProcessRunning = false;
    private string _lastInputContent = "";
    private string _protectedFilePath;
    private string _inputFilePath;
    private Process _au3Process;
    private FileStream _fileLock;
    private bool _fileLockReleased = false;
    private string _batFilePath;
    private bool _isFading = false;
    private CanvasGroup _canvasGroup;

    void Start()
    {
        LevelFileManager manager = FindObjectOfType<LevelFileManager>();
        if (manager == null)
        {
            Debug.LogError("EndLevelController: 未找到 LevelFileManager");
            return;
        }

        _folderPath = manager.GetFolderPath();
        _checkInterval = manager.GetCheckInterval();

        if (string.IsNullOrEmpty(_folderPath))
        {
            Debug.LogError("EndLevelController: 文件夹路径为空");
            return;
        }

        _protectedFilePath = Path.Combine(_folderPath, protectedFileName);
        _inputFilePath = Path.Combine(_folderPath, inputFileName);
        _batFilePath = Path.Combine(_folderPath, "_au3_temp.bat");

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        CreateAndLockFile();
        CreateInputFile();

        StartAu3Process();
        CheckInputFile();

        // 初始化黑屏组件
        InitializeFadeSystem();

        _isReady = true;
    }

    void InitializeFadeSystem()
    {
        // 优先使用 CanvasGroup
        if (fadeCanvasGroup != null)
        {
            _canvasGroup = fadeCanvasGroup;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        // 如果没有 CanvasGroup，尝试从 fadePanel 获取或添加
        else if (fadePanel != null)
        {
            _canvasGroup = fadePanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = fadePanel.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("EndLevelController: 未设置黑屏组件，将自动创建");
            CreateFadePanel();
        }
    }

    void CreateFadePanel()
    {
        // 创建一个新的 UI 面板用于黑屏
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(transform);

        // 添加 Canvas 组件
        Canvas canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 确保在最上层

        // 添加 CanvasGroup
        _canvasGroup = panel.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // 添加图像组件并设置为全屏黑色
        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;

        fadePanel = panel;
        Debug.Log("EndLevelController: 已自动创建黑屏面板");
    }

    /// <summary>
    /// 公开函数：开始黑屏渐变
    /// </summary>
    public void StartFadeToBlack()
    {
        if (_isFading)
        {
            Debug.LogWarning("EndLevelController: 已经在黑屏渐变中");
            return;
        }

        if (_canvasGroup == null)
        {
            Debug.LogError("EndLevelController: 无法开始黑屏渐变 - CanvasGroup 未初始化");
            OnFadeComplete?.Invoke(); // 出错时直接触发完成事件
            return;
        }

        StartCoroutine(FadeToBlackCoroutine());
    }

    /// <summary>
    /// 公开函数：开始从黑屏恢复
    /// </summary>
    public void StartFadeFromBlack()
    {
        if (_isFading)
        {
            Debug.LogWarning("EndLevelController: 已经在黑屏渐变中");
            return;
        }

        if (_canvasGroup == null)
        {
            Debug.LogError("EndLevelController: 无法开始恢复渐变 - CanvasGroup 未初始化");
            return;
        }

        StartCoroutine(FadeFromBlackCoroutine());
    }

    /// <summary>
    /// 公开函数：立即设置黑屏（无动画）
    /// </summary>
    /// <param name="isBlack">true为全黑，false为全透明</param>
    public void SetBlackImmediate(bool isBlack)
    {
        if (_canvasGroup == null)
        {
            Debug.LogError("EndLevelController: CanvasGroup 未初始化");
            return;
        }

        StopAllCoroutines();
        _isFading = false;
        _canvasGroup.alpha = isBlack ? 1f : 0f;
        _canvasGroup.interactable = isBlack;
        _canvasGroup.blocksRaycasts = isBlack;

        Debug.Log($"EndLevelController: 立即设置黑屏状态为 {isBlack}");

        if (isBlack)
        {
            OnFadeComplete?.Invoke();
        }
    }

    /// <summary>
    /// 公开函数：检查是否正在黑屏渐变中
    /// </summary>
    public bool IsFading()
    {
        return _isFading;
    }

    /// <summary>
    /// 公开函数：获取当前黑屏透明度
    /// </summary>
    public float GetCurrentAlpha()
    {
        return _canvasGroup != null ? _canvasGroup.alpha : 0f;
    }

    private IEnumerator FadeToBlackCoroutine()
    {
        _isFading = true;
        float elapsedTime = 0f;
        float startAlpha = _canvasGroup.alpha;

        // 确保在渐变过程中可以阻挡点击
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            // 使用平滑曲线
            float alpha = Mathf.Lerp(startAlpha, 1f, Mathf.SmoothStep(0f, 1f, t));
            _canvasGroup.alpha = alpha;
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _isFading = false;

        Debug.Log("EndLevelController: 黑屏渐变完成");
        OnFadeComplete?.Invoke();
    }

    private IEnumerator FadeFromBlackCoroutine()
    {
        _isFading = true;
        float elapsedTime = 0f;
        float startAlpha = _canvasGroup.alpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            // 使用平滑曲线
            float alpha = Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));
            _canvasGroup.alpha = alpha;
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        // 恢复后不再阻挡点击
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _isFading = false;

        Debug.Log("EndLevelController: 从黑屏恢复完成");
    }

    void CreateAndLockFile()
    {
        try
        {
            if (File.Exists(_protectedFilePath))
            {
                File.Delete(_protectedFilePath);
            }

            _fileLock = new FileStream(_protectedFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            byte[] data = System.Text.Encoding.UTF8.GetBytes("locked");
            _fileLock.Write(data, 0, data.Length);
            _fileLock.Flush();

            Debug.Log($"EndLevelController: 已创建并锁定文件 {_protectedFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EndLevelController: 锁定文件失败 - {e.Message}");
        }
    }

    void StartAu3Process()
    {
        try
        {
            // 创建看起来像 AI 运行的批处理脚本
            string scriptContent = $@"
@echo off
title {windowTitle}
color 0A
echo ========================================
echo    Aμ3 Artificial Intelligence System v3.2.1
echo ========================================
echo.
echo [OK] Neural network loaded (784 nodes)
echo [OK] Memory allocated: 2048MB
echo [OK] File protection enabled
echo.
echo [STATUS] {protectedFileName} is locked and protected
echo [INFO] Process ID: %random%
echo.
echo ----------------------------------------
echo  Aμ3 AI is now monitoring the system
echo  Close this window to shutdown the AI
echo ----------------------------------------
echo.

:ai_loop
echo [%time%] Aμ3^> Scanning file system...
ping 127.0.0.1 -n 2 > nul
echo [%time%] Aμ3^> Checking integrity of {protectedFileName}...
ping 127.0.0.1 -n 2 > nul
echo [%time%] Aμ3^> Protection active - File locked
ping 127.0.0.1 -n 2 > nul
echo [%time%] Aμ3^> Analyzing data patterns...
ping 127.0.0.1 -n 2 > nul
echo.
goto ai_loop
";

            File.WriteAllText(_batFilePath, scriptContent, System.Text.Encoding.UTF8);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _batFilePath;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;

            _au3Process = Process.Start(startInfo);

            Debug.Log($"EndLevelController: Aμ3 AI 已启动，窗口标题: {windowTitle}");
            _isProcessRunning = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EndLevelController: 启动失败 - {e.Message}");
        }
    }

    void Update()
    {
        if (!_isReady) return;

        _timer += Time.deltaTime;
        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            CheckProcess();
            CheckFileExists();
            CheckInputFile();
        }
    }

    void CheckProcess()
    {
        if (_au3Process == null) return;

        bool processExists = !_au3Process.HasExited;

        // 进程关闭时，释放文件锁，让玩家可以删除
        if (_isProcessRunning && !processExists)
        {
            Debug.Log($"EndLevelController: Aμ3 AI 进程已关闭，释放文件锁");

            if (_fileLock != null && !_fileLockReleased)
            {
                _fileLock.Close();
                _fileLock.Dispose();
                _fileLock = null;
                _fileLockReleased = true;
                Debug.Log($"EndLevelController: 文件已解锁，玩家现在可以删除 {protectedFileName}");
            }

            // 清理临时 bat 文件
            if (File.Exists(_batFilePath))
            {
                File.Delete(_batFilePath);
            }
        }

        _isProcessRunning = processExists;
    }

    // 检查文件是否被玩家删除
    void CheckFileExists()
    {
        if (!_fileLockReleased) return;

        if (!File.Exists(_protectedFilePath))
        {
            Debug.Log($"EndLevelController: 玩家已删除文件 {protectedFileName}");
            OnFileDeleted?.Invoke();
            _fileLockReleased = false;
        }
    }

    void CreateInputFile()
    {
        if (File.Exists(_inputFilePath)) return;

        try
        {
            File.WriteAllText(_inputFilePath, "", System.Text.Encoding.UTF8);
            Debug.Log($"EndLevelController: 创建 input.txt {_inputFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EndLevelController: 创建 input.txt 失败 - {e.Message}");
        }
    }

    void CheckInputFile()
    {
        if (!File.Exists(_inputFilePath)) return;

        try
        {
            string content = File.ReadAllText(_inputFilePath, System.Text.Encoding.UTF8).Trim();

            if (content != _lastInputContent)
            {
                _lastInputContent = content;
                Debug.Log($"EndLevelController: input.txt 内容变化 -> '{content}'");

                if (content == targetContent)
                {
                    Debug.Log($"EndLevelController: input.txt 内容匹配 '{targetContent}'");
                    OnInputMatched?.Invoke(content);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EndLevelController: 读取 input.txt 失败 - {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (_fileLock != null)
        {
            _fileLock.Close();
            _fileLock.Dispose();
        }

        if (_au3Process != null && !_au3Process.HasExited)
        {
            _au3Process.Kill();
            _au3Process.Dispose();
        }

        if (File.Exists(_batFilePath))
        {
            File.Delete(_batFilePath);
        }
    }

    public GameObject Endback;
    public void OnEndAnmStart(int wait)
    {
        StartCoroutine(EndBackDelay(wait));
    }
    IEnumerator EndBackDelay(int wait)
    {
        yield return new WaitForSeconds(wait);
        Endback.SetActive(true);
    }
}