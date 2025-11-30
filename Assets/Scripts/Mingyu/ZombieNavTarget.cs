using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))] // 🔥 추가: AudioSource 필수
public class ZombieNavTarget : MonoBehaviour
{
    [Header("Kill Flag Lock")]
    public bool lockToTarget = false;        // 죽는 플래그 이후: 타겟에만 고정

    [Header("Movement Speed Settings")]
    [Tooltip("Lock(죽음 플래그) 상태일 때 이동 속도 배율 (기본 속도의 n배)")]
    public float chaseSpeedMultiplier = 2.5f;
    private float initialSpeed;              // 원래 속도 저장용

    [Header("Audio Settings")] 
    public AudioClip chaseSound;             // 🔥 추가: 달려들 때 재생할 사운드 (괴음 등)
    private AudioSource audioSource;         // 🔥 추가: 오디오 소스 참조

    [Header("Calm Return Settings")]
    public float calmTimeout = 15f;          // flag 없으면 이 시간 뒤 귀환
    private float noFlagTimer = 0f;          // 마지막 flag 이후 경과 시간

    [Header("Target")]
    public Transform targetPoint;            // 좀비가 달려갈 목적지
    public float arriveDistance = 0.35f;     // 도착 판정 거리

    [Header("Idle Wander Settings (경계 전 상태)")]
    public bool useRandomWander = true;      // Alert 전 랜덤 배회할지 여부

    [Tooltip("wanderAreaMesh가 없을 때 사용할 반경 (초기 위치 기준)")]
    public float wanderRadius = 8f;          // Fallback 반경
    public float wanderInterval = 8f;        // 새 목적지를 고르는 최소 간격(초)

    [Tooltip("새 wander 목적지가 현재 위치와 최소 이 정도는 떨어지도록 강제")]
    public float minWanderDistance = 4f;     // 너무 짧은 이동 방지

    [Header("Wander Area (옵션: 이 MeshRenderer bounds 안에서만 배회)")]
    public MeshRenderer wanderAreaMesh;      // 바닥/방 MeshRenderer 넣어주면 됨

    [Header("Alert Settings (플래그 들어오면 추적 시작)")]
    public bool useAlert = true;             // suin_FlagHub 플래그 연동 여부

    [Header("Return Home Settings")]
    [Tooltip("Spawner에서 주입되는 스폰 포인트")]
    public Transform spawnPoint;             // 스폰 위치
    public float returnArriveDistance = 0.3f;

    private NavMeshAgent agent;

    // 플래그 관련
    private suin_FlagHub hub;
    private bool isAlerted = false;          // 현재 Alert 상태 (허브에서 true/false 들어옴)

    // 배회 관련
    private Vector3 wanderCenter;
    private float wanderTimer = 0f;

    // Calm 이후 집에 돌아가는 상태
    private bool isReturningHome = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>(); // 🔥 추가: 컴포넌트 가져오기

        // 초기 설정
        agent.stoppingDistance = arriveDistance;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        
        // 초기 속도 저장
        initialSpeed = agent.speed;

