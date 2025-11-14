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
    public float ceilingMoveSpeed = 2f;
    public float rotateSpeed = 7f; 
    public float dropSpeed = 5f;

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

    void Start()
    {
        // LineRenderer 기본 비활성화
        if (webLine != null)
        {
            webLine.positionCount = 0;
            webLine.enabled = false;
        }

        AttachToCeiling_And_FixNormal();
    }

    void Update()
    {
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
    // 2) 천장에서 목표 지점까지 이동 (방향은 자유롭게 회전)
    // ───────────────────────────────────────────────────────────────
    void MoveOnCeiling()
    {
        if (targetPoint == null) return;

        // target 방향 (수평 기준)
        Vector3 dir = targetPoint.position - transform.position;
        dir.y = 0f;
        dir.Normalize();

        // target 방향으로 회전 (천장 normal은 고정)
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 앞으로 이동
        transform.position += transform.forward * ceilingMoveSpeed * Time.deltaTime;

        // 천장에 계속 붙도록 보정
        MaintainCeilingAttachment();

        // ───── Drop 조건: 수평 거리(xz)로만 판단 ─────
        Vector3 spiderXZ = transform.position;
        spiderXZ.y = 0f;
        Vector3 targetXZ = targetPoint.position;
        targetXZ.y = 0f;

        float horizontalDist = Vector3.Distance(spiderXZ, targetXZ);
        if (horizontalDist <= dropHorizontalRadius)
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
        // 1) 회전 중이면 부드럽게 회전하면서 떨어짐
        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);

            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);

            // 회전 중에도 천천히 아래로 떨어지게
            transform.position += Vector3.down * dropSpeed * Time.deltaTime;

            if (t >= 1f)
            {
                isDropRotating = false; // 회전 완료
            }
        }
        else
        {
            // 회전이 끝난 후에는 그냥 떨어지기
            transform.position += Vector3.down * dropSpeed * Time.deltaTime;
        }

        // ☆ 거미줄(흰 줄) 업데이트
        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);          // 위쪽 고정 점
            webLine.SetPosition(1, transform.position);   // 현재 거미 위치
        }

        // 바닥 착지 체크
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            transform.position = hit.point + Vector3.up * groundStickOffset;
            state = SpiderState.Land;

            // 착지 후 줄 처리 (남겨두고 싶으면 이 부분 주석 처리)
            // webLine.enabled = false;
            // isWebActive = false;
        }
    }
}
