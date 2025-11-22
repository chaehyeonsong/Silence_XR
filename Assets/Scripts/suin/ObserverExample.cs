using UnityEngine;

public class ObserverExample : MonoBehaviour
{
    suin_FlagHub hub;
    // Other 
    void OnEnable()
    {
        hub = suin_FlagHub.instance;

        if (hub == null)
            hub = FindObjectOfType<suin_FlagHub>(true);

        if (hub == null)
        {
            Debug.LogError("[ObserverExample] suin_FlagHub not found in scene.");
            return;
        }

        hub.OnWaterSoundFlag += HandleWater;
        hub.OnPlayerSoundFlag += HandlePlayerSound;
        hub.OnMoveSlightFlag += HandleMoveSlight;
        hub.OnLightStateChanged += HandleLight;

        HandleLight(hub.LightOn);
    }


    void OnDisable()
    {
        if (hub == null) return;

        hub.OnWaterSoundFlag -= HandleWater;
        hub.OnPlayerSoundFlag -= HandlePlayerSound;
        hub.OnMoveSlightFlag -= HandleMoveSlight;
        hub.OnLightStateChanged -= HandleLight;
    }


    void HandleWater(bool v) { Debug.Log("WaterSoundFlag fired"); }
    void HandlePlayerSound(bool v) { Debug.Log("PlayerSoundFlag fired"); }
    void HandleMoveSlight(bool v) { Debug.Log("MoveSlightFlag fired"); }

    void HandleLight(bool isOn)
    {
        Debug.Log("현재 Light 상태: " + (isOn ? "ON" : "OFF"));
        // 여기서 UI 업데이트, 다른 로직 트리거 등
    }
}