using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragonFireballProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 100;

    private Rigidbody projectileRigidbody;
    private Collider projectileCollider;
    private Transform ignoredOwnerRoot;
    private Vector3 velocity;
    private Vector3 previousPosition;
    private bool initialized;
    private bool hasHit;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
    }

    public void Initialize(Vector3 direction, float speed, float lifetime, GameObject owner)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.Normalize();
        velocity = direction * Mathf.Max(0f, speed);
        initialized = true;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        previousPosition = transform.position;
        ignoredOwnerRoot = owner != null ? owner.transform : null;

        IgnoreOwnerColliders(owner);

        if (projectileRigidbody != null)
        {
            projectileRigidbody.useGravity = false;
            projectileRigidbody.velocity = velocity;
        }

        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void FixedUpdate()
    {
        if (!initialized || hasHit)
            return;

        Vector3 currentPosition = transform.position;
        SweepForHit(previousPosition, currentPosition);
        previousPosition = currentPosition;

        if (hasHit || projectileRigidbody != null)
            return;

        transform.position += velocity * Time.fixedDeltaTime;
    }

    private void IgnoreOwnerColliders(GameObject owner)
    {
        if (owner == null)
            return;

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider == null)
            return;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            Collider ownerCollider = ownerColliders[i];
            if (ownerCollider != null)
                Physics.IgnoreCollision(ownCollider, ownerCollider, true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void SweepForHit(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 movement = toPosition - fromPosition;
        float distance = movement.magnitude;
        if (distance <= 0.0001f)
            return;

        float radius = GetSweepRadius();
        RaycastHit[] hits = Physics.SphereCastAll(fromPosition, radius, movement.normalized, distance, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return;

        RaycastHit bestHit = default;
        bool found = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (ShouldIgnoreCollider(hit.collider))
                continue;

            if (hit.distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = hit.distance;
                found = true;
            }
        }

        if (found)
            HandleHit(bestHit.collider);
    }

    private float GetSweepRadius()
    {
        if (projectileCollider is SphereCollider sphereCollider)
            return Mathf.Max(0.05f, sphereCollider.radius * MaxAbsScale(transform.lossyScale));

        return 0.5f;
    }

    private float MaxAbsScale(Vector3 scale)
    {
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private void HandleHit(Collider hitCollider)
    {
        if (hasHit || ShouldIgnoreCollider(hitCollider))
            return;

        hasHit = true;
        TryDamageMonster(hitCollider);
        Destroy(gameObject);
    }

    private void TryDamageMonster(Collider hitCollider)
    {
        if (hitCollider == null)
            return;

        MonsterHealth monsterHealth = hitCollider.GetComponentInParent<MonsterHealth>();
        if (monsterHealth == null)
            return;

        bool groundHitReaction = monsterHealth.GetComponent<GroundInvader>() != null;
        monsterHealth.TakeDamage(damage, groundHitReaction);
    }

    private bool ShouldIgnoreCollider(Collider hitCollider)
    {
        if (hitCollider == null || hitCollider == projectileCollider || hitCollider.transform.IsChildOf(transform))
            return true;

        return ignoredOwnerRoot != null && hitCollider.transform.IsChildOf(ignoredOwnerRoot);
    }
}
