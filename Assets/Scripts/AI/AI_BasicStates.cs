using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A simple enum-based state machine for AI behavior.
/// States are represented as enum values and behavior is handled via switch statements.
/// This approach is straightforward but can become unwieldy as the number of states grows.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class AI_BasicStates : MonoBehaviour
{
    // Enum defining all possible AI states
    public enum AIState { Patrol, Chase, Attack }

    [Header("Detection Ranges")]
    [SerializeField] private float chaseRange = 10f;      // Distance at which AI starts chasing
    [SerializeField] private float attackRange = 2f;       // Distance at which AI stops to attack

    [Header("Hysteresis (prevents state flickering)")]
    [SerializeField] private float chaseHysteresis = 2f;   // Extra distance before returning to patrol
    [SerializeField] private float attackHysteresis = 0.5f; // Extra distance before resuming chase

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 15f;     // How far the AI wanders from current position
    [SerializeField] private float patrolWaitTime = 2f;    // Pause duration at each patrol point

    private NavMeshAgent agent;
    private Transform player;
    private AIState currentState = AIState.Patrol;         // Current active state
    private float patrolTimer;                              // Timer for patrol wait duration
    private Vector3 patrolTarget;                           // Current patrol destination

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        SetNewPatrolTarget();
    }

    void Update()
    {
        if (agent == null) return;

        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        UpdateState(distanceToPlayer);
        ExecuteState();
    }

    /// <summary>
    /// Evaluates transition conditions based on player distance.
    /// Hysteresis values create a buffer zone to prevent rapid state switching
    /// when the player is near a threshold boundary.
    /// </summary>
    private void UpdateState(float distanceToPlayer)
    {
        switch (currentState)
        {
            case AIState.Patrol:
                // Transition to chase when player enters detection range
                if (distanceToPlayer <= chaseRange)
                    TransitionToState(AIState.Chase);
                break;

            case AIState.Chase:
                // Transition to attack when close enough
                if (distanceToPlayer <= attackRange)
                    TransitionToState(AIState.Attack);
                // Return to patrol only when player is beyond chase range + hysteresis
                else if (distanceToPlayer > chaseRange + chaseHysteresis)
                    TransitionToState(AIState.Patrol);
                break;

            case AIState.Attack:
                // Resume chase only when player moves beyond attack range + hysteresis
                if (distanceToPlayer > attackRange + attackHysteresis)
                    TransitionToState(AIState.Chase);
                break;
        }
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                break;

            case AIState.Chase:
                Chase();
                break;

            case AIState.Attack:
                Attack();
                break;
        }
    }

    private void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"AI State: {currentState} -> {newState}");
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrol:
                agent.isStopped = false;
                SetNewPatrolTarget();
                break;

            case AIState.Chase:
                agent.isStopped = false;
                agent.stoppingDistance = attackRange * 0.9f;
                break;

            case AIState.Attack:
                agent.isStopped = true;
                break;
        }
    }

    private void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                SetNewPatrolTarget();
            }
        }
    }

    /// <summary>
    /// Picks a random point within patrolRadius and finds the nearest valid NavMesh position.
    /// NavMesh.SamplePosition ensures the destination is actually walkable.
    /// </summary>
    private void SetNewPatrolTarget()
    {
        patrolTimer = 0f;
        // Generate random point in a sphere around current position
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        // Find the closest point on the NavMesh to our generated random position
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(patrolTarget);
        }
    }

    private void Chase()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    private void Attack()
    {
        if (player != null)
        {
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
            Debug.Log("Attacking player!");
        }
    }

    /// <summary>
    /// Visualizes detection ranges in the Scene view when the AI is selected.
    /// Yellow = chase range, Red = attack range, Blue = patrol radius.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
