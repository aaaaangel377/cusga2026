using UnityEngine;

public class RegionTriggerForwarder : MonoBehaviour
{
    private FileRegionManager _regionManager;

    void Awake()
    {
        _regionManager = GetComponentInParent<FileRegionManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_regionManager != null)
        {
            _regionManager.OnObjectEnterRegion(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (_regionManager != null)
        {
            _regionManager.OnObjectExitRegion(other);
        }
    }
}
