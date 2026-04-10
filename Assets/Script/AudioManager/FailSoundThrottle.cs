using UnityEngine;

public static class FailSoundThrottle
{
    private static float _lastFailSoundTime = 0f;
    private const float COOLDOWN = 1.0f;
    
    public static bool CanPlay()
    {
        return Time.time - _lastFailSoundTime >= COOLDOWN;
    }
    
    public static void MarkPlayed()
    {
        _lastFailSoundTime = Time.time;
    }
}
