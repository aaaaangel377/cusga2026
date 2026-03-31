using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        if (Instance == null) Instance = this;
    }
    public void StartGame()
    {
        isPlaying = true;
        Debug.Log("��Ϸ��ʼ");
    }
    public void CompleteLevel()
    {
        if (!isPlaying) return;
        //UnityEditor.EditorApplication.isPlaying = false;
        //Debug.Log("��ϲ���أ�");
    }

    public void GameOver()
    {
        if (!isPlaying) return;
        //UnityEditor.EditorApplication.isPlaying = false;
        //Debug.Log("��Ϸ����");
        // ���������ʾUI
    }

    public void RestartGame()
    {
        LevelFileManager.shouldResetFiles = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    //��һ��
    public void gotoNextlevel()
    {
        LevelFileManager.shouldResetFiles = false;
        SceneManager.LoadScene(nextLevel);
        Debug.Log("下一关");
    }
    public AudioSource audioSource;
    public void end()
    {
        audioSource.Play();
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


    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
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
