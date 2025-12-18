
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Testing
/*
public class GameOverAudio : MonoBehaviour
{
    public AudioSource sound1;
    public AudioSource sound2;
    public AudioSource sound3;

    public void PlayGameOverSounds()
    {
        sound1?.Play();
        sound2?.Play();
        sound3?.Play();
    }

    public void StopGameOverSounds()
    {
        if (sound1 != null && sound1.isPlaying) sound1.Stop();
        if (sound2 != null && sound2.isPlaying) sound2.Stop();
        if (sound3 != null && sound3.isPlaying) sound3.Stop();
    }
}
*/


// For later

public class GameClearAudio : MonoBehaviour
{
    public AudioSource clearsound1;
    public AudioSource clearsound2;
    public AudioSource clearsound3;
    
    public void PlayGameClearSounds()
    {
        clearsound1?.Play();
        clearsound2?.Play();
        clearsound3?.Play();
        
    }

    
    public void StopGameClearSounds()
    {
        if (clearsound1 != null && clearsound1.isPlaying) clearsound1.Stop();
        if (clearsound2 != null && clearsound2.isPlaying) clearsound2.Stop();
        if (clearsound2 != null && clearsound3.isPlaying) clearsound3.Stop();
        
    }
}
