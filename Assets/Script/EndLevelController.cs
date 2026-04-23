using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class EndLevelController : MonoBehaviour
{
    [SerializeField] private string processName = "au3";
    [SerializeField] private string protectedFileName = "Aμ3.ai";
    [SerializeField] private string inputFileName = "input.txt";
    [SerializeField] private string targetContent = "end";
    [SerializeField] private string windowTitle = "Aμ3 AI System - Running";  // 控制台窗口标题

    // Unity 事件
    public UnityEvent OnFileDeleted;      // 玩家删除文件时触发
    public UnityEvent<string> OnInputMatched;  // input.txt 内容匹配时触发

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
        _isReady = true;
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
}