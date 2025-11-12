using UnityEngine;
using UnityEngine.AI;

public class ZombieNavPatrol : MonoBehaviour
{
    [Header("Waypoints in order: A, B, C, D")]
    public Transform[] points;          // ABCD 
    public float arriveDistance = 0.35f;

    private NavMeshAgent agent;
    private int idx;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = arriveDistance;
        agent.autoRepath = true;        
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        // agent.updateUpAxis = true; agent.updateRotation = true;
    }

    void Start()
    {
        if (points == null || points.Length < 2)
        {
            Debug.LogWarning("Assign at least 2 points (A,B,...)");
            enabled = false;
            return;
        }
        SetNext();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            SetNext();
        }
        if (agent.isPathStale || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            agent.ResetPath();
            SetNext();
        }
    }

    void SetNext()
    {
        if (points.Length == 0) return;
        agent.SetDestination(points[idx].position);
        idx = (idx + 1) % points.Length;
    }
}
