using UnityEngine;
using UnityEngine.Events;

public class CollisionTrigger2 : MonoBehaviour
{
    public UnityEvent onBallReachHole;
    [SerializeField] string targetTag = "Player";
    
    void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.gameObject.CompareTag(targetTag))
        {
            onBallReachHole?.Invoke();
        }
    }

}