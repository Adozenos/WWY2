using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterFireBall : MonoBehaviour
{
    public GameObject target;
    public float speed;
    public Player player;
    public float AtkValue;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("WanJia").GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            FindTarget();
        if (target)
            moveToTarget();
    }
    public void FindTarget()
    {
        float minDistance = 1000;
        foreach (Wall w in WallManager.instance.wallList)
        {
            if (Vector3.Distance(this.transform.position, w.gameObject.transform.position) < minDistance)
            {
                minDistance = Vector3.Distance(this.transform.position, w.gameObject.transform.position);
                target = w.gameObject;
            }
        }
        if (Vector3.Distance(player.gameObject.transform.position, this.transform.position) < minDistance)
            target = player.gameObject;
    }
    public void moveToTarget()
    {
        transform.LookAt(target.transform);

        transform.position +=
            transform.forward *
            speed *
            Time.deltaTime;
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("碰到：" + other.name);

        foreach (Wall w in WallManager.instance.wallList)
        {
            if (w.gameObject == other.gameObject)
            {
                Debug.Log("打到墙");
                w.HP -= AtkValue;
            }
        }

        if (other.gameObject == player.gameObject)
        {
            Debug.Log("打到玩家");
            player.HP -= AtkValue;
        }

        Destroy(gameObject);
    }
}
