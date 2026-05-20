using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManage : MonoBehaviour
{
    // Start is called before the first frame update
    public static MonsterManage instance { get; private set; }
    public List<Monster> monsterList = new List<Monster>();
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
