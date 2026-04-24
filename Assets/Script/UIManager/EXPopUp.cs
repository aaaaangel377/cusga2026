using UnityEngine;
using static LevelUnlockSystem;
public class EXPopUp : MonoBehaviour
{
    public LevelUnlockSystem levelUnlockSystem;
    void Start()
    {
        LevelUnlockSystem.SaveData saveData1 = levelUnlockSystem.LoadSave();

        if (!string.IsNullOrEmpty(saveData1.pendingExLevel))
        {
            string exLevel = saveData1.pendingExLevel;
            Debug.Log("×¼±¸µ¯´°");
            if (!saveData1.passedLevels.Contains(exLevel))
            {
                // µ¯´°
                UnlockNotification notification = FindObjectOfType<UnlockNotification>();
                if (notification != null)
                {
                    notification.ShowUnlockNotification(exLevel);
                }

                levelUnlockSystem.SaveLevelPassed(exLevel);
                saveData1 = levelUnlockSystem.LoadSave();
                levelUnlockSystem.UnlockButtonsFromSave(saveData1);
            }

        }
    }
}