using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public AudioSource aud;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "StartScene")
        {
            aud.Stop();
        }
        if (SceneManager.GetActiveScene().name == "DemoScene")
        {
            aud.Play();
        }
    }
}
