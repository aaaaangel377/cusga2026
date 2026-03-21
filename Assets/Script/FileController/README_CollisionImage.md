# CollisionImageItem 使用说明

## 功能概述

`CollisionImageItem` 是一个基于图片的碰撞箱生成器。玩家可以用图片编辑器修改 PNG 文件中的深色区域，游戏中的碰撞箱会自动更新。

## 使用步骤

### 1. 创建游戏对象

1. 在 Unity 中创建空 GameObject（或选择现有对象）
2. 添加 `SpriteRenderer` 组件
3. 添加 `PolygonCollider2D` 组件
4. 添加 `CollisionImageItem` 组件

### 2. 配置 Inspector

在 `CollisionImageItem` 组件中设置：

```
┌─────────────────────────────────────────┐
│ Collision Image Item                    │
├─────────────────────────────────────────┤
│ ▼ 文件设置                              │
│   File Name: [platform_collision    ]   │ ← 文件名（不含扩展名）
│   Source Sprite: [platform_sprite   ]   │ ← 初始图片模板
├─────────────────────────────────────────┤
│ ▼ 碰撞设置                              │
│   Threshold: [128  ]                    │ ← 深浅色阈值 (0-255)
│   Simplification: [2.0]                 │ ← 多边形简化程度 (0-10)
├─────────────────────────────────────────┤
│ ▼ 调试                                  │
│   ☐ Show Debug Info                     │
│   Contour Points: 0                     │
└─────────────────────────────────────────┘
```

### 3. 参数说明

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| **File Name** | 生成的 PNG 文件名 | 唯一名称 |
| **Source Sprite** | 初始图片模板 | 需要开启 Read/Write |
| **Threshold** | 深浅色分界值 | 128（<128 为深色=碰撞区域） |
| **Simplification** | 碰撞箱简化程度 | 2（值越大越简化，性能越好） |

### 4. 运行游戏

- 首次运行时，会自动从 `Source Sprite` 生成 PNG 文件到：
  `level/{levelIndex}/{fileName}.png`
- 根据图片中的深色区域生成碰撞箱

### 5. 玩家修改碰撞箱

1. 找到文件：`level/{levelIndex}/{fileName}.png`
2. 用图片编辑器（如 Photoshop、Paint.NET）打开
3. 修改深色区域（RGB 值 < threshold）
4. 保存文件
5. 重新运行游戏，碰撞箱自动更新

## 图片规则

### 深色区域 = 碰撞区域
- **深色**：RGB 平均值 < Threshold（默认 128）→ 生成碰撞
- **浅色**：RGB 平均值 >= Threshold → 无碰撞

### 示例图片

```
████████████  ← 深色区域（碰撞）
██░░░░░░░░██  ← 浅色区域（无碰撞）
██░░░░░░░░██
████████████
```

### 推荐配色
- **碰撞区域**：深灰色 (RGB: 50, 50, 50) 或黑色
- **无碰撞区域**：浅灰色 (RGB: 200, 200, 200) 或白色

## 注意事项

### 1. Sprite 设置
`Source Sprite` 必须在 Unity 中开启 **Read/Write Enabled**：
- 选择 Sprite 资源
- 在 Inspector 中点击 "Advanced"
- 勾选 "Read/Write Enabled"
- 点击 Apply

### 2. 图片尺寸
- 如果玩家修改了图片尺寸，GameObject 会自动缩放以保持显示大小一致
- 建议保持原始尺寸以避免缩放问题

### 3. 多个深色区域
- 多个分离的深色区域会合并为一个碰撞箱
- 如果需要有独立碰撞箱，创建多个 GameObject

### 4. 性能考虑
- 扫描间隔：0.5 秒（与 LevelFileManager 一致）
- 轮廓点过多时，增加 `Simplification` 值
- 建议图片尺寸不要过大（推荐 512x512 以内）

## 文件结构

```
项目根目录/
├── level/
│   └── 1/                          ← 关卡 1 文件夹
│       ├── platform_collision.png  ← 碰撞箱图片
│       ├── enemy1.txt
│       └── trap1.txt
└── Assets/
    └── Script/
        └── FileController/
            ├── CollisionImageItem.cs
            └── LevelFileManager.cs
```

## 常见问题

### Q: 碰撞箱不更新？
A: 确保：
1. 文件已保存
2. 文件名正确
3. 图片中有足够的深色区域
4. 检查 Console 是否有错误

### Q: 碰撞箱形状不准确？
A: 尝试：
1. 降低 `Simplification` 值（更精确）
2. 使用更清晰的深浅对比
3. 增加图片分辨率

### Q: 游戏报错 "Source Sprite is null"？
A: 在 Inspector 中拖拽 Sprite 资源到 `Source Sprite` 字段

### Q: 如何禁用碰撞箱？
A: 将图片全部填充为浅色（RGB > 128），碰撞箱会自动清空

## 代码扩展

### 添加新的图片处理逻辑

在 `CollisionImageItem.cs` 的 `ProcessImageToCollision()` 方法中修改：

```csharp
// 当前流程：
// 1. 读取像素 → 2. 转为灰度 → 3. 阈值处理 → 4. 查找轮廓 → 5. 生成碰撞箱

// 可以添加：
// - 颜色过滤（特定颜色 = 碰撞）
// - 边缘检测
// - 形态学操作（膨胀/腐蚀）
```

### 修改扫描频率

在 `LevelFileManager.cs` 中修改：
```csharp
[SerializeField] private float checkInterval = 0.5f; // 修改此值
```

## 技术细节

- **OpenCV 版本**: OpenCvSharp
- **碰撞类型**: 凹包（Concave）
- **坐标系统**: Unity 2D（中心为原点）
- **文件格式**: PNG（无损压缩）
