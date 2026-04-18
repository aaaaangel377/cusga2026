using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BgmController : MonoBehaviour
{
    [Header("扫描设置")]
    [SerializeField] private float checkInterval = 0.5f;
    
    private AudioSource _audioSource;
    private float _timer = 0f;
    private bool _lastBgmState = true;
    
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("[BgmController] 未找到 AudioSource 组件！");
        }
    }
    
    void Start()
    {
        UpdateBgmState();
    }
    
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            CheckBgmState();
        }
    }
    
    void CheckBgmState()
    {
        bool currentBgmState = AudioConfig.BgmEnabledStatic;
        
        if (currentBgmState != _lastBgmState)
        {
            _lastBgmState = currentBgmState;
            UpdateBgmState();
        }
    }
    
    void UpdateBgmState()
    {
        if (_audioSource == null) return;
        
        if (AudioConfig.BgmEnabledStatic)
        {
            // BGM 开启：启用音频组件并播放
            _audioSource.enabled = true;
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
            Debug.Log("[BgmController] BGM 已启用");
        }
        else
        {
            // BGM 关闭：禁用音频组件并停止播放
            _audioSource.enabled = false;
            _audioSource.Stop();
            Debug.Log("[BgmController] BGM 已禁用");
        }
    }
    
    void OnEnable()
    {
        // 场景切换后重新检查配置
        Invoke(nameof(UpdateBgmState), 0.1f);
    }
}
