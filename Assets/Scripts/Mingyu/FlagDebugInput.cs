using UnityEngine;

public class FlagDebugInput : MonoBehaviour
{
    private suin_FlagHub hub;

    [Header("사용할 키 설정")]
    public bool useMoveSlightKey  = true;
    public bool usePlayerSoundKey = true;
    public bool useWaterSoundKey  = true;
    public bool useLightToggleKey = true;

    [Header("키 바인딩")]
    public KeyCode moveSlightKey  = KeyCode.Alpha1; // 예: 1번 키
    public KeyCode playerSoundKey = KeyCode.Alpha2; // 예: 2번 키
    public KeyCode waterSoundKey  = KeyCode.Alpha3; // 예: 3번 키
    public KeyCode lightToggleKey = KeyCode.Alpha4; // 예: 4번 키

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

        // 1) MoveSlight 플래그
        if (useMoveSlightKey && Input.GetKeyDown(moveSlightKey))
        {
            Debug.Log($"[FlagDebug] MoveSlightFlag 트리거 ({moveSlightKey} 키)");
            hub.SetMoveSlightFlag(true);
        }

        // 2) PlayerSound 플래그
        if (usePlayerSoundKey && Input.GetKeyDown(playerSoundKey))
        {
            Debug.Log($"[FlagDebug] PlayerSoundFlag 트리거 ({playerSoundKey} 키)");
            hub.SetPlayerSoundFlag(true);
        }

        // 3) WaterSound 플래그
        if (useWaterSoundKey && Input.GetKeyDown(waterSoundKey))
        {
            Debug.Log($"[FlagDebug] WaterSoundFlag 트리거 ({waterSoundKey} 키)");
            hub.SetWaterSoundFlag(true);
        }

        // 4) Light On/Off 토글
        if (useLightToggleKey && Input.GetKeyDown(lightToggleKey))
        {
            bool next = !hub.LightOn;
            Debug.Log($"[FlagDebug] LightState 토글 → {(next ? "On" : "Off")} ({lightToggleKey} 키)");
            hub.SetLightState(next);
        }
    }
}
