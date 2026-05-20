using UnityEngine;

public class FireBall : MonoBehaviour
{
    public Monster target;
    public float speed = 5;
    public float AtkValue = 20;

    void Start()
    {
        ChoseTarget();
    }

    void Update()
    {
        if (target == null)
        {
            ChoseTarget();
        }

        if (target != null)
        {
            moveToTarget();
        }
    }

    public void ChoseTarget()
    {
        float minDistance = 1000;

        foreach (Monster m in MonsterManage.instance.monsterList)
        {
            float dis = Vector3.Distance(
                m.transform.position,
                transform.position);

            if (dis < minDistance)
            {
                minDistance = dis;
                target = m;
            }
        }
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
        Monster m = other.GetComponent<Monster>();

        if (m != null)
        {
            m.GetHit(AtkValue);
        }

        Destroy(gameObject);
    }
}