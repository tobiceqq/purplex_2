using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }
    public State currentState = State.Patrol;

    public enum PatrolMode { Waypoints, RandomNavMesh }

    [Header("Patrol Mode")]
    public PatrolMode patrolMode = PatrolMode.Waypoints;
    public bool randomWaypointOrder = false;
    public float randomPatrolRadius = 10f;

    [Header("References")]
    public Transform[] patrolPoints;
    public Transform player;

    [Header("Drops & Effects")]
    public GameObject explosionPrefab;
    public GameObject healPrefab;      

    [Header("Movement Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Attack Settings (Propojeno s PlayerHealth)")]
    public float attackDistance = 1.5f;
    public float attackCooldown = 1f;
    public float attackDamage = 20f; 

    [Header("Enemy Stats")]
    public float enemyHealth = 50f; 

    [Header("Detection Settings")]
    public float chaseDistance = 8f;
    public float viewDistance = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("State Materials")]
    public Material patrolMaterial;
    public Material chaseMaterial;
    public Material attackMaterial;

    private Renderer rend;
    private NavMeshAgent agent;
    private int patrolIndex = 0;
    private float attackTimer = 0f;

    private Animator anim;

    private Color originalColor;
    public float flashDuration = 0.15f;

    private Renderer[] childRenderers; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        childRenderers = GetComponentsInChildren<Renderer>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null || this == null) return;


        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                Attack();
                break;
        }

        attackTimer -= Time.deltaTime;
        if (anim != null && agent != null)
        {
            float speed = agent.velocity.magnitude;

            if (speed > 0.1f)
            {
                anim.SetBool("isRunning", true);
            }
            else
            {
                anim.SetBool("isRunning", false);
            }
        }
    }
    IEnumerator FlashEffect(Renderer r)
    {
       
        Material[] mats = r.materials;
        Color[] oldColors = new Color[mats.Length];

        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].HasProperty("_Color")) oldColors[i] = mats[i].color;
            else if (mats[i].HasProperty("_BaseColor")) oldColors[i] = mats[i].GetColor("_BaseColor");

            if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", Color.red);
            else mats[i].color = Color.red;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", oldColors[i]);
            else mats[i].color = oldColors[i];
        }
    }

    void Patrol()
    {
        if (rend != null && patrolMaterial != null) rend.material = patrolMaterial;
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance < 0.3f)
        {
            if (patrolMode == PatrolMode.Waypoints && patrolPoints.Length > 0)
            {
                if (randomWaypointOrder)
                {
                    patrolIndex = Random.Range(0, patrolPoints.Length);
                }
                else
                {
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                }

                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
            else if (patrolMode == PatrolMode.RandomNavMesh)
            {
                agent.SetDestination(GetRandomNavMeshPosition());
            }
        }

        if (ShouldStartChasing())
            currentState = State.Chase;
    }

    void Chase()
    {
        if (rend != null && chaseMaterial != null) rend.material = chaseMaterial;
        agent.speed = chaseSpeed;

        if (!PlayerInChaseRange() && !PlayerInViewRange())
        {
            currentState = State.Patrol;
            return;
        }

        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackDistance && PlayerInViewRange())
            currentState = State.Attack;
    }

    void Attack()
    {
        if (rend != null && attackMaterial != null) rend.material = attackMaterial;
        agent.ResetPath(); 

        if (!PlayerInChaseRange() && !PlayerInViewRange())
        {
            currentState = State.Patrol;
            return;
        }

        if (!PlayerInViewRange())
        {
            currentState = State.Chase;
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        transform.rotation = Quaternion.LookRotation(direction);

        if (attackTimer <= 0f)
        {
            Debug.Log("Nep¯Ìtel tÏ kousnul!");

            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(attackDamage);
            }

            attackTimer = attackCooldown;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                
                if (pc.IsBallMode)
                {
                    float damage = pc.isDashing ? 50f : 25f;
                    TakeDamage(damage);

                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 knockbackDir = (transform.position - collision.transform.position).normalized;
                        knockbackDir.y = 0.5f; 
                        rb.AddForce(knockbackDir * 15f, ForceMode.Impulse);
                    }

                    Debug.Log("⁄SPÃäN› Z¡SAH KOULÕ! HP: " + enemyHealth);
                }
                else
                {
                    Debug.Log("Kolize s hr·Ëem v lidskÈ formÏ - û·dnÈ poökozenÌ nep¯Ìtele.");
                }
            }
        }
    }
    System.Collections.IEnumerator StunEnemy()
    {
        agent.enabled = false; 
        yield return new WaitForSeconds(0.5f); 
        if (enemyHealth > 0) agent.enabled = true; 
    }


    public void TakeDamage(float amount)
    {
        enemyHealth -= amount;
        Debug.Log("Z·sah do modelu! HP: " + enemyHealth);

        // Blikneme vöemi Ë·stmi modelu
        foreach (Renderer r in childRenderers)
        {
            StartCoroutine(FlashWithPropertyBlock(r));
        }

        if (enemyHealth <= 0) Die();
    }

    IEnumerator FlashWithPropertyBlock(Renderer r)
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        
        r.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", Color.red);      
        propBlock.SetColor("_BaseColor", Color.red);   
        r.SetPropertyBlock(propBlock);

        yield return new WaitForSeconds(flashDuration);

        r.GetPropertyBlock(propBlock);
        propBlock.Clear();
        r.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (explosion != null) Destroy(explosion, 3f);
        }

        if (healPrefab != null)
        {
            Instantiate(healPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        StopAllCoroutines();
        Destroy(gameObject);
    }


    bool PlayerInChaseRange()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        float dist = dirToPlayer.magnitude;
        if (dist > chaseDistance) return false;
        if (Physics.Raycast(transform.position, dirToPlayer.normalized, dist, obstacleMask)) return false;
        return true;
    }

    bool PlayerInViewRange()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        float dist = dirToPlayer.magnitude;
        if (dist > viewDistance) return false;
        dirToPlayer.Normalize();
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle / 2f) return false;
        if (Physics.Raycast(transform.position, dirToPlayer, dist, obstacleMask)) return false;
        return true;
    }

    bool ShouldStartChasing()
    {
        return PlayerInChaseRange() || PlayerInViewRange();
    }

    Vector3 GetRandomNavMeshPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * randomPatrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, randomPatrolRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        Gizmos.color = Color.cyan;
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + right * viewDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}