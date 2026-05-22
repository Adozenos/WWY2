using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class FlyMonster : MonoBehaviour
{
    // Start is called before the first frame update
    // private float groudY;
    public float takeOffSpeed;
    public float fightHeight;
    public Animator ani;
    // public float ChaseRange;
    public float AtackRange;
    public float FlyForwardSpeed;
    float rotateSpeed = 10;
    public Transform FirePoint;
    public GameObject FlamePrefab;
    public Player player;
    private bool firstTakeOff = true;
    public float HP=100;
    public float fallSpeed;
    enum FlyMonsterState
    {
        Float,
        Idle,       //µØÃæ´ý»ú
        TakeOff,    //Æð·É
        Chase,      //¿ÕÖÐ×·»÷
        Atack,       //Åç»ð
        Retreat,    //ºó³·
        Land,       //½µÂä
        Dead
    }
    public GameObject target;
    [SerializeField]
    FlyMonsterState state = FlyMonsterState.Idle;
    void Start()
    {
        player = GameObject.FindWithTag("WanJia").GetComponent<Player>();
        MonsterManage.instance.flyMonsterList.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        FindTarget();
        switch (state)
        {
            case FlyMonsterState.Idle:
                UpdateIdle();
                break;

            case FlyMonsterState.TakeOff:
                UpdateTakeOff();
                break;

            case FlyMonsterState.Chase:
                UpdateChase();
                break;

            case FlyMonsterState.Atack:
                UpdateAtack();
                break;

            case FlyMonsterState.Retreat:
                UpdateRetreat();
                break;

            case FlyMonsterState.Land:
                UpdateLand();
                break;
            case FlyMonsterState.Float:
                UpdateFloat();
                break;
            case FlyMonsterState.Dead:
                UpdateDead();
                break;
        }


    }



    private void UpdateFloat()
    {
        throw new NotImplementedException();
    }

    public void UpdateIdle()
    {
        ani.SetBool("IsIdle", true);
        Invoke("TransitionToTakeOff", 3);
    }
    public void TransitionToTakeOff()
    {
        Debug.Log("tttakeoff");
        ani.SetBool("IsIdle", false);
        ani.SetBool("IsAtack", false);
        ani.SetBool("IsFlyForward", false);
        ani.SetBool("IsFloat", false);
        state = FlyMonsterState.TakeOff;
    }

    public void UpdateTakeOff()
    {
        TakeOff();

    }

    private void TakeOff()
    {
        if (transform.position.y <= target.transform.position.y + fightHeight)
        {
            if (firstTakeOff)
                ani.SetBool("IsTakeOff", true);
            else
                ani.SetBool("IsFloat", true);
            Vector3 pos = transform.position;
            pos.y += takeOffSpeed * Time.deltaTime;
            transform.position = pos;
            firstTakeOff = false;
        }

        if (transform.position.y >= target.transform.position.y + fightHeight + 5)
        {
            ani.SetBool("IsFloat", true);
            Vector3 pos = transform.position;
            pos.y -= takeOffSpeed * Time.deltaTime;
            transform.position = pos;
        }
        if (transform.position.y < target.transform.position.y + fightHeight + 5 && transform.position.y > target.transform.position.y + fightHeight)
        {
            Vector3 p1 = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 p2 = new Vector3(target.transform.position.x, 0, target.transform.position.z);

            float distance = Vector3.Distance(p1, p2);
            if (distance > AtackRange)
            {

                TransitionToChase();
            }
            else
            {
                TransitionToAtack();
            }

        }
    }

    private void TransitionToChase()
    {
        ani.SetBool("IsTakeOff", false);
        ani.SetBool("IsIdle", false);
        ani.SetBool("IsAtack", false);
        ani.SetBool("IsFloat", false);
        state = FlyMonsterState.Chase;
    }

   
    private void UpdateChase()
    {
        transform.LookAt(target.transform);
        ani.SetBool("IsFlyForward", true);
        Vector3 pos = new Vector3(target.transform.position.x, target.transform.position.y + fightHeight, target.transform.position.z);
        Debug.Log("chase");
        transform.LookAt(target.transform);
        transform.position = Vector3.MoveTowards(
            transform.position,
            pos,
            FlyForwardSpeed * Time.deltaTime);
        Vector3 p1 = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 p2 = new Vector3(target.transform.position.x, 0, target.transform.position.z);
        float hoizentaldistance = Vector3.Distance(p1, p2);
        if (hoizentaldistance <= AtackRange)
        {
            TransitionToAtack();
        }

    }

    private void TransitionToAtack()
    {
        Debug.Log("ttatack");
        ani.SetBool("IsIdle", false);
        ani.SetBool("IsFlyForward", false);
        ani.SetBool("IsFloat", false);
        state = FlyMonsterState.Atack;
    }
    private void UpdateAtack()
    {
        transform.LookAt(target.transform);
        ani.SetBool("IsAtack", true);
        Vector3 p1 = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 p2 = new Vector3(target.transform.position.x, 0, target.transform.position.z);
        float hoizentaldistance = Vector3.Distance(p1, p2);
        if(transform.position.y <= target.transform.position.y + fightHeight|| transform.position.y >= target.transform.position.y + fightHeight+5)
        {
            TransitionToTakeOff();
            return;
        }
        if (hoizentaldistance > AtackRange)
        {
            TransitionToChase();
        }       
    }
    public void GetHit(float AtkValue)
    {
        ani.SetTrigger("GetHit");
        HP -= AtkValue;
        if (HP <= 0)
        {         
            TransitionToDead();           
            MonsterManage.instance.flyMonsterList.Remove(this);
            return;
        }
        Debug.Log("gethit");
        TransitionToTakeOff();
    }

    private void TransitionToDead()
    {
        ani.SetBool("IsIdle", false);
        ani.SetBool("IsAtack", false);
        ani.SetBool("IsFlyForward", false);
        ani.SetBool("IsFloat", false);
        state = FlyMonsterState.Dead;
        Destroy(this.gameObject,5);
    }
    private void UpdateDead()
    {
         Rigidbody rb;
     BoxCollider boxCollider;
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        ani.SetBool("IsDie", true);
        rb.useGravity = true;
        rb.isKinematic = false;
        boxCollider.isTrigger = false;
        //    Vector3 targetPos =
        //new Vector3(transform.position.x, transform.position.y-target.gameObject.transform.position.y-fightHeight, transform.position.z);

        //    transform.position = Vector3.MoveTowards(
        //        transform.position,
        //        targetPos,
        //        fallSpeed * Time.deltaTime
        //);

    }
    private void Fire()
    {
        GameObject go = GameObject.Instantiate(FlamePrefab, FirePoint.position, Quaternion.identity);
    }
    public void FindTarget()
    {
        float minDistance = 10000;
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
    private void UpdateLand()
    {
        throw new NotImplementedException();
    }

    private void UpdateRetreat()
    {
        throw new NotImplementedException();
    }


}
