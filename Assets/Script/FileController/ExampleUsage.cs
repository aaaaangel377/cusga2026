// 示例：创建一个同时具有位置、旋转、复制功能的物体
// 
// 步骤 1: 在 Unity 中选择游戏对象
// 步骤 2: 添加 AdvancedItemController 组件
// 步骤 3: 配置如下:
//
// ┌─────────────────────────────────────────┐
// │ Advanced Item Controller                │
// ├─────────────────────────────────────────┤
// │ File Name: [移动旋转平台]               │
// │ Custom Content: []                      │
// │ Target Object: [Self]                   │
// ├─────────────────────────────────────────┤
// │ 功能开关：                              │
// │ ☑ Enable Position      (位置控制)      │
// │ ☑ Enable Rotation      (旋转控制)      │
// │ ☑ Enable Spawn         (复制生成)      │
// │ ☐ Enable Critical      (删除重开)      │
// │ ☑ Enable Visibility    (删除消失)      │
// └─────────────────────────────────────────┘
//
// 步骤 4: 运行游戏，会自动创建文件：
// level/{levelIndex}/移动旋转平台.txt
//
// 步骤 5: 编辑文件内容：
// 位置：4,4
// 旋转：90.0
//
// 步骤 6: 复制文件创建新对象：
// 复制 "移动旋转平台.txt" 为 "移动旋转平台 - 副本.txt"
// 修改内容:
// 位置：6,3
// 旋转：45.0
//
// 新对象会自动生成在场景中！

// ============================================================
// 扩展示例：添加缩放功能
// ============================================================

// 1. 创建 ScaleProcessor.cs:

/*
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
    
    public override void OnFileCreated(string folderPath, string fileName, GameObject target)
    {
        Vector3 scale = target.transform.localScale;
        string content = $"缩放：{scale.x},{scale.y}";
        
        string fullPath = System.IO.Path.Combine(folderPath, $"{fileName}.txt");
        if (!System.IO.File.Exists(fullPath))
        {
            System.IO.File.WriteAllText(fullPath, content);
        }
        else
        {
            string existing = System.IO.File.ReadAllText(fullPath);
            if (!existing.Contains("缩放："))
            {
                System.IO.File.AppendAllText(fullPath, "\n" + content);
            }
        }
    }
    
    public override void OnFileDeleted(GameObject target) { }
    public override void OnFileCopied(string newFileName, string content, GameObject target, AdvancedItemController controller)
    {
        OnFileUpdated(content, target);
    }
    public override string CreateDefaultContent(GameObject target)
    {
        Vector3 scale = target.transform.localScale;
        return $"缩放：{scale.x},{scale.y}";
    }
}
*/

// 2. 在 AdvancedItemController.cs 中添加:
// [SerializeField] private bool enableScale = false;
// 
// if (enableScale)
// {
//     _processors.Add(new ScaleProcessor());
// }

// 3. 使用格式:
// 位置：4,4
// 旋转：90.0
