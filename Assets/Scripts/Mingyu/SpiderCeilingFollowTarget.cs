using UnityEngine;

public class SpiderCeilingFollowTarget : MonoBehaviour
{
    private enum SpiderState
    {
        CeilingMove,
        Drop,
        Land
    }

    [Header("Target Point")]
    public Transform targetPoint;

    [Header("Movement")]
    public float ceilingMoveSpeed = 2f;   // 천장에서 이동 속도
    public float rotateSpeed = 7f;
    public float dropSpeed = 5f;

    [Header("Calm Return Settings")]
    public float calmTimeout = 15f;
    private float noFlagTimer = 0f;


    [Header("Ceiling Settings")]
    public LayerMask ceilingLayer;
    public float ceilingCheckDistance = 0.8f;
    public float ceilingStickOffset = 0.05f;

    [Header("Ground Settings")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.5f;
    public float groundStickOffset = 0.05f;

    [Header("Drop Settings")]
    [Tooltip("수평 거리(xz)가 이 값 이하가 되면 Drop 시작")]
    public float dropHorizontalRadius = 0.3f;

    [Tooltip("Drop 진입 후 회전에 걸리는 시간(초)")]
    public float dropRotateDuration = 0.3f;

    [Header("Web Line Settings")]
    [Tooltip("Drop 때 거미줄처럼 보일 LineRenderer")]
    public LineRenderer webLine;
    [Tooltip("Drop 시작 지점에서부터 줄을 그릴지 여부")]
    public bool keepWebFromDropStart = true;

    [Header("Roof Area (거미가 배회할 천장 메쉬)")]
    public MeshRenderer roofMesh;   // 이 mesh의 bounds(XZ) 안에서만 배회
    private Bounds roofBounds;

    [Header("Return Home Settings")]
    [Tooltip("Spawner에서 주입되는 스폰 포인트")]
    public Transform spawnPoint;
    public float returnArriveRadius = 0.2f;

    [Header("Idle Wander Settings (플래그 오기 전 상태)")]
    public bool useRandomWander = true;
    public float wanderDirChangeInterval = 3f;  // 한 방향으로 유지할 시간

    private SpiderState state = SpiderState.CeilingMove;

    // 천장 normal (실내 방향으로 고정: 항상 아래쪽)
    private Vector3 fixedCeilingNormal;

    // Drop 회전 관련
    private bool isDropRotating = false;
    private Quaternion dropStartRot;
    private Quaternion dropTargetRot;
    private float dropRotateTimer = 0f;

    // Web / 줄 관련
    private bool isWebActive = false;
    private Vector3 webStartPos;

    // 플래그 연동 (허브에서 true/false 들어옴)
    private bool isAlerted = false;
    private suin_FlagHub hub;

    // 랜덤 배회용
    private Vector3 wanderDir;
    private float wanderTimer = 0f;

    // Calm 이후 집에 돌아가는 상태
    private bool isReturningHome = false;

    void OnEnable()
    {
        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag  += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag  += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnMoveSlightFlag  -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag  -= OnAlertFlag;
        }
    }

    void OnAlertFlag(bool v)
    {
        // 집으로 돌아가는 중이면 새 플래그 무시
        if (isReturningHome) return;

        // 허브에서 true → 1.5초 뒤 false를 쏘므로, 그대로 따라가기
        isAlerted = v;
        // Debug.Log($"[Spider Alert] {name} isAlerted = {isAlerted}");
    }

    void Start()
    {
        // LineRenderer 기본 비활성화
        if (webLine != null)
        {
            webLine.positionCount = 0;
            webLine.enabled = false;
        }

        AttachToCeiling_And_FixNormal();

        if (roofMesh != null)
        {
            roofBounds = roofMesh.bounds;
        }

        wanderDir = transform.forward; // 배회 시작 방향
        wanderTimer = wanderDirChangeInterval;
    }