        wanderCenter = transform.position;
    }

    void OnEnable()
    {
        if (!useAlert) return;

        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (!useAlert) return;

        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
        }
    }

    // 플래그 들어왔을 때 호출
    void OnAlertFlag(bool v)
    {
        if (!useAlert) return;
        if (isReturningHome) return;
        if (lockToTarget) return;   // 🔒 죽는 플래그 이후에는 새 alert 무시

        isAlerted = v;

        if (v)
        {
            noFlagTimer = 0f;
            if (targetPoint != null)
                SetDestinationToTarget();
        }
        else if (!v && useRandomWander)
        {
            // 경계 해제 시 잠시 멈춤 or 즉시 배회 로직으로 전환
            agent.ResetPath();
        }
    }

    void Start()
    {
        if (targetPoint != null && isAlerted)
        {
            SetDestinationToTarget();
        }
    }

    void Update()
    {
        if (agent == null) return;

        // ─────────────────────────────────────────────────────────────
        // 🔒 1. 죽는 플래그 (Lock Mode) - 최우선 순위
        // ─────────────────────────────────────────────────────────────
        if (lockToTarget)
        {
            // 다른 상태들 강제 리셋
            isReturningHome = false;
            isAlerted = true;
            noFlagTimer = 0f;

            // 속도 증가 로직 적용
            agent.speed = initialSpeed * chaseSpeedMultiplier;

            if (targetPoint != null)
            {
                float dist = Vector3.Distance(transform.position, targetPoint.position);

                if (dist <= arriveDistance)
                {
                    // 도착했으면 '완전 정지'
                    if (!agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;
                    }
                }
                else
                {
                    // 이동
                    if (agent.isStopped) agent.isStopped = false;
                    agent.SetDestination(targetPoint.position);
                }
            }
            
            return; 
        }
        else
        {
            // 🔒 Lock 상태가 아닐 때는 원래 속도로 복구
            agent.speed = initialSpeed;
        }

        // ─────────────────────────────────────────────────────────────
        // 2. Calm Check (플래그 끊김 -> 귀환 타이머)
        // ─────────────────────────────────────────────────────────────
        noFlagTimer += Time.deltaTime;

        if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
        {
            isReturningHome = true;
            isAlerted = false;
            agent.ResetPath();
        }

        // ─────────────────────────────────────────────────────────────
        // 3. Return Home (집으로 귀환)
        // ─────────────────────────────────────────────────────────────
        if (isReturningHome)
        {
            if (spawnPoint == null)
            {
                Destroy(gameObject);
                return;
            }

            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(spawnPoint.position);

            if (!agent.pathPending && agent.remainingDistance <= returnArriveDistance)
            {
                Destroy(gameObject);
            }
            return;
        }

        // ─────────────────────────────────────────────────────────────
        // 4. Alert Chase (추적 - 일반 경계)
        // ─────────────────────────────────────────────────────────────
        if (isAlerted && targetPoint != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(targetPoint.position);
            return;
        }

        // ─────────────────────────────────────────────────────────────
        // 5. Idle Wander (배회)
        // ─────────────────────────────────────────────────────────────
        if (useRandomWander)
        {
            IdleWander();
        }
        else
        {
            // 배회 안 쓰는 좀비는 가만히 대기
            agent.isStopped = true;
        }
    }

    public void SetTarget(Transform target)
    {
        targetPoint = target;

        if (targetPoint == null)
        {
            Debug.LogWarning($"⚠️ {name} tried to SetTarget(null)");
            return;
        }

        if (isAlerted || lockToTarget)
        {
            SetDestinationToTarget();
        }
    }

    // 🔥 죽는 플래그에서 직접 호출할 메서드
    public void ForceLockToTarget(Transform target)
    {
        // 이미 락이 걸려있으면 소리 중복 재생 방지 (원하면 제거 가능)
        bool wasLocked = lockToTarget;

        targetPoint = target;
        lockToTarget = true;
        isAlerted = true;
        isReturningHome = false;
        noFlagTimer = 0f;

        // 즉시 이동 명령 & 속도 증가
        if (agent != null && target != null)
        {
            agent.speed = initialSpeed * chaseSpeedMultiplier;
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(target.position);
        }

        // 🔥 추가: 오디오 재생 (처음 락 걸릴 때만 재생)
        if (!wasLocked && audioSource != null)
        {
            if (chaseSound != null)
            {
                audioSource.clip = chaseSound;
            }
            // 소리 재생 (이미 재생중이 아니라면, 혹은 강제 재생)
            audioSource.Play();
            Debug.Log($"🔊 {name} 추격 사운드 재생!");
        }
        
        Debug.Log($"🧟 {name} 강제 Lock 활성화! (타겟: {target.name}, 속도: {agent.speed})");
    }

    void SetDestinationToTarget()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (targetPoint == null) return;

        agent.isStopped = false;
        agent.stoppingDistance = arriveDistance;
        agent.SetDestination(targetPoint.position);
    }

    void IdleWander()
    {
        wanderTimer -= Time.deltaTime;

        bool needNewDest =
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            (!agent.pathPending && agent.remainingDistance <= arriveDistance) ||
            wanderTimer <= 0f;

        if (!needNewDest) return;

        wanderTimer = wanderInterval * 2f;
        Vector3 rawTarget = transform.position;

        if (wanderAreaMesh != null)
        {
            var b = wanderAreaMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                float rx = Random.Range(b.min.x, b.max.x);
                float rz = Random.Range(b.min.z, b.max.z);
                Vector3 candidate = new Vector3(rx, transform.position.y, rz);

                if (Vector3.Distance(candidate, transform.position) >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir2 = Random.insideUnitCircle.normalized;
                Vector3 candidate = wanderCenter + new Vector3(dir2.x, 0f, dir2.y) * wanderRadius;

                if (Vector3.Distance(candidate, transform.position) >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }
        }

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(hit.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (wanderAreaMesh != null)
        {
            var b = wanderAreaMesh.bounds;
            Vector3 size = b.size;
            size.y = 0.01f;
            Gizmos.DrawWireCube(b.center, size);
        }
        else
        {
            Vector3 center = Application.isPlaying ? wanderCenter : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }
    }
#endif
}