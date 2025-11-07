using UnityEngine;

public class SpiderClimbController : MonoBehaviour
{
    private enum SpiderState
    {
        Ground,
        Climb
    }

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotateSpeed = 8f;
    public float forwardRayDistance = 1.0f;   // 앞 레이 길이
    public float downRayDistance = 2.0f;      // 아래 레이 길이
    public float stickDistance = 0.05f;       // 표면에서 살짝 띄우는 거리

    [Header("거미가 달라붙을 표면 레이어")]
    public LayerMask climbLayers;             // Inspector에서 Climb 체크

    private Animation anim;

    // 상태 관련
    private SpiderState state = SpiderState.Ground;
    private Vector3 wallNormal;         // 지금 타고 있는 벽의 노멀
    private Vector3 climbDir;           // 벽에서 움직일 방향 (위로)
    private const float wallAngleThreshold = 60f; // 이 각도 이상이면 벽으로 간주

    void Start()
    {
        anim = GetComponent<Animation>();
        anim.Play("walk");   // 실제 walk 클립 이름 맞춰줘
    }

    void Update()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        switch (state)
        {
            case SpiderState.Ground:
                UpdateGround(origin);
                break;
            case SpiderState.Climb:
                UpdateClimb(origin);
                break;
        }

        // 애니메이션은 계속 walk 유지
        if (!anim.IsPlaying("walk"))
            anim.CrossFade("walk", 0.15f);

        // 디버그 레이
        Debug.DrawRay(origin, transform.forward * forwardRayDistance, Color.red);
        Debug.DrawRay(origin, Vector3.down * downRayDistance, Color.blue);
    }

    // ───────────────── GROUND 상태 ─────────────────
    void UpdateGround(Vector3 origin)
    {
        // 1) 바닥 붙이기
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, downRayDistance, climbLayers))
        {
            // 위치 Y만 맞춰주고
            Vector3 targetPos = groundHit.point + groundHit.normal * stickDistance;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

            // 평지 회전 : Y축만 사용 (벽용 회전 절대 안 씀)
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            flatForward.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(flatForward, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 2) 평지에서 계속 앞으로 이동
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // 3) 앞에 벽 있으면 → Climb 상태로 한 번 전환
        if (Physics.Raycast(origin, transform.forward, out RaycastHit fHit, forwardRayDistance, climbLayers))
        {
            float angle = Vector3.Angle(fHit.normal, Vector3.up);
            if (angle > wallAngleThreshold)   // 거의 수직이면 벽
            {
                EnterClimb(fHit);
            }
        }
    }

    // ───────────────── CLIMB 시작 ─────────────────
void EnterClimb(RaycastHit hit)
{
    state = SpiderState.Climb;

    wallNormal = hit.normal;

    climbDir = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
    if (climbDir.sqrMagnitude < 0.001f)
        climbDir = Vector3.up;

    if (Vector3.Dot(climbDir, Vector3.up) < 0f)
        climbDir = -climbDir;

    // 🔥 여기 추가
    climbDir = -climbDir;   // 그냥 아예 반대로

    Quaternion climbRot = Quaternion.LookRotation(climbDir, wallNormal);
    transform.rotation = climbRot;

    Vector3 targetPos = hit.point - wallNormal * stickDistance;
    transform.position = targetPos;
}


    // ───────────────── CLIMB 상태 ─────────────────
    void UpdateClimb(Vector3 origin)
    {
        // 1) 아직도 같은 벽에 붙어있는지 확인
        bool onWall = Physics.Raycast(origin, -wallNormal, out RaycastHit wallHit, forwardRayDistance, climbLayers);

        if (!onWall)
        {
            // 더 이상 그 벽이 없으면 → 바닥 모드로 전환 시도
            TryExitClimbToGround(origin);
            return;
        }

        // 2) 벽에 계속 딱 붙이기 (회전은 건드리지 않음!)
        Vector3 targetPos = wallHit.point - wallNormal * stickDistance;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        // 3) 벽 "위쪽" 방향(climbDir)으로만 이동 (대각선 X)
        transform.position += climbDir * moveSpeed * Time.deltaTime;
        // 여기서는 rotation 안 건드림 → 올라갈 때 계속 rotate 안 함ㄹ
    }

    // ───────────────── CLIMB 종료 → GROUND 전환 ─────────────────
    void TryExitClimbToGround(Vector3 origin)
    {
        // 벽에서 떨어졌을 때, 아래에 바닥 있으면 Ground로 전환
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, downRayDistance, climbLayers))
        {
            // 위치
            Vector3 targetPos = groundHit.point + groundHit.normal * stickDistance;
            transform.position = targetPos;

            // ⭐ 여기서 딱 한 번 "평지 회전" 하고,
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            flatForward.Normalize();

            Quaternion groundRot = Quaternion.LookRotation(flatForward, Vector3.up);
            transform.rotation = groundRot;

            // 상태 전환
            state = SpiderState.Ground;
        }
        else
        {
            // 바닥도 없으면 그냥 떨어지게 (원하면 수정 가능)
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        }
    }
}
