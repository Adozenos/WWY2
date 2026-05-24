using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;

public class MonsterFireBall : MonoBehaviour
{
    public GameObject target;
    public float speed;
    public Player player;
    public float AtkValue;
    private Rigidbody rb;
    private float time = 5f;
    private float Timer = 0f;

    void Start()
    {
        player = GameObject.FindWithTag("WanJia").GetComponent<Player>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (target == null)
            FindTarget();
        if (target)
            moveToTarget();
        Timer += Time.deltaTime;
        if(Timer >= time)
        {
            Destroy(this.gameObject);
        }
    }

    public void FindTarget()
    {
        float minDistance = 1000;
        foreach (Wall w in WallManager.instance.wallList)
        {
            if (w == null || w.gameObject == null) continue;

            if (Vector3.Distance(this.transform.position, w.gameObject.transform.position) < minDistance)
            {
                minDistance = Vector3.Distance(this.transform.position, w.gameObject.transform.position);
                target = w.gameObject;
            }
        }
        if (player != null && Vector3.Distance(player.gameObject.transform.position, this.transform.position) < minDistance)
            target = player.gameObject;
    }

    public void moveToTarget()
    {
        transform.LookAt(target.transform);

        Vector3 newPos = transform.position + transform.forward * speed * Time.deltaTime;
        if (rb != null)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("碰到：" + other.name);

        if (WallAttackTargetManager.instance != null &&
            WallAttackTargetManager.instance.TryDamageWall(other.gameObject, AtkValue))
        {
            Debug.Log("打到墙");
            Destroy(gameObject);
            return;
        }

        Player hitPlayer = other.GetComponentInParent<Player>();
        if (hitPlayer != null)
        {
            Debug.Log("打到玩家");
            hitPlayer.HP -= AtkValue;
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
