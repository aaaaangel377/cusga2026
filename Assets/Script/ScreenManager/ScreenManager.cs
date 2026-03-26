using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [Header("场景开始时自动设置")]
    [Tooltip("是否在场景开始时强制设置为4:3全屏模式")]
    public bool setFullscreenOnStart = false;

    [Tooltip("是否在场景开始时强制设置为4:3窗口模式")]
    public bool setWindowedOnStart = false;

    [Header("窗口模式设置")]
    [Tooltip("窗口模式下的分辨率宽度")]
    public int windowedWidth = 800;

    [Tooltip("窗口模式下的分辨率高度")]
    public int windowedHeight = 600;

    [Header("全屏模式设置")]
    [Tooltip("全屏模式下的分辨率宽度")]
    public int fullscreenWidth = 1024;

    [Tooltip("全屏模式下的分辨率高度")]
    public int fullscreenHeight = 768;

    private void Start()
    {
        // 根据公开的bool值决定在场景开始时运行哪个函数
        if (setFullscreenOnStart)
        {
            Set4To3Fullscreen();
        }
        else if (setWindowedOnStart)
        {
            Set4To3Windowed();
        }
    }

    /// <summary>
    /// 强制设置分辨率为4:3比例并全屏
    /// </summary>
    public void Set4To3Fullscreen()
    {
        // 计算4:3分辨率
        int width = fullscreenWidth;
        int height = fullscreenHeight;

        // 确保分辨率是4:3比例
        if (width * 3 != height * 4)
        {
            // 如果不是精确的4:3，调整高度为宽度的3/4
            height = width * 3 / 4;
        }

        // 设置分辨率为4:3，并设置为全屏模式
        Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);

        Debug.Log($"已设置为4:3全屏模式：{width}x{height}");
    }

    /// <summary>
    /// 将游戏以4:3比例缩小至一个比较小的窗口化的4:3窗口
    /// </summary>
    public void Set4To3Windowed()
    {
        // 使用公开的窗口大小设置
        int width = windowedWidth;
        int height = windowedHeight;

        // 确保分辨率是4:3比例
        if (width * 3 != height * 4)
        {
            // 如果不是精确的4:3，调整高度为宽度的3/4
            height = width * 3 / 4;
        }

        // 设置分辨率为4:3，并设置为窗口模式
        Screen.SetResolution(width, height, FullScreenMode.Windowed);

        Debug.Log($"已设置为4:3窗口模式：{width}x{height}");
    }

    /// <summary>
    /// 自定义4:3全屏分辨率
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public void SetCustom4To3Fullscreen(int width, int height)
    {
        // 确保传入的分辨率是4:3比例
        if (width * 3 != height * 4)
        {
            Debug.LogWarning($"分辨率 {width}x{height} 不是标准的4:3比例，将自动调整");
            height = width * 3 / 4;
        }

        Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
        Debug.Log($"已设置为自定义4:3全屏模式：{width}x{height}");
    }

    /// <summary>
    /// 自定义4:3窗口模式
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public void SetCustom4To3Windowed(int width, int height)
    {
        // 确保传入的分辨率是4:3比例
        if (width * 3 != height * 4)
        {
            Debug.LogWarning($"分辨率 {width}x{height} 不是标准的4:3比例，将自动调整");
            height = width * 3 / 4;
        }

        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        Debug.Log($"已设置为自定义4:3窗口模式：{width}x{height}");
    }
}