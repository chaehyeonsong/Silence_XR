using UnityEngine;

public class SpiderCeilingFollowTarget : MonoBehaviour
{
    private enum SpiderState
    {
        CeilingMove,
        Drop,
        Land
    }


    [Header("Drop Rotate Settings")]
    public float dropRotateDuration = 0.3f;

    [Header("Drop Limits")]
    [Tooltip("드롭 시작점 기준 최대 낙하 길이. 여기에 도달하면 Game Over")]
    public float maxDropDistance = 1.5f;

    [Header("Kill Flag Lock")]
    public bool lockToTarget = false;

    [Header("Target Point")]
    public Transform targetPoint;

    [Header("Movement")]
    public float ceilingMoveSpeed = 2f;
    [Tooltip("Lock 상태일 때 이동 속도 배율")]
    public float chaseSpeedMultiplier = 2.5f;
    public float rotateSpeed = 7f;
    public float dropSpeed = 8f;

    [Header("Calm Return Settings")]
    public float calmTimeout = 15f;
    private float noFlagTimer = 0f;

    [Header("Ceiling Settings")]
    public LayerMask ceilingLayer;
    public float ceilingCheckDistance = 0.8f;
    public float ceilingStickOffset = 0.05f;

    [Header("Ground Settings (외부 호환용, 내부 로직 미사용)")]
    public LayerMask groundLayer;       // 외부(Spawner 등) 참조 호환용

    [Header("Drop Settings (외부 호환용)")]
    [Tooltip("이 수평 반경 안으로 들어오면 Drop 시작")]
    public float dropHorizontalRadius = 1.0f; // MoveOnCeiling에서 드롭 시작 조건

    [Header("Web Line Settings")]
    public LineRenderer webLine;
    [Tooltip("드롭 시작점에서 줄을 그릴지 여부 (시각효과용)")]
    public bool keepWebFromDropStart = true;

    [Header("Roof Area")]
    public MeshRenderer roofMesh;
    private Bounds roofBounds;

    [Header("Return Home")]
    public Transform spawnPoint;
    public float returnArriveRadius = 0.2f;

    [Header("Idle Wander")]
    public bool useRandomWander = true;
    public float wanderDirChangeInterval = 3f;

    // 내부 상태 변수들
    private SpiderState state = SpiderState.CeilingMove;
    private Vector3 fixedCeilingNormal;

    // Drop 회전 관련
    private bool isDropRotating = false;
    private Quaternion dropStartRot;
    private Quaternion dropTargetRot;
    private float dropRotateTimer = 0f;

    // Web / Drop 관련
    private bool isWebActive = false;
    private Vector3 webStartPos;   // 라인렌더러 시작점(시각효과용)
    private Vector3 dropOrigin;    // ★ 드롭 시작 위치(거리 판정 기준)

    // Flag / 상태
    private bool isAlerted = false;
    private suin_FlagHub hub;

    // Wander
    private Vector3 wanderDir;
    private float wanderTimer = 0f;
    private bool isReturningHome = false;

    // 중복 호출 방지용
    private bool hasTriggeredGameOver = false;

