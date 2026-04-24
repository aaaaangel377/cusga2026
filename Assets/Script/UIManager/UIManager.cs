using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    [SerializeField] Text levelName;
    [SerializeField] LevelFileManager levelFileManager;
    // Start is called before the first frame update
    void Start()
    {
        levelName.text = levelFileManager.GetLevelIndex();
    }


    public void PauseGame()
    {
        Time.timeScale = 0;
    }
    public void ResumeGame()
    {
        Time.timeScale = 1;
    }

}
