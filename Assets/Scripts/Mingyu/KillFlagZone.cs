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

    void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    void OnEnable() { TrySubscribe(); }
    void OnDisable() { Unsubscribe(); }

    void Update()
    {
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (hub == null) hub = suin_FlagHub.instance;
        if (hub == null) return;
        if (subscribed) return;

        if (useMoveSlightFlag) hub.OnMoveSlightFlag += OnFlag;
        if (usePlayerSoundFlag) hub.OnPlayerSoundFlag += OnFlag;
        if (useWaterSoundFlag) hub.OnWaterSoundFlag += OnFlag;

        subscribed = true;
        // Debug.Log($"[KillFlagZone] {name} 구독 완료");
    }

    void Unsubscribe()
    {
        if (!subscribed || hub == null) return;
        if (useMoveSlightFlag) hub.OnMoveSlightFlag -= OnFlag;
        if (usePlayerSoundFlag) hub.OnPlayerSoundFlag -= OnFlag;
        if (useWaterSoundFlag) hub.OnWaterSoundFlag -= OnFlag;
        subscribed = false;
    }

    void OnFlag(bool v)
    {
        if (!v) return;
        if (hub == null) hub = suin_FlagHub.instance;

        // 1. 영역 내 몬스터(좀비+거미)를 찾아서 '강제 타겟 고정'
        bool anyMonster = HandleMonstersInZoneXZ();

        // 2. 몬스터가 하나라도 있으면 글로벌 킬 플래그 발송
        if (anyMonster && hub != null)
        {
            Debug.Log($"[KillFlagZone] {name} 몬스터 감지 → PlayerKillFlag 발사!");
            hub.TriggerPlayerKillFlag();
        }
    }

    bool HandleMonstersInZoneXZ()
    {
        if (box == null) box = GetComponent<BoxCollider>();
        if (playerTargetPoint == null) return false;

        // BoxCollider의 월드 좌표 기준 Bounds
        Bounds b = box.bounds;
        
        bool found = false;

        // === 1) 좀비 찾기 (좀비도 높이 무시하고 XZ로만 체크 추천) ===
        ZombieNavTarget[] zombies = FindObjectsOfType<ZombieNavTarget>();
        foreach (var z in zombies)
        {
            if (z == null) continue;
            // 높이(Y) 무시하고 XZ 범위만 체크
            if (IsInsideXZ(z.transform.position, b))
            {
                z.ForceLockToTarget(playerTargetPoint);
                Debug.Log($"🧟 [KillFlagZone] 좀비 발견! ({z.name}) → 강제 고정");
                found = true;
            }
        }

        // === 2) 거미 찾기 (높이는 천장에 있으므로 반드시 Y 무시) ===
        SpiderCeilingFollowTarget[] spiders = FindObjectsOfType<SpiderCeilingFollowTarget>();
        foreach (var s in spiders)
        {
            if (s == null) continue;
            // 거미가 아무리 높아도 X, Z 좌표만 맞으면 감지됨
            if (IsInsideXZ(s.transform.position, b))
            {
                s.ForceLockToTarget(playerTargetPoint);
                Debug.Log($"🕷️ [KillFlagZone] 거미 발견! ({s.name}) → 강제 고정");
                found = true;
            }
        }

        return found;
    }

    // Y축(높이) 상관없이 X, Z가 박스 안에 있는지 검사하는 함수
    bool IsInsideXZ(Vector3 pos, Bounds b)
    {
        return (pos.x >= b.min.x && pos.x <= b.max.x &&
                pos.z >= b.min.z && pos.z <= b.max.z);
    }
}