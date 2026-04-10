using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 音频管理器 - 单例模式
/// 负责管理游戏中的所有音频播放，包括背景音乐(BGM)和音效(Effect)
/// 提供音效限流、连续音效播放等高级功能
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    /// <summary>
    /// 标志位：是否允许添加新的音效片段
    /// 用于控制音效的播放频率，防止短时间内播放过多音效
    /// </summary>
    private bool canAddClip;

    /// <summary>
    /// 每秒允许播放的最大音效次数
    /// 用于限制音效的播放频率，避免音效叠加过多
    /// </summary>
    [SerializeField] private int maxSecondEffectTimes = 20;

    /// <summary>
    /// 移动音效音量
    /// </summary>
    [SerializeField] private float walkVolume = 0.6f;

    /// <summary>
    /// 跳跃音效音量
    /// </summary>
    [SerializeField] private float jumpVolume = 0.8f;

    /// <summary>
    /// 胜利音效音量
    /// </summary>
    [SerializeField] private float victoryVolume = 1.0f;

    /// <summary>
    /// 死亡音效音量
    /// </summary>
    [SerializeField] private float gameOverVolume = 1.0f;

    /// <summary>
    /// 文件修改成功音效音量
    /// </summary>
    [SerializeField] private float fileSuccessVolume = 0.7f;

    /// <summary>
    /// 文件修改失败音效音量
    /// </summary>
    [SerializeField] private float fileFailVolume = 0.7f;

    /// <summary>
    /// 单例实例
    /// 全局唯一访问点，确保整个游戏中只有一个AudioManager实例
    /// </summary>
    public static AudioManager Instance;

    /// <summary>
    /// 背景音乐音频源组件
    /// 专门用于播放背景音乐，支持循环播放
    /// </summary>
    [SerializeField] private AudioSource bgmSource;

    /// <summary>
    /// 背景音乐是否循环播放
    /// 可在Inspector面板中配置，默认为true
    /// </summary>
    [SerializeField] bool bgmIsLoop = true;

    /// <summary>
    /// 音效音频源组件
    /// 专门用于播放各种音效（如按钮点击、爆炸声等）
    /// </summary>
    [SerializeField] private AudioSource effectSource;

    /// <summary>
    /// 移动音效音量
    /// </summary>
    public float WalkVolume => walkVolume;

    /// <summary>
    /// 跳跃音效音量
    /// </summary>
    public float JumpVolume => jumpVolume;

    /// <summary>
    /// 胜利音效音量
    /// </summary>
    public float VictoryVolume => victoryVolume;

    /// <summary>
    /// 死亡音效音量
    /// </summary>
    public float GameOverVolume => gameOverVolume;

    /// <summary>
    /// 文件修改成功音效音量
    /// </summary>
    public float FileSuccessVolume => fileSuccessVolume;

    /// <summary>
    /// 文件修改失败音效音量
    /// </summary>
    public float FileFailVolume => fileFailVolume;

    /// <summary>
    /// 连续音效播放器字典
    /// 存储所有已注册的连续音效播放器，通过唯一键值标识
    /// 用于管理需要持续播放或条件触发的音效（如脚步声、环境音效等）
    /// </summary>
    private Dictionary<string,ContinuousAudioEffectPlayer> continuousAudioEffectPlayers = new Dictionary<string, ContinuousAudioEffectPlayer>();

    /// <summary>
    /// 场景加载事件是否已订阅
    /// 避免 DontDestroyOnLoad 导致重复订阅
    /// </summary>
    private bool _sceneLoadedEventSubscribed = false;

    /// <summary>
    /// 初始化单例实例和音频源组件
    /// 确保场景切换时音频管理器不会被销毁
    /// 自动获取或创建所需的AudioSource组件
    /// </summary>
    private void Awake()
    {
        // 单例模式实现：确保只有一个实例存在
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
        }
        else
        {
            Destroy(gameObject); // 已存在实例则销毁当前对象
        }

        // 自动获取或创建AudioSource组件
        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (audioSources == null || audioSources.Length == 0)
        {
            // 没有AudioSource组件，创建BGM和Effect两个组件
            bgmSource = gameObject.AddComponent<AudioSource>();
            effectSource = gameObject.AddComponent<AudioSource>();
        }
        else if (audioSources.Length == 1)
        {
            // 只有一个AudioSource，分配给BGM，再创建一个给Effect
            bgmSource = audioSources[0];
            effectSource = gameObject.AddComponent<AudioSource>();
        }
        else if (audioSources.Length >= 2)
        {
            // 有两个或更多AudioSource，使用前两个
            bgmSource = audioSources[0];
            effectSource = audioSources[1];
        }
        
        // 设置循环属性
        bgmSource.loop = bgmIsLoop;  // BGM默认循环
        effectSource.loop = false;    // 音效不循环
    }

    /// <summary>
    /// 启动音效限流协程
    /// 控制每秒可播放的音效数量，防止音效过于密集
    /// </summary>
    private void Start()
    {
        StartCoroutine(ContinueToChangeCanAddClips());
    }

    void OnEnable()
    {
        if (!_sceneLoadedEventSubscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneLoadedEventSubscribed = true;
        }
    }

    void OnDisable()
    {
        if (_sceneLoadedEventSubscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _sceneLoadedEventSubscribed = false;
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// 从Resources/Sounds/BGM/目录加载指定名称的音频文件
    /// </summary>
    /// <param name="name">音频文件名（不包含扩展名）</param>
    /// <param name="is_loop">是否循环播放，默认为true</param>
    public void PlayBGM(string name, bool is_loop = true)
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/BGM/" + name);
        bgmSource.clip = clip;
        bgmSource.loop = is_loop;
        bgmSource.Play();
        Resources.UnloadUnusedAssets(); // 释放未使用的资源
    }

    /// <summary>
    /// 停止背景音乐播放
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>
    /// 暂停背景音乐播放
    /// 可通过ContinueBGM方法恢复播放
    /// </summary>
    public void PauseBGM()
    {
        bgmSource.Pause();
    } 

    /// <summary>
    /// 恢复背景音乐播放
    /// 从暂停位置继续播放
    /// </summary>
    public void ContinueBGM()
    {
        bgmSource.UnPause();
    }

    /// <summary>
    /// 播放音效
    /// 从Resources/Sounds/Effects/目录加载指定名称的音频文件
    /// 支持限流控制，防止短时间内播放过多音效
    /// </summary>
    /// <param name="name">音效文件名（不包含扩展名）</param>
    /// <param name="isIgnore">是否忽略限流控制，默认为false
    /// 如果为true，则在canAddClip为true时才能播放，播放后立即设为false</param>
    public void PlayEffect(string name, bool isIgnore = false)
    {
        if (isIgnore)
        {
            // 开启限流：检查是否可以播放
            if (canAddClip)
            {
                canAddClip = false;
                AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/" + name); 
                effectSource.clip = clip;
                effectSource.Play();
            }
        }
        else
        {
            // 不开启限流：直接播放
            AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/" + name);
            effectSource.clip = clip;
            effectSource.Play();
        }
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 使用 PlayOneShot 方式播放音效
    /// PlayOneShot 允许同时播放多个音效，不会被新的音效打断
    /// 适用于需要叠加播放的音效（如连续射击、爆炸等）
    /// </summary>
    /// <param name="name">音效文件名（不包含扩展名）</param>
    /// <param name="volumeScale">音量缩放系数，默认为 1.0</param>
    /// <param name="isIgnore">是否忽略限流控制，默认为 true</param>
    public void PlayOneShotEffect(string name, float volumeScale = 1.0f, bool isIgnore = true)
    {
        if (isIgnore)
        {
            if (canAddClip)
            {
                canAddClip = false;
                AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/" + name); 
                effectSource.PlayOneShot(clip, volumeScale);
            }
        }
        else
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/" + name);
            effectSource.PlayOneShot(clip, volumeScale);
        }
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 停止当前正在播放的音效
    /// </summary>
    public void StopEffect()
    {
        effectSource.Stop();
    }

    /// <summary>
    /// 音效限流协程
    /// 每隔一定时间间隔重置canAddClip标志，允许播放新的音效
    /// 时间间隔 = 1秒 / maxSecondEffectTimes
    /// </summary>
    private IEnumerator ContinueToChangeCanAddClips()
    {
        while (true)
        {
            yield return new WaitForSeconds((float)1/maxSecondEffectTimes);
            canAddClip = true;
        }
    }
    
    /// <summary>
    /// 注册连续音效播放器（多音频版本）
    /// 用于需要持续播放、根据条件触发的音效（如脚步声、环境音效等）
    /// 支持从多个音频中随机选择播放
    /// </summary>
    /// <param name="key">唯一标识符，用于后续操作该播放器</param>
    /// <param name="condition">播放条件函数，返回true时播放，false时暂停</param>
    /// <param name="audioClipNames">音频文件名称列表（随机播放其中一个）</param>
    /// <param name="startImmediately">是否立即开始监控播放，默认为true</param>
    public void RegisterContinuousAudioEffect(string key, Func<bool> condition, List<string> audioClipNames,bool startImmediately = true)
    {
        if (!continuousAudioEffectPlayers.ContainsKey(key))
        {
            ContinuousAudioEffectPlayer player = new ContinuousAudioEffectPlayer(effectSource, condition, audioClipNames);
            continuousAudioEffectPlayers.Add(key, player);
            if (startImmediately)
            {
                player.StartMonitorToPlay();
            }
        }
        else
        {
            Debug.LogWarning("Duplicate key: " + key);
        }
    } 

    /// <summary>
    /// 注册连续音效播放器（单音频版本）
    /// 重载方法，方便直接传入单个音频文件名
    /// </summary>
    /// <param name="key">唯一标识符，用于后续操作该播放器</param>
    /// <param name="condition">播放条件函数，返回true时播放，false时暂停</param>
    /// <param name="audioClipName">音频文件名称</param>
    /// <param name="startImmediately">是否立即开始监控播放，默认为true</param>
    public void RegisterContinuousAudioEffect(string key, Func<bool> condition, string audioClipName,bool startImmediately = true)
    {
        if (!continuousAudioEffectPlayers.ContainsKey(key))
        {
            ContinuousAudioEffectPlayer player = new ContinuousAudioEffectPlayer(effectSource, condition,new List<string>(){audioClipName});
            continuousAudioEffectPlayers.Add(key, player);
            if (startImmediately)
            {
                player.StartMonitorToPlay();
            }
        }
        else
        {
            Debug.LogWarning("Duplicate key: " + key);
        }
    }

    /// <summary>
    /// 注销连续音效播放器
    /// 停止对应的音效监控并从字典中移除
    /// </summary>
    /// <param name="key">注册时使用的唯一标识符</param>
    public void UnregisterContinuousAudioEffect(string key)
    {
        if (continuousAudioEffectPlayers.ContainsKey(key))
        {
            continuousAudioEffectPlayers[key].StopMonitorToPlay();
            continuousAudioEffectPlayers.Remove(key);
        }
        else
        {
            Debug.LogWarning("None key: " + key);
        }
    }
    
    /// <summary>
    /// 启动指定的连续音效播放器
    /// 用于之前注册但未立即启动的播放器，或已停止的播放器
    /// </summary>
    /// <param name="key">注册时使用的唯一标识符</param>
    public void StartContinuousAudioEffect(string key)
    {
        if (continuousAudioEffectPlayers.ContainsKey(key))
        {
            continuousAudioEffectPlayers[key].StartMonitorToPlay();
        }
        else
        {
            Debug.LogWarning("None key: " + key);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var player in continuousAudioEffectPlayers.Values)
        {
            player.StopMonitorToPlay();
        }
        continuousAudioEffectPlayers.Clear();
        
        Debug.Log("[AudioManager] 场景切换，已清理所有连续音效播放器，数量：" + continuousAudioEffectPlayers.Count);
    }
    
}

/// <summary>
/// 连续音效播放器
/// 用于管理需要持续监控条件并播放音效的场合
/// 如：角色移动时的脚步声、车辆引擎声等
/// 支持多个音频随机选择播放
/// </summary>
class ContinuousAudioEffectPlayer
{
    /// <summary>
    /// 音频源组件，用于实际播放音频
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// 音频片段列表
    /// 从多个片段中随机选择一个播放，增加音效的多样性
    /// </summary>
    private List<AudioClip> AudioClips = new List<AudioClip>();

    /// <summary>
    /// 播放条件委托
    /// 每次循环检查此条件，返回true时播放，false时跳过
    /// </summary>
    private Func<bool> condition;

    /// <summary>
    /// 连续播放协程引用
    /// 用于启动和停止协程
    /// </summary>
    private IEnumerator continuousPlayCoroutine;

    /// <summary>
    /// 是否正在播放的状态标志
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// 构造函数
    /// 加载指定的音频文件到内存中
    /// </summary>
    /// <param name="source">音频源组件</param>
    /// <param name="condition">播放条件函数</param>
    /// <param name="audioClipNames">音频文件名列表</param>
    public ContinuousAudioEffectPlayer(AudioSource source, Func<bool> condition, List<string> audioClipNames)
    {
        audioSource = source;
        this.condition = condition;
        AudioClip clip;
        foreach (var VARIABLE in audioClipNames)
        {
           if((clip = Resources.Load<AudioClip>("Sounds/Effects/" + VARIABLE))!= null)
           {
               AudioClips.Add(clip);
           }
           else
           {
               Debug.LogWarning("Audio clip " + VARIABLE + " not found");
           }
        }
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 连续播放协程
    /// 循环检查条件并播放音效，每个音效播放完成后等待音频长度再继续
    /// 从AudioClips列表中随机选择一个播放
    /// </summary>
    private IEnumerator ContinuousPlayCoroutine()
    {
        if (AudioClips.Count > 0)
        {
            while (IsPlaying)
            {
                if (audioSource && condition())
                {
                    // 从列表中随机选择一个音频
                    AudioClip clip = AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];
                    audioSource.PlayOneShot(clip);
                    yield return new WaitForSeconds(clip.length);
                }
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("No audio clips to play.");
        }
    }

    /// <summary>
    /// 开始监控并播放音效
    /// 启动连续播放协程，开始检查条件并播放
    /// </summary>
    public void StartMonitorToPlay()
    {
        if (!IsPlaying)
        {
            IsPlaying = true;
            continuousPlayCoroutine = ContinuousPlayCoroutine();
            AudioManager.Instance.StartCoroutine(continuousPlayCoroutine);
        }
    }

    /// <summary>
    /// 停止监控并停止播放
    /// 停止连续播放协程，IsPlaying标志设为false
    /// </summary>
    public void StopMonitorToPlay()
    {
        IsPlaying = false;
        AudioManager.Instance.StopCoroutine(continuousPlayCoroutine);
    }

}
