using System.Collections;
using UnityEngine;

public class TwoWayPlatform : MonoBehaviour
{
    private GameObject currentPlatform;
   private Collider2D platformCollider;
   [SerializeField] private CapsuleCollider2D playerCollider;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentPlatform = collision.gameObject;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentPlatform = null;
        }
    }
    private IEnumerator DisableCollisionTemporarily()
    {
        if (currentPlatform != null)
        {
            platformCollider = currentPlatform.GetComponent<Collider2D>();
            if (platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
                yield return new WaitForSeconds(0.5f); // Adjust the duration as needed
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && currentPlatform != null)
        {
            StartCoroutine(DisableCollisionTemporarily());
        }
    }
}
