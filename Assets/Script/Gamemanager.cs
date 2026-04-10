using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class GameManager : MonoBehaviour
{
    public Vector3 startPosition1, startPosition2;
    public static GameManager Instance;

    public Image blackScreen;
    public float fadeDuration = 1.0f;

    public int nextLevel;
    [Header("��Ϸ״̬")]
    public bool isPlaying = true;

    void Awake()
    {
        Debug.Log("[GameManager] Awake() 执行，场景：" + gameObject.scene.name + ", Instance 是否为 null: " + (Instance == null));
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameManager] 设置为 Instance");
        }
        else if (Instance != this)
        {
            Debug.Log("[GameManager] 已存在 Instance，销毁当前对象：" + gameObject.name);
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        isPlaying = true;
        Debug.Log("Ϸʼ");
    }
    public void RestartGame()
    {
        LevelFileManager.shouldResetFiles = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    //��һ��
    public void gotoNextlevel()
    {
        AudioManager.Instance.PlayOneShotEffect("游戏过关音效", AudioManager.Instance.JumpVolume);
        LevelFileManager.shouldResetFiles = false;
        SceneManager.LoadScene(nextLevel);
        Debug.Log("下一关");
    }
    public AudioSource audioSource;
    public void end()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
    
    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public static void PlaySoundAt(AudioClip clip, Vector3 position)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }
    
    public void CompleteLevel()
    {
        if (!isPlaying) return;
        AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/10 - HappyRobot");
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, AudioManager.Instance.VictoryVolume);
        }
    }
    
    public void GameOver()
    {
        if (!isPlaying) return;
        AudioClip clip = Resources.Load<AudioClip>("Sounds/Effects/11 - Death");
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, AudioManager.Instance.GameOverVolume);
        }
    }


    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
        AudioManager.Instance.PlayOneShotEffect("游戏过关音效", AudioManager.Instance.JumpVolume);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        // 编辑器模式下停止运行
        UnityEditor.EditorApplication.isPlaying = false;
#else
// 打包后退出游戏
Application.Quit();
#endif
    }
}
