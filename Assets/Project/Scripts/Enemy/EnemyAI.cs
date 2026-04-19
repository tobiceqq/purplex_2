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
    public GameObject explosionPrefab; // Tvùj Particle System (Prefab)
    public GameObject healPrefab;      // Prefab lékárnièky/healu

    [Header("Movement Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Attack Settings (Propojeno s PlayerHealth)")]
    public float attackDistance = 1.5f;
    public float attackCooldown = 1f;
    public float attackDamage = 20f; // Kolik životù ubere Purplexovi

    [Header("Enemy Stats")]
    public float enemyHealth = 50f; // Životy nepøítele

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

    private Renderer[] childRenderers; // Budeme si pamatovat všechny èásti modelu

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        // Tohle najde všechny renderery v modelu (ruce, nohy, tìlo...)
        childRenderers = GetComponentsInChildren<Renderer>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        // Pokud hráè neexistuje (tøeba umøel), AI nic nedìlá
        if (player == null) return;

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
            // Zjistíme, jak rychle se agent hýbe
            float speed = agent.velocity.magnitude;

            // Pokud je rychlost vyšší než 0.1, nastavíme isRunning na true
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
        // Uložíme si všechny pùvodní barvy materiálù na tomto rendereru
        // (Model mùže mít víc materiálù, napø. kùže, brnìní atd.)
        Material[] mats = r.materials;
        Color[] oldColors = new Color[mats.Length];

        for (int i = 0; i < mats.Length; i++)
        {
            // Zkusíme najít barvu pod rùznými názvy, které Unity používá
            if (mats[i].HasProperty("_Color")) oldColors[i] = mats[i].color;
            else if (mats[i].HasProperty("_BaseColor")) oldColors[i] = mats[i].GetColor("_BaseColor");

            // Nastavíme jasnì èervenou
            if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", Color.red);
            else mats[i].color = Color.red;
        }

        yield return new WaitForSeconds(flashDuration);

        // Vrátíme barvy zpìt
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
        agent.ResetPath(); // Zastaví se, aby mohl zaútoèit

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

        // Otoèí se èelem k hráèi
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Aby se nenaklánìl nahoru/dolù
        transform.rotation = Quaternion.LookRotation(direction);

        if (attackTimer <= 0f)
        {
            Debug.Log("Nepøítel tì kousnul!");

            // --- PROPOJENÍ S TVÝM HEALTH SYSTÉMEM ---
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(attackDamage);
            }

            attackTimer = attackCooldown;
        }
    }

    // --- TADY DOSTÁVÁ DAMAGE NEPØÍTEL OD HYPERROLLU ---
    private void OnCollisionEnter(Collision collision)
    {
        // 1. Reagujeme jen pokud do nás narazí objekt s tagem Player
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. Najdeme HLAVNÍ skript hráèe kdekoli ve scénì (je tam jen jeden, takže je to jistota)
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();

            if (pc != null)
            {
                // 3. TADY JE TA ZMÌNA: Budeme ignorovat, co si myslí kolize, 
                // a zeptáme se pøímo skriptu, v jakém je módu.
                if (pc.IsBallMode)
                {
                    // Výpoèet poškození - pokud dashuje, dá víc
                    float damage = pc.isDashing ? 50f : 25f;
                    TakeDamage(damage);

                    // Pøidáme fyzický odraz (Knockback), aby se o sebe nezasekávali
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 knockbackDir = (transform.position - collision.transform.position).normalized;
                        knockbackDir.y = 0.5f; // Trochu ho to nadzvedne
                        rb.AddForce(knockbackDir * 15f, ForceMode.Impulse);
                    }

                    Debug.Log("ÚSPÌŠNÝ ZÁSAH KOULÍ! HP: " + enemyHealth);
                }
                else
                {
                    // Pokud nejsi koule, nepøítel tì prostì "odstrèí" nebo ty jeho, ale nic se nestane
                    Debug.Log("Kolize s hráèem v lidské formì - žádné poškození nepøítele.");
                }
            }
        }
    }
    System.Collections.IEnumerator StunEnemy()
    {
        agent.enabled = false; // Vypne mozek AI
        yield return new WaitForSeconds(0.5f); // Poèká pùl sekundy
        if (enemyHealth > 0) agent.enabled = true; // Zase zapne mozek
    }


    public void TakeDamage(float amount)
    {
        enemyHealth -= amount;
        Debug.Log("Zásah do modelu! HP: " + enemyHealth);

        // Blikneme všemi èástmi modelu
        foreach (Renderer r in childRenderers)
        {
            StartCoroutine(FlashWithPropertyBlock(r));
        }

        if (enemyHealth <= 0) Die();
    }

    IEnumerator FlashWithPropertyBlock(Renderer r)
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // 1. Získáme aktuální blok a nastavíme èervenou
        r.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", Color.red);       // Pro starší shadery
        propBlock.SetColor("_BaseColor", Color.red);   // Pro URP shadery
        r.SetPropertyBlock(propBlock);

        yield return new WaitForSeconds(flashDuration);

        // 2. Vyèistíme blok - vrátí se pùvodní barva z materiálu
        r.GetPropertyBlock(propBlock);
        propBlock.Clear();
        r.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        Debug.Log("Metoda Die se spustila!"); // Pokud tohle neuvidíš v konzoli, nepøítel neumírá správnì

        if (explosionPrefab != null)
        {
            Debug.Log("Vytváøím výbuch!");
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }
        else
        {
            Debug.LogWarning("POZOR: Chybí pøiøazený prefab výbuchu v Inspectoru!");
        }

        if (healPrefab != null)
        {
            Instantiate(healPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // --- ZBYTEK METOD ZÙSTÁVÁ STEJNÝ ---

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