using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> zombieSpawnPoints;
    public List<Transform> spiderSpawnPoints;

    [Header("Prefabs")]
    public GameObject zombiePrefab;
    public GameObject spiderPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 10f;
    [Range(0f, 1f)] public float zombieSpawnChance = 0.5f;

    [Header("Spawn Probability Cycle")]
    [Range(0f, 1f)] public float firstSpawnChance = 0.5f;
    [Range(0f, 1f)] public float secondSpawnChance = 0.75f;
    [Range(0f, 1f)] public float thirdSpawnChance = 1.0f;

    [Header("Targets")]
    public Transform zombieTargetPoint;
    public Transform spiderTargetPoint;

    [Header("Monster Limit")]
    public int maxMonsters = 4;

    [Header("Spider Env")]
    public MeshRenderer spiderRoofMesh;
    public LayerMask spiderCeilingLayer;
    public LayerMask spiderGroundLayer;

    [Header("Zombie Env")]
    public MeshRenderer zombieWanderAreaMesh;

    [Header("Audio Settings")]
    public AudioClip zombieSpawnClip;
    public AudioClip spiderSpawnClip;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    // 내부 변수들
    private List<GameObject> activeMonsters = new List<GameObject>();
    private float currentSpawnChance;
    private int failedSpawnStreak = 0;
    private bool spawnCycleActive = false;

    // ★ 코루틴 제어용 변수
    private Coroutine spawnCoroutine;

    void Start()
    {
        // 처음 시작할 때도 리셋 로직을 통해 시작
        ResetSpawner();
    }

    // ★★★ [GameManager에서 호출할 리셋 함수] ★★★
    public void ResetSpawner()
    {
        // 1. 기존 몬스터 싹 정리
        ClearAllMonsters();

        // 2. 변수 초기화 (Start 값으로 복구)
        currentSpawnChance = firstSpawnChance;
        failedSpawnStreak = 0;
        spawnCycleActive = false;

        // 3. 실행 중이던 코루틴을 강제로 끄고 새로 시작!
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());

        Debug.Log("🔄 [Spawner] 리셋 완료 (재시작 준비 끝)");
    }

    IEnumerator SpawnRoutine()
    {
        // 재시작 시 안전하게 1초 대기 후 로직 시작
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            // 게임 중이 아니면 대기
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            yield return new WaitForSeconds(spawnInterval);

            // 대기 후 재확인
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                continue;
            }

            activeMonsters.RemoveAll(m => m == null);

            if (activeMonsters.Count >= maxMonsters) continue;

            // 좀비 생존 시 스폰 중단 로직
            bool hasZombie = activeMonsters.Exists(m => m != null && m.GetComponent<ZombieNavTarget>() != null);
            if (hasZombie)
            {
                if (spawnCycleActive) Debug.Log("[Spawner] 좀비 생존 중 → 사이클 리셋");
                spawnCycleActive = false;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                continue;
            }

            if (!spawnCycleActive)
            {
                spawnCycleActive = true;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
            }

            TrySpawnMonsterWithChance();
        }
    }

    void TrySpawnMonsterWithChance()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float roll = Random.value;
        bool pass = roll <= currentSpawnChance;

        if (pass)
        {
            if (SpawnRandomMonster())
            {
                Debug.Log($"[Spawner] 스폰 성공! (확률: {currentSpawnChance * 100}%)");
                spawnCycleActive = false;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
            }
            else
            {
                IncreaseSpawnChanceStep();
            }
        }
        else
        {
            IncreaseSpawnChanceStep();
        }
    }

    void IncreaseSpawnChanceStep()
    {
        failedSpawnStreak++;
        if (failedSpawnStreak == 1) currentSpawnChance = secondSpawnChance;
        else currentSpawnChance = thirdSpawnChance;
        Debug.Log($"[Spawner] 꽝 → 확률 증가: {currentSpawnChance * 100}%");
    }

    bool SpawnRandomMonster()
    {
        activeMonsters.RemoveAll(m => m == null);
        if (activeMonsters.Count >= maxMonsters) return false;

        bool spawnZombie = (Random.value < zombieSpawnChance);
        return spawnZombie ? SpawnZombie() : SpawnSpider();
    }

    bool SpawnZombie()
    {
        if (zombieSpawnPoints == null || zombieSpawnPoints.Count == 0) return false;

        Transform point = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Count)];
        GameObject monster = Instantiate(zombiePrefab, point.position, point.rotation);
        activeMonsters.Add(monster);

        ZombieNavTarget mover = monster.GetComponent<ZombieNavTarget>();
        if (mover != null)
        {
            mover.SetTarget(zombieTargetPoint);
            if (zombieWanderAreaMesh != null) mover.wanderAreaMesh = zombieWanderAreaMesh;
            mover.spawnPoint = point;
        }

        PlayLoudSpawnSound(zombieSpawnClip, point.position, soundVolume);
        return true;
    }

    bool SpawnSpider()
    {
        if (spiderSpawnPoints == null || spiderSpawnPoints.Count == 0) return false;

        Transform point = spiderSpawnPoints[Random.Range(0, spiderSpawnPoints.Count)];
        GameObject spider = Instantiate(spiderPrefab, point.position, point.rotation);
        activeMonsters.Add(spider);

        SpiderCeilingFollowTarget ctrl = spider.GetComponent<SpiderCeilingFollowTarget>();
        if (ctrl != null)
        {
            ctrl.SetTarget(spiderTargetPoint);
            if (spiderRoofMesh != null) ctrl.roofMesh = spiderRoofMesh;
            ctrl.ceilingLayer = spiderCeilingLayer;
            ctrl.groundLayer = spiderGroundLayer;
            ctrl.spawnPoint = point;
        }

        PlayLoudSpawnSound(spiderSpawnClip, point.position, soundVolume);
        return true;
    }

    void PlayLoudSpawnSound(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null) return;
        GameObject audioObj = new GameObject("SpawnSound_Loud");
        audioObj.transform.position = position;
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0.8f;      
        source.minDistance = 20.0f;      
        source.maxDistance = 150.0f;     
        source.rolloffMode = AudioRolloffMode.Linear; 
        source.Play();
        Destroy(audioObj, clip.length);
    }

    public void ClearAllMonsters()
    {
        foreach (GameObject monster in activeMonsters)
        {
            if (monster != null) Destroy(monster);
        }
        activeMonsters.Clear();
    }
}