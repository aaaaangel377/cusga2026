using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class LevelUnlockSystem : MonoBehaviour
{
    public GameObject[] levelButtons;  
    public string[] num;               

    [System.Serializable]
    public class SaveData
    {
        public List<string> passedLevels = new List<string>();
    }

    private string saveFileName = "level_save.json";

    void Start()
    {
        // 0. 隐藏所有按钮
        HideAllButtons();

        // 1. 加载存档
        SaveData saveData = LoadSave();

        // 2. 解锁按钮
        UnlockButtonsFromSave(saveData);
    }

    // 新增：隐藏所有按钮
    void HideAllButtons()
    {
        foreach (GameObject button in levelButtons)
        {
            if (button != null)
            {
                button.SetActive(false);
            }
        }
    }

    // 加载存档
    SaveData LoadSave()
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources", saveFileName);

        if (File.Exists(resourcesPath))
        {
            string json = File.ReadAllText(resourcesPath);
            return JsonUtility.FromJson<SaveData>(json);
        }

        // 新游戏
        SaveData newData = new SaveData();
        if (num.Length > 0)
        {
            newData.passedLevels.Add(num[0]);  // 默认解锁第一关
        }
        SaveToFile(newData);
        return newData;
    }

    // 解锁按钮
    void UnlockButtonsFromSave(SaveData saveData)
    {
        // 遍历存档中的每一个已通过关卡
        foreach (string levelCode in saveData.passedLevels)
        {
            // 在num数组中找对应的索引
            for (int i = 0; i < num.Length; i++)
            {
                if (num[i] == levelCode && i < levelButtons.Length)
                {
                    if (levelButtons[i] != null)
                    {
                        levelButtons[i].SetActive(true);
                    }
                    break;
                }
            }
        }

        // 确保第一关总是显示
        if (num.Length > 0 && levelButtons.Length > 0 && levelButtons[0] != null)
        {
            levelButtons[0].SetActive(true);
        }
    }

    // ============ 给旗子调用的函数 ============

    public void SaveLevelPassed(string levelCode)
    {
        // 1. 加载现有存档
        SaveData saveData = LoadSave();

        // 2. 添加新记录
        if (!saveData.passedLevels.Contains(levelCode))
        {
            saveData.passedLevels.Add(levelCode);

            // 3. 保存
            SaveToFile(saveData);
        }
    }

    // 保存到文件
    void SaveToFile(SaveData saveData)
    {
        string filePath = Path.Combine(Application.dataPath, "Resources", saveFileName);
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, json);
    }
}