using System.Collections;
using System.Collections.Generic;
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
