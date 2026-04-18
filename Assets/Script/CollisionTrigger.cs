using UnityEngine;
using UnityEngine.Events;

public class CollisionTrigger : MonoBehaviour
{
    public UnityEvent onBallReachHole;
    [SerializeField] string targetTag = "Player";
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!AudioConfig.CanDeath)
        {
            return;
        }
        
        if (other.gameObject.CompareTag(targetTag))
        {
            onBallReachHole?.Invoke();
        }
    }

}