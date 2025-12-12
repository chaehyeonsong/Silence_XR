using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class KillFlagZone : MonoBehaviour
{
    [Header("어떤 플래그에 반응할지")]
    public bool useMoveSlightFlag  = true;
    public bool usePlayerSoundFlag = true;
    public bool useWaterSoundFlag  = false;

    [Header("플레이어 타겟 (targetpoint)")]
    public Transform playerTargetPoint;

    private suin_FlagHub hub;
    private BoxCollider box;
    private bool subscribed = false;

    // ... (Awake, OnEnable, Unsubscribe 등 위쪽 코드는 기존과 동일) ...

    void Update() { if (!subscribed) TrySubscribe(); }
    void TrySubscribe() { /* 기존과 동일 */ if (hub == null) hub = suin_FlagHub.instance; if (hub == null) return; if (subscribed) return; if (useMoveSlightFlag) hub.OnMoveSlightFlag += OnFlag; if (usePlayerSoundFlag) hub.OnPlayerSoundFlag += OnFlag; if (useWaterSoundFlag) hub.OnWaterSoundFlag += OnFlag; subscribed = true; }
    void Unsubscribe() { /* 기존과 동일 */ if (!subscribed || hub == null) return; if (useMoveSlightFlag) hub.OnMoveSlightFlag -= OnFlag; if (usePlayerSoundFlag) hub.OnPlayerSoundFlag -= OnFlag; if (useWaterSoundFlag) hub.OnWaterSoundFlag -= OnFlag; subscribed = false; }

    void OnFlag(bool v)
    {
        if (!v) return;
        if (hub == null) hub = suin_FlagHub.instance;

        // 1. 영역 내 몬스터를 찾아서 공격 명령(ForceLock)만 내림
        bool anyMonsterFound = HandleMonstersInZoneXZ();

        // [수정됨] 여기서 바로 게임오버를 시키지 않습니다!
        // 거미가 떨어져서 도착하면 그때 거미가 직접 신호를 보냅니다.
        
        if (anyMonsterFound && hub != null)
        {
             // hub.TriggerPlayerKillFlag(); // <--- 이거 삭제!!
             Debug.Log($"[KillFlagZone] {name} 몬스터 감지 → 공격 시작 명령만 내림 (게임오버는 몬스터가 처리)");
        }
        
    }

    bool HandleMonstersInZoneXZ()
    {
        if (box == null) box = GetComponent<BoxCollider>();
        if (playerTargetPoint == null) return false;

        Bounds b = box.bounds;
        bool found = false;

        // 좀비 처리 (좀비는 닿으면 게임오버? 혹은 애니메이션 후? 일단 여기서는 락온만)
        ZombieNavTarget[] zombies = FindObjectsOfType<ZombieNavTarget>();
        foreach (var z in zombies)
        {
            if (z == null) continue;
            if (IsInsideXZ(z.transform.position, b))
            {
                z.ForceLockToTarget(playerTargetPoint);
                found = true;
            }
        }

        // 거미 처리
        SpiderCeilingFollowTarget[] spiders = FindObjectsOfType<SpiderCeilingFollowTarget>();
        foreach (var s in spiders)
        {
            if (s == null) continue;
            if (IsInsideXZ(s.transform.position, b))
            {
                s.ForceLockToTarget(playerTargetPoint); // -> 이걸 하면 거미가 떨어지기 시작함
                Debug.Log($"🕷️ [KillFlagZone] 거미 발견! 공격 명령 전달");
                found = true;
            }
        }

        return found;
    }

    bool IsInsideXZ(Vector3 pos, Bounds b)
    {
        return (pos.x >= b.min.x && pos.x <= b.max.x &&
                pos.z >= b.min.z && pos.z <= b.max.z);
    }
}