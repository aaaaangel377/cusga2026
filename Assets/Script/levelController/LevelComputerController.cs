using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

public class LevelComputerController : MonoBehaviour
{
    // Start is called before the first frame update
    LevelFileManager  levelFileManager;
    void Start()
    {
        levelFileManager = GetComponent<LevelFileManager>();
        StartCoroutine(DelayStart());
    }


    IEnumerator DelayStart()
    {
        yield return null;
        File.Delete(Path.Combine(levelFileManager.GetFolderPath(), "key.txt"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
