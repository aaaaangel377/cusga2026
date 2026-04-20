using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;

public class LevelUnlockSystem : MonoBehaviour
{
    public GameObject[] levelButtons;  
    public string[] num;
    public Toggle hintsToggle;

    [System.Serializable]
    public class SaveData
    {
        public List<string> passedLevels = new List<string>();
        public bool hintsEnabled = true;
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

        // 3. 设置提示开关状态
        SetHintsToggle(saveData);
    }

    // 设置提示开关
    void SetHintsToggle(SaveData saveData)
    {
        if (hintsToggle != null)
        {
            // 从存档加载开关状态
            hintsToggle.isOn = saveData.hintsEnabled;

            // 添加状态改变监听
            hintsToggle.onValueChanged.AddListener(OnHintsToggleChanged);
        }
    }

    // 提示开关状态改变回调
    void OnHintsToggleChanged(bool isOn)
    {
        // 加载存档
        SaveData saveData = LoadSave();

        // 更新提示开关状态
        saveData.hintsEnabled = isOn;

        // 保存
        SaveToFile(saveData);
    }

    // 新增：获取提示开关状态（供其他脚本调用）
    public bool AreHintsEnabled()
    {
        SaveData saveData = LoadSave();
        return saveData.hintsEnabled;
    }

    // 新增：设置提示开关状态（供其他脚本调用）
    public void SetHintsEnabled(bool enabled)
    {
        if (hintsToggle != null)
        {
            hintsToggle.isOn = enabled;
        }
        else
        {
            // 如果 Toggle 未设置，直接修改存档
            SaveData saveData = LoadSave();
            saveData.hintsEnabled = enabled;
            SaveToFile(saveData);
        }
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
        newData.hintsEnabled = true; // 默认开启提示
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