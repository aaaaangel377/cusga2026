# AdvancedItemController 使用指南

## 系统架构

```
AdvancedItemController (主控制器)
    ├── FeatureProcessor (抽象基类)
    │   ├── PositionProcessor    (位置功能)
    │   ├── RotationProcessor    (旋转功能)
    │   ├── SpawnProcessor       (复制生成功能)
    │   ├── CriticalProcessor    (删除重开功能)
    │   ├── VisibilityProcessor  (删除消失功能)
    │   └── ColorProcessor       (颜色功能 - 示例)
    └── GridUtils (工具类)
```

## 基础使用

### 1. 在 Unity Inspector 中配置

```
1. 选择游戏对象
2. 添加组件：AdvancedItemController
3. 配置参数:
   - File Name: 文件名 (不含 .txt)
   - Target Object: 目标对象 (默认为自身)
   - 功能开关:
     ☑ Enable Position    (位置控制)
     ☐ Enable Rotation    (旋转控制)
     ☐ Enable Spawn       (复制生成)
     ☐ Enable Critical    (删除重开)
     ☑ Enable Visibility  (删除消失)
     ☐ Enable Color       (颜色控制)
```

### 2. 文件格式

```
# 位置格式
位置：x,y

# 旋转格式
旋转：角度

# 颜色格式 (示例)
颜色：r,g,b

# 组合示例
位置：4,4
旋转：90.0
```

### 3. 文件路径

```
level/{levelIndex}/{fileName}.txt
例如：level/2/旋转平台.txt
```

## 添加新功能

### 步骤 1: 创建新的 Processor 类

```csharp
using UnityEngine;

public class MyNewFeatureProcessor : FeatureProcessor
{
    public override string FeatureName => "MyNewFeature";
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
        // 文件创建时的逻辑
    }
    
    public override void OnFileUpdated(string content, GameObject target)
    {
        // 文件内容更新时的逻辑
        // 从 content 中解析自定义格式
    }
    
    public override void OnFileDeleted(GameObject target)
    {
        // 文件删除时的逻辑
    }
    
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        // 文件复制时的逻辑
    }
    
    public override string CreateDefaultContent(GameObject target)
    {
        // 创建默认文件内容
        return "默认内容";
    }
}
```

### 步骤 2: 在 AdvancedItemController 中添加开关

```csharp
// 添加序列化字段
[SerializeField] private bool enableMyNewFeature = false;

// 在 InitializeProcessors() 中注册
if (enableMyNewFeature)
{
    _processors.Add(new MyNewFeatureProcessor());
}
```

### 步骤 3: 创建.meta 文件

为新的脚本文件创建 Unity .meta 文件。

## 扩展示例：缩放功能

```csharp
using UnityEngine;
using System.Text.RegularExpressions;

public class ScaleProcessor : FeatureProcessor
{
    public override string FeatureName => "Scale";
    
    public override void OnFileUpdated(string content, GameObject target)
    {
        Match match = Regex.Match(content, @"缩放：(-?\d+\.?\d*),(-?\d+\.?\d*)");
        if (match.Success)
        {
            float x = float.Parse(match.Groups[1].Value);
            float y = float.Parse(match.Groups[2].Value);
            target.transform.localScale = new Vector3(x, y, 1);
        }
    }
    
    // ... 其他方法实现
}
```

## 与旧系统兼容

- `BasicItem` 及其子类仍然可用
- `LevelFileManager` 同时支持 `BasicItem` 和 `AdvancedItemController`
- 可以混合使用两种系统

## 常见问题

### Q: 如何禁用某个功能？
A: 在 Inspector 中取消勾选对应的功能开关即可。

### Q: 可以只使用部分功能吗？
A: 可以，自由勾选需要的功能开关。

### Q: 如何添加多个自定义功能？
A: 创建多个 Processor 类，分别添加开关和注册逻辑。

### Q: 文件内容格式可以自定义吗？
A: 可以，在每个 Processor 中自定义解析逻辑。
