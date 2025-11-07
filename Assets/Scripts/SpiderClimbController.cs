using UnityEngine;

public class SpiderClimbController : MonoBehaviour
{
    private enum SpiderState
    {
        Ground,     // 평지 걷기
        ClimbWall,  // 벽 타고 수직 이동 (forward = 위)
        ClimbUp,    // 꼭대기 턱 넘기기 (위 + 안쪽 대각선 이동)
        DropDown    // 바닥을 바라보며 낙하
    }

    private SpiderState state = SpiderState.Ground;

    [Header("Speed")]
    public float moveSpeed = 2f;       // 평지 이동 속도
    public float wallClimbSpeed = 2f;  // 벽 타고 올라가는 속도
    public float climbUpSpeed = 2f;    // 턱 넘을 때 속도 (위+안쪽)
    public float dropSpeed = 3f;       // 떨어지는 속도

    [Header("Layers")]
    public LayerMask groundLayer;      // 평지 레이어
    public LayerMask climbLayer;       // 벽(장애물) 레이어

    [Header("Ray Settings")]
    public float groundCheckDistance = 1.2f;   // 아래로 레이 길이
    public float forwardCheckDistance = 0.5f;  // 앞 레이 길이
    public float forwardRayHeight = 0.3f;      // 앞 레이 쏘는 높이
    public float stickOffset = 0.05f;          // 표면에서 살짝 띄우는 거리

    [Header("Drop Settings")]
    public float landingCheckDistance = 0.3f;  // 착지 감지용 짧은 거리

    private Vector3 wallNormal;   // 현재 붙어있는 벽의 노멀
    private Vector3 climbDir;     // 벽면 따라 올라갈 방향 (위쪽)

    // 아래로 쏠 때는 ground + climb 둘 다 "걸을 수 있는 표면" 취급
    LayerMask WalkableMask => groundLayer | climbLayer;

    void Start()
    {
        Debug.Log("Spider State → " + state);
    }

    void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * forwardRayHeight, transform.forward * forwardCheckDistance, Color.red);

        switch (state)
        {
            case SpiderState.Ground:
                UpdateGround();
                break;
            case SpiderState.ClimbWall:
                UpdateClimbWall();
                break;
            case SpiderState.ClimbUp:
                UpdateClimbUp();
                break;
            case SpiderState.DropDown:
                UpdateDropDown();
                break;
        }
    }

    void SetState(SpiderState newState)
    {
        if (state == newState) return;

        // DropDown으로 들어갈 때 한 번만 회전 세팅
        if (newState == SpiderState.DropDown)
        {
            AlignForFall();
        }

        state = newState;
        Debug.Log("Spider State → " + state);
    }

    // 떨어질 때 바닥을 바라보게 회전
    void AlignForFall()
    {
        // forward(Z+)를 바닥(Down)으로
        Vector3 fallForward = Vector3.down;

        // Up축은 이전 forward의 수평 성분을 사용해서 옆으로 덜 비틀리게
        Vector3 up = transform.forward;
        up.y = 0f;
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.forward; // 안전빵 기본값

        up.Normalize();

        transform.rotation = Quaternion.LookRotation(fallForward, up);
    }

    // 평지용 자세로 복구 (forward 수평화)
    void AlignForGround()
    {
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;   // 모델이 Z+가 앞

        flatForward.Normalize();
        transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    // ─────────────────────── 1) 평지 상태 ───────────────────────
    void UpdateGround()
    {
        // 1) 먼저 발밑 바닥부터 확실히 잡고
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, WalkableMask))
        {
            transform.position = groundHit.point + Vector3.up * stickOffset;
        }
        else
        {
            SetState(SpiderState.DropDown);
            return;
        }

        // 2) 그 다음 Z+ 방향(앞)으로 이동
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // 3) 앞에 벽(climbLayer) 있는지 체크
        Vector3 forwardOrigin = transform.position + Vector3.up * forwardRayHeight;

        if (Physics.Raycast(forwardOrigin, transform.forward, out RaycastHit wallHit, forwardCheckDistance, climbLayer))
        {
            float angle = Vector3.Angle(wallHit.normal, Vector3.up);

            // 수직면(거의 90도)이면 벽으로 판정
            if (angle > 80f)
            {
                // 벽 정보 저장
                wallNormal = wallHit.normal;

                // 벽을 따라 "위로" 올라갈 방향 계산
                climbDir = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
                if (climbDir.sqrMagnitude < 0.0001f)
                    climbDir = Vector3.up;

                if (Vector3.Dot(climbDir, Vector3.up) < 0)
                    climbDir = -climbDir;

                // forward(Z+) = climbDir(위쪽), up = wallNormal(벽 바깥)
                transform.rotation = Quaternion.LookRotation(climbDir, wallNormal);

                // 벽 표면에 딱 붙이기
                transform.position = wallHit.point + wallNormal * stickOffset;

                SetState(SpiderState.ClimbWall);
            }
        }
    }

    // ─────────────────────── 2) 벽 타기 ───────────────────────
    void UpdateClimbWall()
    {
        // 아직도 같은 벽에 붙어있는지 확인 (-wallNormal 방향으로 짧게 레이)
        if (Physics.Raycast(transform.position, -wallNormal, out RaycastHit hit, 1f, climbLayer))
        {
            // 벽 표면에 밀착
            transform.position = hit.point + wallNormal * stickOffset;

            // forward(Z+) = climbDir(위쪽) 으로 계속 올라감
            transform.position += climbDir * wallClimbSpeed * Time.deltaTime;
        }
        else
        {
            // 더 이상 벽이 없으면 턱 넘기 단계로
            SetState(SpiderState.ClimbUp);
        }
    }

    // ─────────────────────── 3) 꼭대기 턱 넘기 ───────────────────────
    void UpdateClimbUp()
    {
        // "위쪽(+Y) + 큐브 안쪽(-wallNormal)" 방향으로 이동해서
        // 큐브 위 평면 위로 자연스럽게 올라감
        Vector3 overDir = (Vector3.up - wallNormal).normalized;
        transform.position += overDir * climbUpSpeed * Time.deltaTime;

        // 밑에 뭔가(ground 또는 climb 위면)가 생길 때까지 계속 감
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance * 3f, WalkableMask))
        {
            // 그 지점을 바닥으로 보고 착지
            transform.position = groundHit.point + Vector3.up * stickOffset;

            // 정상에서 forward를 수평으로 돌려놓기
            AlignForGround();

            SetState(SpiderState.Ground);
        }
    }

    // ─────────────────────── 4) 떨어지는 상태 ───────────────────────
    void UpdateDropDown()
    {
        // 1) 계속 아래로 떨어지기
        transform.position += Vector3.down * dropSpeed * Time.deltaTime;

        // 2) 발밑 "가까운" 위치에 바닥이 있는지 짧게 체크
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, landingCheckDistance, WalkableMask))
        {
            // 바로 아래에 바닥이 있으면 그때만 살짝 스냅
            transform.position = hit.point + Vector3.up * stickOffset;

            // 평지용 회전으로 복구 (이제 더 이상 바닥을 안 봄)
            AlignForGround();

            SetState(SpiderState.Ground);
        }
    }
}
