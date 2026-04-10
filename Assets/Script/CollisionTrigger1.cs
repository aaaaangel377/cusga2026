using UnityEngine;
using UnityEngine.Events;

public class CollisionTrigger1 : MonoBehaviour
{
    public UnityEvent onBallReachHole;
    [SerializeField] string targetTag = "Player";
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag(targetTag))
        {
            onBallReachHole?.Invoke();
        }
    }



}