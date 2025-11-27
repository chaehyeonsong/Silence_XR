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
    public float spawnInterval = 5f;
    [Range(0f, 1f)]
    public float zombieSpawnChance = 0.5f;      // 0.7이면 70% 좀비, 30% 스파이더

    [Header("Targets")]
    public Transform zombieTargetPoint;         // 모든 좀비가 달려갈 타겟
    public Transform spiderTargetPoint;         // 스파이더가 향할 타겟

    [Header("Monster Limit")]
    public int maxMonsters = 4;                 // 최대 몬스터 수 (좀비+스파이더 합산)

    // 현재 살아있는 몬스터들 추적용
    private List<GameObject> activeMonsters = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnRandomMonster();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnRandomMonster()
    {
        // 먼저 null 정리 (죽어서 Destroy된 애들 제거)
        activeMonsters.RemoveAll(m => m == null);

        // 최대 마리 수에 도달했으면 스폰 스킵
        if (activeMonsters.Count >= maxMonsters)
        {
            //Debug.Log($"🐾 Max monsters reached ({activeMonsters.Count}/{maxMonsters}), skip spawn.");
            return;
        }

        bool spawnZombie = (Random.value < zombieSpawnChance);

        if (spawnZombie)
        {
            SpawnZombie();
        }
        else
        {
            SpawnSpider();
        }
    }

    void SpawnZombie()
    {
        if (zombieSpawnPoints == null || zombieSpawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ No zombie spawn points assigned to Spawner.");
            return;
        }

        Transform point = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Count)];
        GameObject monster = Instantiate(zombiePrefab, point.position, point.rotation);

        // 리스트에 등록
        activeMonsters.Add(monster);

        // 좀비에게 타겟 포인트 할당
        ZombieNavTarget mover = monster.GetComponent<ZombieNavTarget>();
        if (mover != null)
        {
            mover.SetTarget(zombieTargetPoint);
        }
        else
        {
            Debug.LogWarning($"⚠️ Spawned zombie '{monster.name}' has no ZombieNavTarget component.");
        }

        Debug.Log("🧟 Spawned Zombie at: " + point.name);
    }

    void SpawnSpider()
    {
        if (spiderSpawnPoints == null || spiderSpawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ No spider spawn points assigned to Spawner.");
            return;
        }

        Transform point = spiderSpawnPoints[Random.Range(0, spiderSpawnPoints.Count)];
        GameObject spider = Instantiate(spiderPrefab, point.position, point.rotation);

        // 리스트에 등록
        activeMonsters.Add(spider);

        // 스파이더에게 타겟 포인트 할당 (SpiderCeilingFollowTarget 사용)
        SpiderCeilingFollowTarget ctrl = spider.GetComponent<SpiderCeilingFollowTarget>();
        if (ctrl != null)
        {
            ctrl.SetTarget(spiderTargetPoint);
        }
        else
        {
            Debug.LogWarning($"⚠️ Spawned spider '{spider.name}' has no SpiderCeilingFollowTarget component.");
        }

        Debug.Log("🕷️ Spawned Spider at Vent: " + point.name);
    }
}
