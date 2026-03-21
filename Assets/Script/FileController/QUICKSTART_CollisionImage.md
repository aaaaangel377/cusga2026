# CollisionImageItem 快速开始示例

## 场景设置步骤

### 1. 创建平台对象

```
Hierarchy 面板:
右键 → Create Empty
重命名为 "CollisionPlatform"
```

### 2. 添加必要组件

**添加 SpriteRenderer:**
```
Inspector → Add Component → SpriteRenderer
Sprite: [拖入你的平台图片]
```

**添加 PolygonCollider2D:**
```
Inspector → Add Component → PolygonCollider2D
```

**添加 CollisionImageItem:**
```
Inspector → Add Component → CollisionImageItem
```

### 3. 配置 CollisionImageItem

```
File Name: "platform1"
Source Sprite: [拖入与 SpriteRenderer 相同的图片]
Threshold: 128
Simplification: 2
Show Debug Info: ✓ (调试用)
```

### 4. 设置图片资源

**重要**: Source Sprite 必须开启 Read/Write：
```
Project 面板:
1. 选择图片资源
2. Inspector 中展开 "Advanced"
3. 勾选 "Read/Write Enabled"
4. 点击 Apply
```

### 5. 运行游戏

```
运行后:
- 自动生成文件：level/1/platform1.png
- 根据图片深色区域生成碰撞箱
- 玩家可以用图片编辑器修改 platform1.png
```

---

## 图片编辑示例

### 使用 Paint.NET 编辑

1. 打开 `level/1/platform1.png`
2. 用画笔工具绘制深色区域
3. 保存（Ctrl+S）
4. 重新运行游戏

### 推荐颜色值

| 区域类型 | RGB 值 | 说明 |
|---------|--------|------|
| 碰撞区域 | (50, 50, 50) | 深灰色，肯定 < 128 |
| 无碰撞 | (200, 200, 200) | 浅灰色，肯定 > 128 |
| 半透明碰撞 | (100, 100, 100) | 中灰色，也 < 128 |

### 示例图案

```
创建 L 形平台:
████████
█
█
████

创建带洞的平台:
█████████
█░░░░░█
█░░░░░█
█████████
(█=碰撞，░=无碰撞)
```

---

## 代码验证

### 检查清单

- [ ] CollisionImageItem.cs 已创建
- [ ] LevelFileManager.cs 已修改
- [ ] Assembly-CSharp.csproj 已更新
- [ ] 编译无错误

### 测试步骤

1. **首次运行测试**:
   - 运行游戏
   - 检查 `level/1/platform1.png` 是否生成
   - 检查碰撞箱是否生成

2. **文件修改测试**:
   - 用图片编辑器打开 PNG
   - 添加一个深色圆点
   - 保存
   - 重新运行
   - 碰撞箱应该包含新圆点区域

3. **尺寸变化测试**:
   - 修改 PNG 尺寸为 2 倍
   - 保存
   - 运行游戏
   - GameObject 应该自动缩放，显示大小不变

---

## 常见问题排查

### 问题 1: 没有生成 PNG 文件

**检查**:
- Source Sprite 是否赋值
- Source Sprite 是否开启 Read/Write
- 检查 Console 是否有错误

**解决**:
```
1. 选择图片资源
2. Inspector → Advanced → Read/Write Enabled ✓
3. Apply
4. 重新运行
```

### 问题 2: 碰撞箱为空

**检查**:
- 图片中是否有足够的深色区域
- Threshold 值是否合适
- 检查 Console 日志

**解决**:
```
1. 降低 Threshold (如 100)
2. 确保图片中有 RGB < Threshold 的区域
3. 降低 Simplification (如 0.5)
```

### 问题 3: 碰撞箱形状不对

**检查**:
- Simplification 值是否太大
- 图片分辨率是否太低

**解决**:
```
1. 降低 Simplification (如 0.5)
2. 使用更高分辨率的图片
3. 确保深浅对比明显
```

### 问题 4: 修改图片后不更新

**检查**:
- 文件是否真的保存了
- 文件名是否正确
- LevelFileManager 是否运行

**解决**:
```
1. 确认文件保存时间戳变化
2. 检查 Console 中的日志
3. 确认 GameObject 在场景中激活
```

---

## 性能优化建议

### 图片尺寸
- 推荐：256x256 或 512x512
- 最大：1024x1024
- 过大影响处理速度

### Simplification 设置
| 值 | 效果 | 适用场景 |
|----|------|---------|
| 0-1 | 高精度 | 复杂形状，性能充足 |
| 2-3 | 平衡 | 一般平台 (推荐) |
| 5-10 | 低精度 | 简单形状，性能紧张 |

### 扫描频率
在 LevelFileManager 中修改：
```csharp
[SerializeField] private float checkInterval = 0.5f;
// 改为 1.0f 可降低 CPU 占用
```

---

## 扩展功能想法

### 1. 颜色编码碰撞类型
```csharp
// 不同颜色 = 不同物理属性
红色区域 = 危险碰撞
蓝色区域 = 弹性碰撞
绿色区域 = 普通碰撞
```

### 2. 渐变透明度
```csharp
// 根据灰度值设置不同的碰撞强度
深黑 = 完全碰撞
中灰 = 半碰撞
浅灰 = 无碰撞
```

### 3. 多层碰撞
```csharp
// 使用不同通道
R 通道 = 物理碰撞
G 通道 = 触发区域
B 通道 = 特殊效果
```

---

## 技术支援

遇到问题请检查:
1. Console 错误日志
2. 文件路径是否正确
3. 图片格式是否为 PNG
4. Unity 版本兼容性

相关文件:
- `Assets/Script/FileController/CollisionImageItem.cs`
- `Assets/Script/FileController/LevelFileManager.cs`
- `Assets/Script/FileController/README_CollisionImage.md`
