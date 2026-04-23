using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // 如果使用TextMeshPro，否则使用UnityEngine.UI

public class UnlockNotification : MonoBehaviour
{
    [Header("弹窗设置")]
    public GameObject notificationPanel;      // 弹窗面板
    public Text notificationText;             // 显示文本（如果用TextMeshPro则改为TMP_Text）
    public float displayDuration = 3f;        // 显示时长
    public float fadeInDuration = 0.5f;       // 淡入时长
    public float fadeOutDuration = 0.5f;      // 淡出时长

    [Header("位置设置")]
    public Vector2 offset = new Vector2(-50f, 50f);  // 距离右下角的偏移

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine currentNotification;

    // 单例模式，方便其他脚本调用
    public static UnlockNotification Instance { get; private set; }

    void Awake()
    {
        //// 单例设置
        //if (Instance == null)
        //{
            Instance = this;
        //    //DontDestroyOnLoad(gameObject);
        //}
        //else
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        // 初始化组件
        if (notificationPanel != null)
        {
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }

            rectTransform = notificationPanel.GetComponent<RectTransform>();

            // 设置初始状态为隐藏
            canvasGroup.alpha = 0f;
            notificationPanel.SetActive(false);
        }
    }

    void Start()
    {
        
    }

    /// <summary>
    /// 显示解锁通知（公开调用方法）
    /// </summary>
    /// <param name="unlockedLevelName">解锁的关卡名称，如 "8ex1"</param>
    public void ShowUnlockNotification(string unlockedLevelName)
    {
        string message = $"你已解锁 {unlockedLevelName}";
        ShowNotification(message);
    }

    /// <summary>
    /// 显示自定义通知
    /// </summary>
    /// <param name="message">通知内容</param>
    public void ShowNotification(string message)
    {
        // 如果已有通知在显示，先停止
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }

        // 开始新的通知
        currentNotification = StartCoroutine(DisplayNotificationCoroutine(message));
    }

    /// <summary>
    /// 通知显示的协程
    /// </summary>
    IEnumerator DisplayNotificationCoroutine(string message)
    {
        if (notificationPanel == null || notificationText == null)
        {
            Debug.LogError("Notification Panel 或 Text 未设置！");
            yield break;
        }

        // 设置文本
        notificationText.text = message;

        // 激活面板
        notificationPanel.SetActive(true);

        // 淡入
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));

        // 等待显示时间
        yield return new WaitForSeconds(displayDuration);

        // 淡出
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));

        // 隐藏面板
        notificationPanel.SetActive(false);

        currentNotification = null;
    }

    /// <summary>
    /// 淡入淡出协程
    /// </summary>
    IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime; // 使用unscaledDeltaTime，不受Time.timeScale影响
            float t = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 立即隐藏通知
    /// </summary>
    public void HideNotificationImmediately()
    {
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
            currentNotification = null;
        }

        if (notificationPanel != null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            notificationPanel.SetActive(false);
        }
    }
}