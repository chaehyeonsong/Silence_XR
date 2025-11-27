using UnityEngine;

public class FlagDebugInput : MonoBehaviour
{
    private suin_FlagHub hub;

    void Start()
    {
        hub = suin_FlagHub.instance;
        if (hub == null)
        {
            Debug.LogError("[FlagDebugInput] suin_FlagHub.instance가 없습니다. FlagHub 프리팹이 씬에 있는지 확인하세요.");
        }
    }

    void Update()
    {
        if (hub == null) return;

        // 1: MoveSlight 플래그
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[FlagDebug] MoveSlightFlag 트리거 (1 키)");
            hub.SetMoveSlightFlag(true);
        }

        // 2: PlayerSound 플래그
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[FlagDebug] PlayerSoundFlag 트리거 (2 키)");
            hub.SetPlayerSoundFlag(true);
        }

        // 3: WaterSound 플래그
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("[FlagDebug] WaterSoundFlag 트리거 (3 키)");
            hub.SetWaterSoundFlag(true);
        }

        // 4: Light On/Off 토글
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            bool next = !hub.LightOn;
            Debug.Log($"[FlagDebug] LightState 토글 → {(next ? "On" : "Off")} (4 키)");
            hub.SetLightState(next);
        }
    }
}
