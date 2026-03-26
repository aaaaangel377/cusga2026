using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Vector3 startPosition1, startPosition2;
    public static GameManager Instance;

    public Image blackScreen;
    public float fadeDuration = 1.0f;
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
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("��ϲ���أ�");
    }

    public void GameOver()
    {
        if (!isPlaying) return;
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("��Ϸ����");
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        Debug.Log("下一关");
    }
    public AudioSource audioSource;
    public void end()
    {
        audioSource.Play();
    }
}
