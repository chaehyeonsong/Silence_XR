using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> zombieSpawnPoints;   // 좀비 스폰 위치들
    public List<Transform> spiderSpawnPoints;   // 스파이더 스폰 위치들 (vent 등)

    [Header("Prefabs")]
    public GameObject zombiePrefab;
    public GameObject spiderPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 10f;           // 10초마다 시도
    [Range(0f, 1f)]
    public float zombieSpawnChance = 0.5f;      // 0.7이면 70% 좀비, 30% 스파이더

    [Header("Spawn Probability Cycle")]
    [Range(0f, 1f)] public float firstSpawnChance  = 0.5f;   // 첫 시도: 50%
    [Range(0f, 1f)] public float secondSpawnChance = 0.75f;  // 두 번째 시도: 75%
    [Range(0f, 1f)] public float thirdSpawnChance  = 1.0f;   // 세 번째 이후: 100%

    [Header("Targets")]
    public Transform zombieTargetPoint;         // 모든 좀비가 달려갈 타겟
    public Transform spiderTargetPoint;         // 스파이더가 향할 타겟

    [Header("Monster Limit")]
    public int maxMonsters = 4;                 // 최대 몬스터 수 (좀비+스파이더 합산)

    [Header("Spider Env (씬 오브젝트 참조)")]
    public MeshRenderer spiderRoofMesh;         // 거미가 돌아다닐 천장 MeshRenderer
    public LayerMask spiderCeilingLayer;        // 거미가 붙을 천장 레이어
    public LayerMask spiderGroundLayer;         // 거미가 떨어져서 닿을 바닥 레이어

    [Header("Zombie Env (씬 오브젝트 참조)")]
    public MeshRenderer zombieWanderAreaMesh;   // 좀비가 배회할 바닥 영역 MeshRenderer (있으면)

    // 현재 살아있는 몬스터들 추적용 (좀비 + 스파이더)
    private List<GameObject> activeMonsters = new List<GameObject>();

    // 스폰 확률 사이클 상태
    private float currentSpawnChance;
    private int failedSpawnStreak = 0;
    private bool spawnCycleActive = false;

    void Start()
    {
        currentSpawnChance = firstSpawnChance;
        failedSpawnStreak = 0;
        spawnCycleActive = false;

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // null 정리
            activeMonsters.RemoveAll(m => m == null);

            // 최대 마리 수 체크 (좀비가 없더라도 너무 많으면 스폰 안 함)
            if (activeMonsters.Count >= maxMonsters)
            {
                Debug.Log($"[Spawner] Max monsters reached ({activeMonsters.Count}/{maxMonsters}), 스폰 스킵");
                continue;
            }

            // ✅ 현재 좀비가 한 마리라도 있는지 체크
            bool hasZombie = activeMonsters.Exists(
                m => m != null && m.GetComponent<ZombieNavTarget>() != null
            );

            if (hasZombie)
            {
                // 좀비가 있는 동안은 스폰 사이클 강제 중단 + 리셋
                if (spawnCycleActive)
                {
                    Debug.Log("[Spawner] 좀비가 살아있어서 스폰 사이클 리셋");
                }
                spawnCycleActive = false;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                continue;
            }

            // 여기까지 왔다는 건 "좀비는 0마리" 상태
            // 스폰 사이클이 비활성 상태였다면 새로 시작
            if (!spawnCycleActive)
            {
                spawnCycleActive = true;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                Debug.Log("[Spawner] 스폰 사이클 시작 (첫 시도)");
            }

            // 확률에 따라 스폰 시도
            TrySpawnMonsterWithChance();
        }
    }

    void TrySpawnMonsterWithChance()
    {
        float roll = Random.value;
        bool pass = roll <= currentSpawnChance;

        Debug.Log(
            $"[Spawner] CycleRoll={roll:F3}, " +
            $"currentSpawnChance={currentSpawnChance:F3}, " +
            $"pass={(pass ? "YES" : "NO")}"
        );

        if (pass)
        {
            // 실제 스폰 시도 (실패할 수도 있음: 스폰 포인트 없음 등)
            bool spawned = SpawnRandomMonster();

            if (spawned)
            {
                // 한 번이라도 스폰 성공하면: 이번 사이클 종료
                Debug.Log("[Spawner] ▶ 스폰 성공 → 사이클 종료, 다음에는 좀비가 0마리 될 때까지 대기");
                spawnCycleActive = false;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                // 이후에는 "좀비가 다시 0마리"가 될 때까지 스폰Routine에서 다시 사이클 시작 안 함
            }
            else
            {
                // 스폰 시도 자체가 실패했다면(스폰 포인트 없음 등) → 실패로 간주하고 확률 단계만 올려줌
                Debug.Log("[Spawner] 스폰 시도는 했지만 실패 → 확률 단계 업");
                IncreaseSpawnChanceStep();
            }
        }
        else
        {
            // 확률에 실패한 경우 → 다음 단계로 확률 업
            Debug.Log("[Spawner] CycleRoll 실패 → 확률 단계 업");
            IncreaseSpawnChanceStep();
        }
    }

    void IncreaseSpawnChanceStep()
    {
        failedSpawnStreak++;

        if (failedSpawnStreak == 1)
        {
            currentSpawnChance = secondSpawnChance;   // 75%
            Debug.Log($"[Spawner] 스폰 확률 단계 2로 상승 → {currentSpawnChance:F3}");
        }
        else
        {
            currentSpawnChance = thirdSpawnChance;    // 100%
            Debug.Log($"[Spawner] 스폰 확률 단계 3(최대) → {currentSpawnChance:F3}");
        }
    }

    /// <summary>
    /// 좀비/스파이더 중 하나를 스폰 시도하고,
    /// 실제로 인스턴스를 만들면 true, 아니면 false 리턴.
    /// </summary>
    bool SpawnRandomMonster()
    {
        // 최대 마리 수 다시 한 번 방어적 체크
        activeMonsters.RemoveAll(m => m == null);
        if (activeMonsters.Count >= maxMonsters)
        {
            Debug.Log($"[Spawner] 🐾 Max monsters reached ({activeMonsters.Count}/{maxMonsters}), 스폰 취소");
            return false;
        }

        // 타입 결정용 랜덤
        float typeRoll = Random.value;
        bool spawnZombie = (typeRoll < zombieSpawnChance);

        Debug.Log(
            $"[Spawner] TypeRoll={typeRoll:F3}, " +
            $"zombieSpawnChance={zombieSpawnChance:F3} → " +
            $"{(spawnZombie ? "Zombie" : "Spider")} 선택"
        );

        if (spawnZombie)
        {
            return SpawnZombie();
        }
        else
        {
            return SpawnSpider();
        }
    }

    bool SpawnZombie()
    {
        if (zombieSpawnPoints == null || zombieSpawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ [Spawner] No zombie spawn points assigned to Spawner.");
            return false;
        }

        Transform point = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Count)];
        GameObject monster = Instantiate(zombiePrefab, point.position, point.rotation);

        // 리스트에 등록
        activeMonsters.Add(monster);

        // 좀비에게 타겟 포인트 + 환경 정보 할당
        ZombieNavTarget mover = monster.GetComponent<ZombieNavTarget>();
        if (mover != null)
        {
            mover.SetTarget(zombieTargetPoint);

            // 🔹 좀비 배회 영역 MeshRenderer 주입
            if (zombieWanderAreaMesh != null)
            {
                mover.wanderAreaMesh = zombieWanderAreaMesh;
            }

            // 🔹 자기 스폰 포인트 기억 (15초 플래그 없을 때 복귀용)
            mover.spawnPoint = point;
        }
        else
        {
            Debug.LogWarning($"⚠️ [Spawner] Spawned zombie '{monster.name}' has no ZombieNavTarget component.");
        }

        Debug.Log($"🧟 [Spawner] Spawned Zombie at: {point.name} (현재 몬스터 수: {activeMonsters.Count})");
        return true;
    }

    bool SpawnSpider()
    {
        if (spiderSpawnPoints == null || spiderSpawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ [Spawner] No spider spawn points assigned to Spawner.");
            return false;
        }

        Transform point = spiderSpawnPoints[Random.Range(0, spiderSpawnPoints.Count)];
        GameObject spider = Instantiate(spiderPrefab, point.position, point.rotation);

        // 리스트에 등록
        activeMonsters.Add(spider);

        // 스파이더에게 타겟 포인트 + 환경 정보 할당 (SpiderCeilingFollowTarget 사용)
        SpiderCeilingFollowTarget ctrl = spider.GetComponent<SpiderCeilingFollowTarget>();
        if (ctrl != null)
        {
            ctrl.SetTarget(spiderTargetPoint);

            // 🔹 씬의 천장/레이어 정보 주입
            if (spiderRoofMesh != null)
            {
                ctrl.roofMesh = spiderRoofMesh;
            }
            ctrl.ceilingLayer = spiderCeilingLayer;
            ctrl.groundLayer  = spiderGroundLayer;

            // 🔹 자기 스폰 포인트 기억 (15초 플래그 없을 때 복귀용)
            ctrl.spawnPoint = point;
        }
        else
        {
            Debug.LogWarning($"⚠️ [Spawner] Spawned spider '{spider.name}' has no SpiderCeilingFollowTarget component.");
        }

        Debug.Log($"🕷️ [Spawner] Spawned Spider at: {point.name} (현재 몬스터 수: {activeMonsters.Count})");
        return true;
    }
}
