using UnityEngine;

public static class FailSoundThrottle
{
    private static float _lastFailSoundTime = 0f;
    private const float COOLDOWN = 1.0f;
    private static bool _hasPlayedForCurrentError = false;
    
    public static bool CanPlay()
    {
        if (_hasPlayedForCurrentError) return false;
        return Time.time - _lastFailSoundTime >= COOLDOWN;
    }
    
    public static void MarkPlayed()
    {
        _lastFailSoundTime = Time.time;
        _hasPlayedForCurrentError = true;
    }
    
    public static void Reset()
    {
        _hasPlayedForCurrentError = false;
    }
}
