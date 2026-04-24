using UnityEngine;

public class PopUpTester : MonoBehaviour
{
    void Start()
    {

    }



    void Update()
    {
        // ∞¥ø’∏Òº¸≤‚ ‘
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UnlockNotification.Instance.ShowUnlockNotification("≤‚ ‘");
        }
    }
}