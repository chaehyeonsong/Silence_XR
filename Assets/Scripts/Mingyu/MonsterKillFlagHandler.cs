using UnityEngine;
using System.Collections;

public class MonsterKillFlagHandler : MonoBehaviour
{
    [Header("플레이어 타겟 (targetPoint)")]
    public Transform playerTargetPoint;

    private suin_FlagHub hub;

    void OnEnable()
    {
        StartCoroutine(InitAndSubscribe());
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnPlayerKillFlag -= OnPlayerKillFlag;
        }
    }

    IEnumerator InitAndSubscribe()
    {
        while (suin_FlagHub.instance == null)
        {
            yield return null; 
        }

        hub = suin_FlagHub.instance;

        hub.OnPlayerKillFlag -= OnPlayerKillFlag;
        hub.OnPlayerKillFlag += OnPlayerKillFlag;

        Debug.Log("[MonsterKillFlagHandler] FlagHub 인스턴스 확인 후 PlayerKillFlag 구독 완료");
    }

    void OnPlayerKillFlag()
    {
        if (playerTargetPoint == null)
        {
            Debug.LogWarning("[MonsterKillFlagHandler] playerTargetPoint가 비어 있습니다.");
            return;
        }

        Debug.Log("🔥 [MonsterKillFlagHandler] PlayerKillFlag 수신 → 모든 몬스터를 플레이어 위치로 '강제 고정(Lock)'");

        // === 1) 좀비들 처리 ===
        ZombieNavTarget[] zombies = FindObjectsOfType<ZombieNavTarget>();
        foreach (var z in zombies)
        {
            if (z == null) continue;

            // ❌ 기존 코드: z.SetTarget(playerTargetPoint); <- 이것만 하면 도착 후 다시 움직임
            
            // ✅ 수정 코드: ForceLockToTarget을 호출해야 lockToTarget = true가 됨
            z.ForceLockToTarget(playerTargetPoint);

            Debug.Log($"[MonsterKillFlagHandler] Zombie → {z.name} 타겟 고정(Lock) 설정 완료");
        }

        // === 2) 거미들 처리 ===
        SpiderCeilingFollowTarget[] spiders = FindObjectsOfType<SpiderCeilingFollowTarget>();
        foreach (var s in spiders)
        {
            if (s == null) continue;

            // 거미도 마찬가지로 SetTarget만 하면 움직일 수 있으므로, 
            // 거미 스크립트에도 ForceLock 같은 기능이 있다면 그걸 써야 함.
            // (현재 거미 코드는 SetTarget만 보임)
            s.SetTarget(playerTargetPoint);
            Debug.Log($"[MonsterKillFlagHandler] Spider → {s.name} 타겟 변경");
        }
    }
}