    void OnEnable()
    {
        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag += OnAlertFlag;
            hub.OnLightStateChanged += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
            hub.OnLightStateChanged -= OnAlertFlag;
        }
    }

    void OnAlertFlag(bool v)
    {
        if (lockToTarget) return;

        if (v)
        {
            if (isReturningHome)
            {
                Debug.Log("🕷️ [Spider] 복귀 중 인기척 감지! 다시 추격 모드 전환");
                isReturningHome = false;
            }
            noFlagTimer = 0f;
        }
        isAlerted = v;
    }

    void Start()
    {
        if (roofMesh == null)
        {
            GameObject foundRoof = GameObject.Find("Bedroom_roof");
            if (foundRoof != null) roofMesh = foundRoof.GetComponent<MeshRenderer>();
        }

        if (webLine != null)
        {
            webLine.positionCount = 0;
            webLine.enabled = false;
        }

        AttachToCeiling_And_FixNormal();
        if (roofMesh != null) roofBounds = roofMesh.bounds;

        wanderDir = transform.forward;
        wanderTimer = wanderDirChangeInterval;
    }

    void Update()
    {
        // Lock 상태면 항상 경계 유지
        if (lockToTarget)
        {
            isAlerted = true;
            isReturningHome = false;
        }
        else
        {
            // 진정 타이머
            noFlagTimer += Time.deltaTime;
            if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
            {
                isReturningHome = true;
                isAlerted = false;

                // 드롭 중이었다면 천장 이동으로 복귀
                if (state == SpiderState.Drop) state = SpiderState.CeilingMove;

                // 라인 비활성
                if (webLine != null) { webLine.enabled = false; webLine.positionCount = 0; }
                isWebActive = false;
            }
        }

        switch (state)
        {
            case SpiderState.CeilingMove:
                MoveOnCeiling();
                break;
            case SpiderState.Drop:
                DropDown(); // ▶ 여기에서만 Game Over 판정
                break;
            case SpiderState.Land:
                // 게임오버 이후 대기
                break;
        }
    }

    // 초기화 및 천장 부착
    void AttachToCeiling_And_FixNormal()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;
            if (Vector3.Dot(n, Vector3.down) < 0f) n = -n;
            fixedCeilingNormal = n;
            transform.position = hit.point + n * ceilingStickOffset;
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
        else
        {
            fixedCeilingNormal = Vector3.down;
        }
    }

    // 천장 이동 로직
    void MoveOnCeiling()
    {
        if (lockToTarget) isReturningHome = false;

        // 집으로 돌아가는 로직
        if (isReturningHome && spawnPoint != null)
        {
            MoveToTarget(spawnPoint.position, ceilingMoveSpeed);
            if (Vector3.Distance(GetXZ(transform.position), GetXZ(spawnPoint.position)) <= returnArriveRadius)
                Destroy(gameObject);
            return;
        }

        // 평화 상태 배회
        if (!isAlerted || targetPoint == null)
        {
            if (useRandomWander && !lockToTarget) CeilingIdleWander();
            else MaintainCeilingAttachment();
            return;
        }

        // 추적
        float currentSpeed = lockToTarget ? (ceilingMoveSpeed * chaseSpeedMultiplier) : ceilingMoveSpeed;
        MoveToTarget(targetPoint.position, currentSpeed);

        // 수평 반경 내로 들어오면 드롭 시작
        float dist = Vector3.Distance(GetXZ(transform.position), GetXZ(targetPoint.position));
        if (dist <= dropHorizontalRadius)
        {
            StartDrop();
        }
    }

    // 목표 지점으로 이동 (회전 포함)
    void MoveToTarget(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(dir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        Vector3 next = transform.position + transform.forward * speed * Time.deltaTime;
        next = ClampToRoofXZ(next);
        transform.position = next;
        MaintainCeilingAttachment();
    }

    // 낙하 시작 초기화
    void StartDrop()
    {
        isDropRotating = true;
        dropRotateTimer = 0f;
        dropStartRot = transform.rotation;

        // 현재 방향에서 고개만 아래(-90도)로 숙임
        dropTargetRot = transform.rotation * Quaternion.Euler(-90f, 0f, 0f);

        // ★ 드롭 거리 기준점 저장
        dropOrigin = transform.position;

        // 라인 이펙트
        if (webLine != null)
        {
            isWebActive = true;
            webLine.enabled = true;
            webLine.positionCount = 2;
            webStartPos = keepWebFromDropStart ? transform.position : (targetPoint != null ? targetPoint.position : transform.position);
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }

        state = SpiderState.Drop;
        Debug.Log("🕷️ [Spider] Drop 시작! (-Y 방향 하강)");
    }

    // 배회 로직
    void CeilingIdleWander()
    {
        if (roofMesh == null)
        {
            Debug.LogWarning("거미: Roof Mesh가 없습니다! 배회 중지.");
            MaintainCeilingAttachment();
            return;
        }
        roofBounds = roofMesh.bounds;
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f || wanderDir == Vector3.zero)
        {
            wanderTimer = wanderDirChangeInterval * 2f;
            Vector2 r2 = Random.insideUnitCircle.normalized;
            wanderDir = new Vector3(r2.x, 0f, r2.y);
        }

        if (wanderDir != Vector3.zero)
        {
            Quaternion tr = Quaternion.LookRotation(wanderDir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, tr, rotateSpeed * Time.deltaTime);
        }

        Vector3 next = transform.position + transform.forward * ceilingMoveSpeed * Time.deltaTime;
        next = ClampToRoofXZ(next);
        transform.position = next;
        MaintainCeilingAttachment();
    }

    // 이동 제한 (천장 범위 유지)
    Vector3 ClampToRoofXZ(Vector3 pos)
    {
        if (roofMesh == null) return pos;
        roofBounds = roofMesh.bounds;
        pos.x = Mathf.Clamp(pos.x, roofBounds.min.x, roofBounds.max.x);
        pos.z = Mathf.Clamp(pos.z, roofBounds.min.z, roofBounds.max.z);
        return pos;
    }

    // 천장에 붙어있도록 유지
    void MaintainCeilingAttachment()
    {
        if (Physics.Raycast(transform.position, -fixedCeilingNormal, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;
            if (Vector3.Dot(n, Vector3.down) < 0f) n = -n;
            transform.position = hit.point + n * ceilingStickOffset;
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
    }

    // ─────────────────────────────────────────
    // 오직 "드롭 길이가 maxDropDistance 도달" 시에만 Game Over
    // ─────────────────────────────────────────
    void DropDown()
    {
        float step = dropSpeed * Time.deltaTime;

        // 드롭 회전
        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);
            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);
            if (t >= 1f) isDropRotating = false;
        }

        // 다음 위치(아래로 하강)
        Vector3 proposed = transform.position + Vector3.down * step;

        // 드롭 시작점으로부터의 다음 길이
        float nextLen = Vector3.Distance(proposed, dropOrigin);

        // 최대 길이에 도달하면: 그 지점으로 스냅 + Game Over
        if (nextLen >= maxDropDistance)
        {
            Vector3 dir = (proposed - dropOrigin).normalized;
            Vector3 clampedPos = dropOrigin + dir * maxDropDistance;
            transform.position = clampedPos;

            if (isWebActive && webLine != null)
            {
                webLine.SetPosition(0, webStartPos);
                webLine.SetPosition(1, transform.position);
            }

            state = SpiderState.Land;
            if (!hasTriggeredGameOver && hub != null)
            {
                hasTriggeredGameOver = true;
                hub.TriggerPlayerKillFlag(); // ▶ 유일한 Game Over 트리거
            }
            return;
        }

        // 아직 최대 길이에 못 미치면 계속 하강
        transform.position = proposed;

        // 라인렌더러 업데이트(시각효과)
        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }
    }

    // 유틸리티: Y축 제거
    Vector3 GetXZ(Vector3 v) => new Vector3(v.x, 0, v.z);

    // 외부에서 타겟 설정
    public void SetTarget(Transform target)
    {
        targetPoint = target;
    }

    // 강제 타겟 고정 및 추적 시작
    public void ForceLockToTarget(Transform target)
    {
        targetPoint = target;
        lockToTarget = true;
        isAlerted = true;
        isReturningHome = false;

        Debug.Log("🕷️ [Spider] 강제 타겟 고정 (Kill Mode)");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (roofMesh != null)
        {
            Gizmos.color = Color.cyan;
            Bounds b = roofMesh.bounds;
            Vector3 center = b.center;
            Vector3 size = b.size;
            size.y = 0.01f;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}
