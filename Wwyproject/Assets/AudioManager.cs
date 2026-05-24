using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource aud;
    public static AudioManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        aud = GetComponent<AudioSource>();
        //  PlayBgm(Config.bgm1);
    }
    void Update()
    {
        
    }
    public void PlayBgm(string path)
    {
        Debug.Log("PlayBGM");
        AudioClip ac = Resources.Load<AudioClip>(path);
        aud.clip = ac;
        aud.Play();
    }
    public void PlayBGM()
    {

    }
    public void StopBgm()
    {
        aud.Stop();
    }
    public void PlayClip(string path, float volume = 1)
    {
        AudioClip ac = Resources.Load<AudioClip>(path);
        AudioSource.PlayClipAtPoint(ac, transform.position, volume);

    }
    public void PlayBgm1()
    {
        AudioClip ac = Resources.Load<AudioClip>(config.bgm1);
        AudioSource.PlayClipAtPoint(ac, transform.position, 1);
    }
}
