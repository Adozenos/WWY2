using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFireBall : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    public float speed = 5;
    public float AtkValue = 20;
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            ChoseTarget();
        }
        if(target)
        moveToTarget();

    }
    public void ChoseTarget()
    {
        float minDistance = 1000;
        foreach(Monster m in MonsterManage.instance.monsterList)
        {
            
            if (Vector3.Distance(m.gameObject.transform.position,transform.position)<minDistance)
            {
                minDistance = Vector3.Distance(m.gameObject.transform.position, transform.position);
                target = m.gameObject;
                Debug.Log(minDistance);
            }
        }
        foreach (FlyMonster fm in MonsterManage.instance.flyMonsterList)
        {

            if (Vector3.Distance(fm.gameObject.transform.position, transform.position) < minDistance)
            {
                minDistance = Vector3.Distance(fm.gameObject.transform.position, transform.position);
                target = fm.gameObject;
                Debug.Log(minDistance);
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
            Destroy(gameObject);
            return;
        }

        FlyMonster fm = other.GetComponent<FlyMonster>();
        if (fm != null)
        {
            fm.GetHit(AtkValue);
            Destroy(gameObject);
            return;
        }
        Destroy(this.gameObject);
    }
}
