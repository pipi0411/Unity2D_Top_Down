// Scripts/EnemyAI.cs
using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private MonoBehaviour enemyType;

    private bool canAttack = true;
    private Vector2 roamPosition;
    private float timeRoaming = 0f;

    private enum State { Roaming, Attacking }
    private State state;

    private EnemyPathfinding pathfinding;

    private void Awake()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        state = State.Roaming;
    }

    private void Start()
    {
        roamPosition = GetRoamingPosition();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Roaming: Roaming(); break;
            case State.Attacking: Attacking(); break;
        }
    }

    private void Roaming()
    {
        timeRoaming += Time.deltaTime;
        pathfinding.MoveTo(roamPosition);

        // Nếu thấy player trong tầm thì chuyển qua Attacking
        if (Vector2.Distance(transform.position, PlayerController.Instance.transform.position) < enemyData.attackRange)
        {
            state = State.Attacking;
        }

        if (timeRoaming > enemyData.roamChangeDirTime)
        {
            roamPosition = GetRoamingPosition();
        }
    }

    private void Attacking()
    {
        Transform player = PlayerController.Instance.transform;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > enemyData.attackRange * 3f) // Player thoát xa
        {
            state = State.Roaming;
            return;
        }

        // Luôn hướng về Player
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        pathfinding.MoveTo(dirToPlayer);

        // Nếu đủ gần thì Attack
        if (distanceToPlayer <= 3f && canAttack) // 1f = melee range
        {
            canAttack = false;
            (enemyType as IEnemy)?.Attack();

            if (enemyData.stopMovingWhileAttacking)
                pathfinding.StopMoving();

            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(enemyData.attackCooldown);
        canAttack = true;
    }

    private Vector2 GetRoamingPosition()
    {
        timeRoaming = 0f;
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }
}
