using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    public NavMeshAgent navMesh;
    public GameObject target;
    public Player player;
    public float speed = 0;
    public Animator ani;
    public float HP = 100;
    public bool isAlive=true;
    public float AtcValue;
    public float atackRange;
    public float chaseRange;
    public float walkSpeed;
    public float runSpeed;
    enum MoveState
    {
        Idle,
        run,
        walk
    }
     MoveState moveState = MoveState.Idle;
    // Start is called before the first frame update
    void Start()
    {
        //player = GameObject.FindWithTag("Player").GetComponent<Player>();
        GameObject obj = GameObject.FindWithTag("WanJia");
        player = obj.GetComponent<Player>();
        MonsterManage.instance.monsterList.Add(this);
        navMesh = GetComponent<NavMeshAgent>();
        target = player.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (HP>0)
        {
            AliveUpdate();
        }

    }
    public void TransitionToWalk()
    {
        navMesh.speed = walkSpeed;
        ani.SetBool("IsWalk", true);
        ani.SetBool("IsRun", false);
        ani.SetBool("IsAtack", false);
    }
    public void TransitionToun()
    {
        navMesh.speed = runSpeed;
        ani.SetBool("IsWalk", true);
        ani.SetBool("IsRun", true);
        ani.SetBool("IsAtack", false);
    }
    public void TransitionToAtack()
    {
        ani.SetBool("IsWalk", false);
        ani.SetBool("IsRun", false);
        navMesh.speed = 0;
        ani.SetBool("IsAtack", true);
    }
    public void Die()
    {
        if (isAlive == false) return;
        isAlive = false;
        ani.SetBool("IsWalk", false);
        ani.SetBool("IsRun", false);
        ani.SetBool("IsAtack", false);
        ani.SetTrigger("Die");
        MonsterManage.instance.monsterList.Remove(this);
        Destroy(this.gameObject,5);

    }
    public void AliveUpdate()
    {
        ChoseTarget();
        navMesh.SetDestination(target.transform.position);
        if (Vector3.Distance(transform.position, target.transform.position) >= chaseRange)
        {
            moveState = MoveState.run;
            TransitionToun();
        }
        else if (Vector3.Distance(transform.position, target.transform.position) < chaseRange &&
            Vector3.Distance(transform.position, target.transform.position) >= atackRange)
        {
            moveState = MoveState.run;
            TransitionToWalk();
        }
        else if (Vector3.Distance(transform.position, target.transform.position) < atackRange)
        {
            TransitionToAtack();
        }
    }

    public GameObject go;
    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.Log("enter");

    //    if (other.CompareTag("WanJia"))
    //    {
    //        go = other.gameObject;
    //    }
    //    if (other.CompareTag("Wall"))
    //    {
    //        go = other.gameObject;
    //    }
    //}
    //private void OnTriggerExit(Collider other)
    //{
    //    Debug.Log("exit");

    //    if (other.CompareTag("Player"))
    //    {
    //        go = null;
    //    }
    //    if (other.CompareTag("Wall")&&target.gameObject!=go)
    //    {
    //        go = null;
    //    }

    //}
    public void TackDamage()
    {
        // Debug.Log("TackDamage");
        transform.LookAt(target.transform);
        Player p = target.GetComponent<Player>();
        Wall w = target.GetComponent<Wall>();
        if (p) p.HP -= AtcValue;
        if (w) w.HP -= AtcValue;
    }
    public void ChoseTarget()
    {
        float minDistance=10000;
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
    public void GetHit(float AtkValue)
    {
        if (isAlive == false) return;
        Debug.Log("GetHit");
        ani.SetTrigger("GetHit");
        HP -= AtkValue;
        if (HP <= 0)
        {
            Die();
        }
    }
}
