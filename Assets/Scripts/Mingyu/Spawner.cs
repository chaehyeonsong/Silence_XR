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
            // 🛑 [GameManager 연동]
            // 게임 매니저가 있고, 현재 상태가 Playing이 아니라면(Opening 등) 스폰 멈춤
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                yield return new WaitForSeconds(1.0f); // 1초 대기 후 다시 검사
                continue; 
            }

            // [기본 로직] 시간 대기
            yield return new WaitForSeconds(spawnInterval);

            // 몬스터 리스트 정리 (죽은 애들 제거)
            activeMonsters.RemoveAll(m => m == null);

            // 최대 마리 수 체크
            if (activeMonsters.Count >= maxMonsters)
            {
                Debug.Log($"[Spawner] 몬스터 가득 참 ({activeMonsters.Count}/{maxMonsters}), 스폰 스킵");
                continue;
            }

            // 좀비 생존 여부 체크
            bool hasZombie = activeMonsters.Exists(m => m != null && m.GetComponent<ZombieNavTarget>() != null);

            if (hasZombie)
            {
                if (spawnCycleActive) Debug.Log("[Spawner] 좀비 생존 중 → 사이클 리셋");
                spawnCycleActive = false;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                continue;
            }

            // 사이클 시작
            if (!spawnCycleActive)
            {
                spawnCycleActive = true;
                currentSpawnChance = firstSpawnChance;
                failedSpawnStreak = 0;
                Debug.Log("[Spawner] 스폰 사이클 시작");
            }

            TrySpawnMonsterWithChance();
        }
    }

    void TrySpawnMonsterWithChance()
    {
        float roll = Random.value;
        bool pass = roll <= currentSpawnChance;

        if (pass)
        {
            bool spawned = SpawnRandomMonster();
            if (spawned)
            {
                Debug.Log("[Spawner] ▶ 스폰 성공! 사이클 종료");
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
        
        Debug.Log($"[Spawner] 실패 → 확률 증가: {currentSpawnChance}");
    }

    bool SpawnRandomMonster()
    {
        // 다시 한 번 체크
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

        // 🔊 [오디오 수정됨] 좀비 클립 재생 (기존 코드에선 spider 클립이었음)
        PlayLoudSpawnSound(zombieSpawnClip, point.position, soundVolume);

        Debug.Log($"🧟 좀비 스폰됨: {point.name}");
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

        // 🔊 [오디오] 스파이더 클립 재생
        PlayLoudSpawnSound(spiderSpawnClip, point.position, soundVolume);

        Debug.Log($"🕷️ 스파이더 스폰됨: {point.name}");
        return true;
    }

    // 👇 [최종 오디오 함수] 이것만 남겼습니다 (Custom3DSound 삭제)
    void PlayLoudSpawnSound(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null) return;

        GameObject audioObj = new GameObject("SpawnSound_Loud");
        audioObj.transform.position = position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;

        // 세팅: 20m까지 최대 볼륨, 150m까지 들림, 2D 느낌 20% 섞음
        source.spatialBlend = 0.8f;      
        source.minDistance = 20.0f;      
        source.maxDistance = 150.0f;     
        source.rolloffMode = AudioRolloffMode.Linear; 

        source.Play();
        Destroy(audioObj, clip.length);
    }
}