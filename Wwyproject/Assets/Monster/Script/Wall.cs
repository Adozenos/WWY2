using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    // Start is called before the first frame update
    
    public float HP = 100;

    void Start()
    {
        WallManager.instance.wallList.Add(this);
    }
    // Update is called once per frame
    void Update()
    {
        if (HP <= 0)
        {
            Destroy(this.gameObject);
            WallManager.instance.wallList.Remove(this);
        }
    }
}
