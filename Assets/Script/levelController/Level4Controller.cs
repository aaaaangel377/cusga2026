using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Level4Controller : MonoBehaviour
{
    LevelFileManager levelFileManager4;
    bool isSetcop = false;
    // Start is called before the first frame update
    void Start()
    {
        levelFileManager4 = GetComponent<LevelFileManager>();
       isSetcop = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (!isSetcop)
        {
            string filePath = Path.Combine(levelFileManager4.GetFolderPath(), "ball - 副本.cop");
            File.WriteAllText(filePath, ""); // 创建空文件
            isSetcop = true;
        }
    }
}
