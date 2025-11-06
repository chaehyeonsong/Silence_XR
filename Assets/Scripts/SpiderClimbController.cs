using UnityEngine;
using UnityEngine.AI;

public class SpiderClimbController : MonoBehaviour
{
    public Animation anim;
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip climbClip;

    public float walkSpeed = 2f;
    public float climbSpeed = 1f;
    public float wallCheckDistance = 1f;

    private NavMeshAgent agent;
    private bool isClimbing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim.Play(idleClip.name);
    }

    void Update()
    {
        // 벽 감지 (전방 레이캐스트)
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, wallCheckDistance))
        {
            // 부딪힌 게 벽이면 → climb
            if (!isClimbing && hit.normal.y < 0.5f)
            {
                StartClimb(hit);
            }
        }
        else
        {
            // 벽이 아니면 → 일반 걷기 상태 복귀
            if (isClimbing)
                StopClimb();
        }

        // 이동 모션 업데이트
        if (!isClimbing && agent.velocity.magnitude > 0.1f)
            anim.CrossFade(walkClip.name, 0.2f);
        else if(!isClimbing)
            anim.CrossFade(idleClip.name, 0.2f);
    }

    void StartClimb(RaycastHit wallHit)
    {
        isClimbing = true;
        agent.enabled = false; // NavMeshAgent 비활성화 (벽 타기)

        anim.CrossFade(climbClip.name, 0.2f);

        // 거미를 벽 방향으로 회전
        Quaternion targetRot = Quaternion.LookRotation(-wallHit.normal);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 10f * Time.deltaTime);
    }

    void StopClimb()
    {
        isClimbing = false;
        agent.enabled = true; // 다시 이동 가능

        anim.CrossFade(walkClip.name, 0.2f);
    }
}
