using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class RotateTest : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject headCamera;
    public GameObject dragon;
    public GameObject player;
    public GameObject point;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        dragon.transform.localRotation = headCamera.transform.localRotation;
        player.transform.position = point.transform.position;
    }
}
