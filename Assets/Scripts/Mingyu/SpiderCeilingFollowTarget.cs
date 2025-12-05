using UnityEngine;

public class SpiderCeilingFollowTarget : MonoBehaviour
{
    private enum SpiderState
    {
        CeilingMove,
        Drop,
        Land
    }

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

    [Header("Ground Settings")]
    public LayerMask groundLayer;      
    public float groundCheckDistance = 1.0f;
    public float groundStickOffset = 0.05f;

    [Header("Drop Settings")]
    [Tooltip("이 거리 안으로 들어오면 Drop 시작")]
    public float dropHorizontalRadius = 1.0f;
    public float dropRotateDuration = 0.3f;

    [Header("Web Line Settings")]
    public LineRenderer webLine;
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

    // Web 관련
    private bool isWebActive = false;
    private Vector3 webStartPos;

    // Flag / 상태
    private bool isAlerted = false;
    private suin_FlagHub hub;

    // Wander
    private Vector3 wanderDir;
    private float wanderTimer = 0f;
    private bool isReturningHome = false;

    // ★ 중복 호출 방지용 플래그
    private bool hasTriggeredGameOver = false;

    void OnEnable()
    {
        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag += OnAlertFlag;
            
            // ▼▼▼ [추가] 4번 트리거(불 켜짐/상태변경)에도 반응하도록 추가 ▼▼▼
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

            // ▼▼▼ [추가] 해제도 잊지 말고 추가 ▼▼▼
            hub.OnLightStateChanged -= OnAlertFlag; 
        }
    }

    void OnAlertFlag(bool v)
    {
        // 이미 타겟 강제 고정(Lock) 상태면 간섭하지 않음
        if (lockToTarget) return;

        // 신호(v)가 true(소리/이동 감지)라면?
        if (v)
        {
            // 만약 집으로 가던 중이었다면? -> 복귀 취소!
            if (isReturningHome)
            {
                Debug.Log("🕷️ [Spider] 복귀 중 인기척 감지! 다시 추격 모드 전환");
                isReturningHome = false; 
            }
            
            // 진정 타이머 초기화 (다시 0초부터 카운트)
            noFlagTimer = 0f;
        }

        // 알람 상태 갱신
        isAlerted = v;
    }

    void Start()
    {
        if (roofMesh == null)
    {
        GameObject foundRoof = GameObject.Find("Bedroom_roof"); 
        if (foundRoof != null)
        {
            roofMesh = foundRoof.GetComponent<MeshRenderer>();
        }
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
        // 1. 상태 업데이트 (Lock 상태면 무조건 Alert 유지)
        if (lockToTarget)
        {
            isAlerted = true;
            isReturningHome = false;
        }
        else
        {
            // Calm 체크 로직
            noFlagTimer += Time.deltaTime;
            if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
            {
                isReturningHome = true;
                isAlerted = false;
                
                // 만약 떨어지던 중이었다면 다시 천장 이동 상태로 복귀 (원하는 기획에 따라 변경 가능)
                if (state == SpiderState.Drop) state = SpiderState.CeilingMove;
                
                if (webLine != null) { webLine.enabled = false; webLine.positionCount = 0; }
                isWebActive = false;
            }
        }

        // 2. 행동 실행
        switch (state)
        {
            case SpiderState.CeilingMove:
                MoveOnCeiling();
                break;
            case SpiderState.Drop:
                DropDown();
                break;
            case SpiderState.Land:
                // 착지 완료 상태. 이미 게임오버 요청을 보냈으므로 대기.
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
                Destroy(gameObject); // 집에 도착하면 삭제
            return;
        }

        // 평화로운 상태일 때 배회
        if (!isAlerted || targetPoint == null)
        {
            if (useRandomWander && !lockToTarget) CeilingIdleWander();
            else MaintainCeilingAttachment();
            return;
        }

        // 추적 로직
        float currentSpeed = lockToTarget ? (ceilingMoveSpeed * chaseSpeedMultiplier) : ceilingMoveSpeed;
        MoveToTarget(targetPoint.position, currentSpeed);

        // 타겟과 수평 거리가 가까워지면 낙하 시작
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
        dir.y = 0f; // 높이 무시
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

        if (webLine != null)
        {
            isWebActive = true;
            webLine.enabled = true;
            webLine.positionCount = 2;
            // 줄 시작점을 천장(현재위치) or 타겟위치 중 선택
            webStartPos = keepWebFromDropStart ? transform.position : targetPoint.position;
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
        Debug.LogWarning("거미: Roof Mesh가 없습니다! 배회 중지."); // 로그 확인
        MaintainCeilingAttachment(); 
        return; 
    }
        if (roofMesh == null) { MaintainCeilingAttachment(); return; }
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

    // 이동 제한 (천장 범위 밖으로 나가지 않게)
    Vector3 ClampToRoofXZ(Vector3 pos)
    {
        if (roofMesh == null) return pos;
        roofBounds = roofMesh.bounds;
        pos.x = Mathf.Clamp(pos.x, roofBounds.min.x, roofBounds.max.x);
        pos.z = Mathf.Clamp(pos.z, roofBounds.min.z, roofBounds.max.z);
        return pos;
    }

    // 천장에 딱 붙어있도록 유지
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

    // ───────────────────────────────────────────────────────────────
    // ★ 낙하 및 착지 로직 (여기서 Game Over 호출)
    // ───────────────────────────────────────────────────────────────
    void DropDown()
    {
        float step = dropSpeed * Time.deltaTime;

        // 1) 회전 (고개를 아래로)
        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);
            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);
            if (t >= 1f) isDropRotating = false;
        }

        // 2) 바닥 감지 (Raycast)
        // 거미 머리 위쪽에서부터 Ray를 쏴서 바닥을 미리 감지 (뚫림 방지)
        float rayStartOffset = 1.5f; 
        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;
        float rayLength = rayStartOffset + groundCheckDistance + (step * 2f); // 넉넉하게 체크

        // 디버그용 붉은 선
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            // 이번 프레임 이동 시 바닥을 뚫거나 거의 도달한다면
            if (transform.position.y - step <= hit.point.y + groundStickOffset)
            {
                // 위치를 바닥 바로 위로 강제 이동 (Snap)
                transform.position = new Vector3(transform.position.x, hit.point.y + groundStickOffset, transform.position.z);
                
                // 거미줄 끝점 업데이트
                if (isWebActive && webLine != null)
                {
                    webLine.SetPosition(0, webStartPos);
                    webLine.SetPosition(1, transform.position);
                }

                state = SpiderState.Land;
                Debug.Log($"🕷️ [Spider] 바닥 착지 완료! ({hit.collider.name})");

                // ============================================
                // ★ 핵심: 착지 순간 -> FlagHub에 죽음 신호 전송
                // ============================================
                if (!hasTriggeredGameOver && hub != null)
                {
                    hasTriggeredGameOver = true; // 중복 실행 방지
                    Debug.Log("🕷️ [Spider] 착지함 -> FlagHub.TriggerPlayerKillFlag() 호출!");
                    hub.TriggerPlayerKillFlag(); // -> FlagHub -> GameManager -> Game Over
                }
                return;
            }
        }

        // 3) 바닥에 안 닿았으면 계속 하강
        transform.position += Vector3.down * step;

        // 거미줄 업데이트
        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }
    }

    // 유틸리티: Y축 제거
    Vector3 GetXZ(Vector3 v) => new Vector3(v.x, 0, v.z);

    // 외부에서 타겟 설정 (KillFlagZone 등에서 호출)
    public void SetTarget(Transform target) 
    { 
        targetPoint = target; 
    }

    // 강제로 타겟 고정 및 추적 시작 (KillFlagZone에서 호출)
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