    void Update()
    {
        // 🔥 0) 허브가 존재하고, 15초 이상 아무 flag가 없으면 → 귀환 모드 진입
         noFlagTimer += Time.deltaTime;

    // 아직 귀환 중 아니고, 15초 넘으면 귀환 모드 진입
    if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
    {
        isReturningHome = true;
        isAlerted = false;

        if (state == SpiderState.Drop)
            state = SpiderState.CeilingMove;

        if (webLine != null)
        {
            webLine.enabled = false;
            webLine.positionCount = 0;
        }
        isWebActive = false;
    }

        switch (state)
        {
            case SpiderState.CeilingMove:
                MoveOnCeiling();
                break;

            case SpiderState.Drop:
                DropDown();
                break;

            case SpiderState.Land:
                // 필요하면 착지 이후 로직 추가
                break;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 1) START: 천장에 붙기 + normal 고정 (실내 천장만 인식)
    // ───────────────────────────────────────────────────────────────
    void AttachToCeiling_And_FixNormal()
    {
        // 거미가 천장 "아래"에 있다고 가정 → 위로 캐스트해서 내부 천장 찾기
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;

            // 천장 법선이 항상 "아래(Vector3.down)"를 향하도록 정규화
            if (Vector3.Dot(n, Vector3.down) < 0f)
                n = -n;

            fixedCeilingNormal = n;

            // 천장 안쪽(실내)으로 약간 밀어넣기 → + n * offset
            transform.position = hit.point + n * ceilingStickOffset;

            // 거꾸로 매달린 상태: up = fixedCeilingNormal
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
        else
        {
            // 못 찾았을 때 기본값
            fixedCeilingNormal = Vector3.down;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 2) 천장에서 이동 로직 (플래그 전: 배회 / 플래그 후: 추적)
    // ───────────────────────────────────────────────────────────────
    void MoveOnCeiling()
    {
        // 0) 집에 돌아가는 중이면 spawnPoint 쪽으로만 이동하고,
        //    도착하면 Destroy
        if (isReturningHome && spawnPoint != null)
        {
            Vector3 dir = spawnPoint.position - transform.position;
            dir.y = 0f;
            dir.Normalize();

            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, fixedCeilingNormal);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            Vector3 nextPos = transform.position + transform.forward * ceilingMoveSpeed * Time.deltaTime;
            nextPos = ClampToRoofXZ(nextPos);
            transform.position = nextPos;

            MaintainCeilingAttachment();

            // 스폰 포인트 근처에 도달하면 삭제
            Vector3 spiderXZ = transform.position; spiderXZ.y = 0f;
            Vector3 spawnXZ  = spawnPoint.position; spawnXZ.y = 0f;

            if (Vector3.Distance(spiderXZ, spawnXZ) <= returnArriveRadius)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 아직 플래그가 안 들어왔거나, 타겟이 없으면 → 배회 모드
        if (!isAlerted || targetPoint == null)
        {
            if (useRandomWander)
            {
                CeilingIdleWander();
            }
            else
            {
                // 배회 끄고 싶으면 그냥 천장에만 붙어있게
                MaintainCeilingAttachment();
            }
            return;
        }

        // ───────── 여기부터는 "플래그 이후" → targetPoint 추적 ─────────

        // target 방향 (수평 기준)
        Vector3 dirTrack = targetPoint.position - transform.position;
        dirTrack.y = 0f;
        dirTrack.Normalize();

        // target 방향으로 회전 (천장 normal은 고정)
        if (dirTrack != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirTrack, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 앞으로 이동 (alert도 같은 speed 사용)
        Vector3 next = transform.position + transform.forward * ceilingMoveSpeed * Time.deltaTime;
        next = ClampToRoofXZ(next);
        transform.position = next;

        // 천장에 계속 붙도록 보정
        MaintainCeilingAttachment();

        // ───── Drop 조건: 수평 거리(xz)로만 판단 ─────
        Vector3 spiderXZ2 = transform.position;
        spiderXZ2.y = 0f;
        Vector3 targetXZ = targetPoint.position;
        targetXZ.y = 0f;

        float horizontalDist = Vector3.Distance(spiderXZ2, targetXZ);
        if (!isReturningHome && horizontalDist <= dropHorizontalRadius)
        {
            // Drop 진입 시 회전 셋업
            isDropRotating = true;
            dropRotateTimer = 0f;
            dropStartRot = transform.rotation;

            // Z축 180 → X축 90 (local 기준)
            Quaternion delta = Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(90f, 0f, 0f);
            dropTargetRot = transform.rotation * delta;

            // Web 시작 지점 저장
            if (webLine != null)
            {
                isWebActive = true;
                webLine.enabled = true;
                webLine.positionCount = 2;

                // 줄의 시작점: Drop 시작 시의 위치 or 현재 위치 기준
                webStartPos = keepWebFromDropStart ? transform.position : targetPoint.position;

                webLine.SetPosition(0, webStartPos);          // 위쪽 고정 점
                webLine.SetPosition(1, transform.position);   // 아래쪽: 거미 위치
            }

            state = SpiderState.Drop;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 2-1) 플래그 오기 전: Roof bounds 안에서 랜덤 배회
    // ───────────────────────────────────────────────────────────────
    void CeilingIdleWander()
    {
        // roofMesh가 없으면 그냥 기존 위치 유지 + 천장 붙이기만
        if (roofMesh == null)
        {
            MaintainCeilingAttachment();
            return;
        }

        // 혹시 roof가 움직일 수 있으면 매 프레임 bounds 갱신
        roofBounds = roofMesh.bounds;

        // 일정 시간마다 새로운 방향 뽑기
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || wanderDir == Vector3.zero)
        {
            // 같은 방향을 더 오래 유지 → 같은 속도로 2배 거리 이동
            wanderTimer = wanderDirChangeInterval * 2f;

            // 수평 랜덤 방향
            Vector2 r2 = Random.insideUnitCircle.normalized;
            wanderDir = new Vector3(r2.x, 0f, r2.y);

            Debug.Log($"[Spider IdleWander] {name} → new wanderDir = {wanderDir}");
        }

        // 회전
        if (wanderDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(wanderDir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 이동
        Vector3 nextPos = transform.position + transform.forward * ceilingMoveSpeed * Time.deltaTime;
        nextPos = ClampToRoofXZ(nextPos);
        transform.position = nextPos;

        // 천장에 계속 붙도록 보정
        MaintainCeilingAttachment();
    }

    // ───────────────────────────────────────────────────────────────
    // Roof Mesh의 XZ bounds 안으로 클램프
    // ───────────────────────────────────────────────────────────────
    Vector3 ClampToRoofXZ(Vector3 pos)
    {
        if (roofMesh == null) return pos;

        // bounds는 월드 좌표 기준
        roofBounds = roofMesh.bounds;

        pos.x = Mathf.Clamp(pos.x, roofBounds.min.x, roofBounds.max.x);
        pos.z = Mathf.Clamp(pos.z, roofBounds.min.z, roofBounds.max.z);

        return pos;
    }

    // ───────────────────────────────────────────────────────────────
    // 3) 천장 유지 (fixed normal 기반 → 흔들림 0%)
    // ───────────────────────────────────────────────────────────────
    void MaintainCeilingAttachment()
    {
        Vector3 rayDir = -fixedCeilingNormal;

        if (Physics.Raycast(transform.position, rayDir, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;

            // 혹시라도 반대면이면 뒤집기
            if (Vector3.Dot(n, Vector3.down) < 0f)
                n = -n;

            // 천장 안쪽(실내)으로 붙이기 → + n * offset
            transform.position = hit.point + n * ceilingStickOffset;

            // up은 계속 fixedCeilingNormal 유지
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 4) Drop: Z축 180 → X축 90을 부드럽게 회전하며 낙하 + 흰 줄 연출
    // ───────────────────────────────────────────────────────────────
    void DropDown()
    {
        // 이번 프레임에 떨어질 거리
        float step = dropSpeed * Time.deltaTime;

        // 1) 회전 로직
        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);

            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);

            if (t >= 1f)
            {
                isDropRotating = false; // 회전 완료
            }
        }

        // 2) 이번 프레임 안에 바닥을 만나는지 먼저 Ray로 체크
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckDistance;
        float rayLength = step + groundCheckDistance * 2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            // 이번 프레임 안에 바닥을 만난 경우 → 그 위치에 스냅하고 착지 처리
            transform.position = hit.point + Vector3.up * groundStickOffset;

            if (isWebActive && webLine != null)
            {
                webLine.SetPosition(0, webStartPos);
                webLine.SetPosition(1, transform.position);
            }

            state = SpiderState.Land;
            return;
        }

        // 3) 바닥 안 만났으면 그냥 아래로 이동
        transform.position += Vector3.down * step;

        // 거미줄(흰 줄) 업데이트
        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);        // 위쪽 고정 점
            webLine.SetPosition(1, transform.position); // 현재 거미 위치
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 5) Spawner에서 타겟을 주입하기 위한 메서드
    // ───────────────────────────────────────────────────────────────
    public void SetTarget(Transform target)
    {
        targetPoint = target;
        // 바로 방향 맞추고 싶으면 천장 이동 시작 전에 한 번 회전
        if (targetPoint != null && state == SpiderState.CeilingMove)
        {
            Vector3 dir = targetPoint.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                transform.rotation = Quaternion.LookRotation(dir, fixedCeilingNormal);
            }
        }
    }

#if UNITY_EDITOR
    // 선택했을 때 Roof bounds 시각화하면 튜닝 편함
    void OnDrawGizmosSelected()
    {
        if (roofMesh == null) return;

        Gizmos.color = Color.cyan;
        Bounds b = roofMesh.bounds;
        Vector3 center = b.center;
        Vector3 size = b.size;
        // Y는 얇게 줄여서 대충 XZ 영역만 보이게
        size.y = 0.01f;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
