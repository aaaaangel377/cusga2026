using UnityEngine;

public class HintVisibilityController : MonoBehaviour
{
    [Header("提示对象设置")]
    [Tooltip("需要根据提示开关控制显示/隐藏的GameObject")]
    public GameObject hintObject;

    [Tooltip("如果为空，将使用当前附加的对象")]
    public bool useSelf = true;

    [Header("存档设置")]
    [Tooltip("存档系统，如果为空将自动查找")]
    public LevelUnlockSystem levelSystem;

    [Header("行为选项")]
    [Tooltip("反转逻辑：当提示关闭时显示，开启时隐藏")]
    public bool invertLogic = false;

    [Tooltip("启用时自动更新一次")]
    public bool updateOnEnable = true;

    [Tooltip("在Start中更新一次")]
    public bool updateOnStart = true;

    [Header("调试")]
    [Tooltip("在控制台显示状态")]
    public bool debugLog = false;

    private void Start()
    {
        // 自动查找目标对象
        if (hintObject == null && useSelf)
        {
            hintObject = gameObject;
        }

        // 自动查找存档系统
        if (levelSystem == null)
        {
            levelSystem = FindObjectOfType<LevelUnlockSystem>();
        }

        if (updateOnStart)
        {
            UpdateHintVisibility();
        }
    }

    private void OnEnable()
    {
        if (updateOnEnable)
        {
            UpdateHintVisibility();
        }
    }

    /// <summary>
    /// 根据存档中的提示开关状态更新对象可见性
    /// </summary>
    public void UpdateHintVisibility()
    {
        if (hintObject == null)
        {
            Debug.LogWarning($"[HintVisibilityController] 没有设置提示对象: {gameObject.name}");
            return;
        }

        if (levelSystem == null)
        {
            levelSystem = FindObjectOfType<LevelUnlockSystem>();

            if (levelSystem == null)
            {
                Debug.LogError($"[HintVisibilityController] 找不到 LevelUnlockSystem: {gameObject.name}");
                hintObject.SetActive(true); // 默认显示
                return;
            }
        }

        // 获取提示开关状态
        bool hintsEnabled = levelSystem.AreHintsEnabled();

        // 根据反转逻辑计算最终状态
        bool shouldShow = invertLogic ? !hintsEnabled : hintsEnabled;

        // 更新对象可见性
        hintObject.SetActive(shouldShow);

        if (debugLog)
        {
            Debug.Log($"[HintVisibilityController] {hintObject.name} 设置为: {shouldShow} (提示开关: {hintsEnabled}, 反转: {invertLogic})");
        }
    }

    /// <summary>
    /// 手动设置可见性
    /// </summary>
    public void SetHintVisibility(bool visible)
    {
        if (hintObject != null)
        {
            hintObject.SetActive(visible);

            if (debugLog)
            {
                Debug.Log($"[HintVisibilityController] 手动设置 {hintObject.name} 为: {visible}");
            }
        }
    }

    /// <summary>
    /// 立即获取当前提示开关状态
    /// </summary>
    public bool GetCurrentHintState()
    {
        if (levelSystem == null)
        {
            levelSystem = FindObjectOfType<LevelUnlockSystem>();
        }

        return levelSystem != null ? levelSystem.AreHintsEnabled() : false;
    }

    /// <summary>
    /// 刷新并返回当前可见性状态
    /// </summary>
    public bool RefreshVisibility()
    {
        UpdateHintVisibility();
        return hintObject != null ? hintObject.activeSelf : false;
    }
}