using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Level2Tip : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Text tip;
    [SerializeField] LevelFileManager levelFileManager;
    
    void Start()
    {
        
        tip.text = "Warning£º´íÎó·¢ÉúÓÚ£º\r" + levelFileManager.GetParentFolderPath() + "\n//\r" + "\nplease try tofinditttttttttttt_";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
