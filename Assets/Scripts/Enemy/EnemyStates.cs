using UnityEngine;
using UnityEngine.AI;

// Enum for possible enemy states for debugging purposes mostly
public enum EnemyState
{
    Looking,
    Chasing,
    Attacking,
    NOTIMPLEMENTED
}
public interface IEnemyState
{
    void Enter(Enemy enemy);
    void Execute(Enemy enemy);
    void Exit(Enemy enemy);
}

//public class LookingState : IEnemyState
//{
//    //probably dont even need wander but good to have just in case
//    private float wanderRadius = 5f;
//    private float wanderTimer = 3f;
//    private float timer;

//    public void Enter(Enemy enemy)
//    {
//        timer = wanderTimer;
//    }

//    public void Execute(Enemy enemy)
//    {
//        timer += Time.deltaTime;

//        if (enemy.IsTargetInFOV())
//        {
//            enemy.StateMachine.ChangeState(new ChaseState(), enemy);
//            return;
//        }

//        if (timer >= wanderTimer)
//        {
//            Vector3 newPos = RandomNavSphere(enemy.transform.position, wanderRadius, -1);
//            enemy.agent.SetDestination(newPos);
//            timer = 0;
//        }
//    }

//    public void Exit(Enemy enemy)
//    {
//    }

//    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
//    {
//        Vector3 randDirection = Random.insideUnitSphere * dist;
//        randDirection += origin;

//        NavMeshHit navHit;
//        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

//        return navHit.position;
//    }
//}

public class ChaseState : IEnemyState
{
    float timer = 0.0f;
    public void Enter(Enemy enemy)
    {
        if (enemy.target == null) return;

        enemy.agent.SetDestination(enemy.target.position);
        enemy.animator.SetBool("IsMoving", true);
    }

    public void Execute(Enemy enemy)
    {
        float dist = Vector3.Distance(enemy.transform.position, enemy.target.position);
        if (dist < enemy.attackDistance)
        {
            enemy.StateMachine.ChangeState(new AttackingState(), enemy);
        }

        timer -= Time.deltaTime;
        if (timer < 0.0f)
        {
            timer = enemy.updatePathTime;
            if (enemy.target == null) return;
            enemy.agent.SetDestination(enemy.target.position);
        }
    }

    public void Exit(Enemy enemy)
    {
        enemy.animator.SetBool("IsMoving", false);
    }
}

public class AttackingState : IEnemyState
{
    private float attackTime;

    public void Enter(Enemy enemy)
    {
        // Start attack delay and animation
        enemy.agent.isStopped = true;
        float halfDelay = enemy.attackStartDelay / 4;
        attackTime = Time.time + enemy.attackStartDelay + Random.Range(-halfDelay, halfDelay);
    }

    public void Execute(Enemy enemy)
    {
        // Check distance to target
        float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.target.position);

        if (distanceToTarget >= enemy.loseDistance)
        {
            // Switch back to chasing if the target is out of sight
            enemy.StateMachine.ChangeState(new ChaseState(), enemy);
            return;
        }

        if (Time.time >= attackTime)
        {
            enemy.StartAttack();
            attackTime = Time.time + enemy.attackDelay;
        }

        enemy.LookTowardsTarget();
    }

    public void Exit(Enemy enemy)
    {
        // Resume movement or other logic as needed
        enemy.agent.isStopped = false;
        enemy.EndAttack();
    }
}