using UnityEngine;

public class CriticalItem : BasicItem
{
    protected override void OnFileDeleted()
    {
        _manager.ReloadScene();
    }
}
