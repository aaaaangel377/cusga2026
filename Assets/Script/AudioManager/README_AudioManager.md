# AudioManager 音频管理器

## 简介

AudioManager是Unity项目的音频管理解决方案，采用单例模式设计，提供背景音乐(BGM)和音效(Effect)的统一管理。支持音效限流、连续音效播放等高级功能。

## 功能特性

### 核心功能
- **单例模式**：全局唯一实例，跨场景持久化
- **背景音乐管理**：支持播放、暂停、停止、恢复
- **音效管理**：支持普通播放和PlayOneShot叠加播放
- **音效限流**：控制每秒最大音效播放次数，防止音效叠加过多
- **连续音效播放**：支持条件触发的持续音效（如脚步声、环境音效等）

### 高级特性
- 自动AudioSource管理（自动创建或复用现有组件）
- 多音频随机选择播放
- 资源自动释放优化

## 目录结构

```
Resources/
├── Sounds/
│   ├── BGM/          # 背景音乐文件夹
│   │   └── *.mp3/wav/ogg
│   └── Effects/      # 音效文件夹
│       └── *.mp3/wav/ogg
```

## 使用方法

### 基础使用

```csharp
// 播放背景音乐
AudioManager.Instance.PlayBGM("menu_bgm");

// 播放背景音乐（不循环）
AudioManager.Instance.PlayBGM("menu_bgm", false);

// 暂停背景音乐
AudioManager.Instance.PauseBGM();

// 恢复背景音乐
AudioManager.Instance.ContinueBGM();

// 停止背景音乐
AudioManager.Instance.StopBGM();
```

### 音效播放

```csharp
// 普通音效播放（会中断当前音效）
AudioManager.Instance.PlayEffect("click");

// 开启限流的音效播放
AudioManager.Instance.PlayEffect("shoot", isIgnore: true);

// PlayOneShot播放（不会中断其他音效，可叠加）
AudioManager.Instance.PlayOneShotEffect("explosion");

// 停止当前音效
AudioManager.Instance.StopEffect();
```

### 连续音效播放

适用于需要持续播放的音效，如脚步声、引擎声等：

```csharp
// 注册连续音效（单音频）
AudioManager.Instance.RegisterContinuousAudioEffect(
    "footsteps",                    // 唯一标识符
    () => player.isMoving,          // 播放条件
    "footstep_sound"                // 音频文件名
);

// 注册连续音效（多音频随机播放）
AudioManager.Instance.RegisterContinuousAudioEffect(
    "ambient",
    () => true,
    new List<string> { "wind1", "wind2", "wind3" }
);

// 启动连续音效（如果注册时未立即启动）
AudioManager.Instance.StartContinuousAudioEffect("footsteps");

// 注销连续音效
AudioManager.Instance.UnregisterContinuousAudioEffect("footsteps");
```

## API参考

### AudioManager类

| 方法 | 参数 | 描述 |
|------|------|------|
| PlayBGM(name, is_loop) | string name, bool is_loop=true | 播放背景音乐 |
| StopBGM() | - | 停止背景音乐 |
| PauseBGM() | - | 暂停背景音乐 |
| ContinueBGM() | - | 恢复背景音乐 |
| PlayEffect(name, isIgnore) | string name, bool isIgnore=false | 播放音效 |
| PlayOneShotEffect(name, isIgnore) | string name, bool isIgnore=true | 使用PlayOneShot播放音效 |
| StopEffect() | - | 停止当前音效 |
| RegisterContinuousAudioEffect(key, condition, audioClipName, startImmediately) | string key, Func<bool> condition, string audioClipName, bool startImmediately=true | 注册连续音效（单音频） |
| RegisterContinuousAudioEffect(key, condition, audioClipNames, startImmediately) | string key, Func<bool> condition, List<string> audioClipNames, bool startImmediately=true | 注册连续音效（多音频） |
| UnregisterContinuousAudioEffect(key) | string key | 注销连续音效 |
| StartContinuousAudioEffect(key) | string key | 启动连续音效 |

### 可配置字段

| 字段 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| maxSecondEffectTimes | int | 20 | 每秒允许播放的最大音效次数 |
| bgmIsLoop | bool | true | 背景音乐是否循环 |

## 注意事项

1. **音频文件路径**：音频文件必须放在 `Resources/Sounds/` 目录下
   - BGM放在 `Resources/Sounds/BGM/`
   - 音效放在 `Resources/Sounds/Effects/`

2. **音效限流**：当 `isIgnore` 为 `true` 时，音效受 `maxSecondEffectTimes` 参数控制

3. **PlayOneShot vs Play**：
   - `Play`：会中断当前正在播放的同一音频源的其他音频
   - `PlayOneShot`：可以叠加播放，不会中断其他音频

4. **内存管理**：系统会自动调用 `Resources.UnloadUnusedAssets()` 释放未使用的音频资源

## 示例场景

### 游戏菜单场景
```csharp
void Start()
{
    // 播放菜单背景音乐
    AudioManager.Instance.PlayBGM("menu_theme");
}

void OnButtonClick()
{
    // 播放按钮点击音效
    AudioManager.Instance.PlayOneShotEffect("button_click");
}
```

### 玩家移动
```csharp
void Update()
{
    bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
    
    if (isMoving && !footstepsRegistered)
    {
        // 注册脚步声
        AudioManager.Instance.RegisterContinuousAudioEffect(
            "player_footsteps",
            () => isMoving,
            new List<string> { "footstep1", "footstep2", "footstep3" }
        );
        footstepsRegistered = true;
    }
    else if (!isMoving && footstepsRegistered)
    {
        // 注销脚步声
        AudioManager.Instance.UnregisterContinuousAudioEffect("player_footsteps");
        footstepsRegistered = false;
    }
}
```

## 作者

CUSGA 2026 Team
