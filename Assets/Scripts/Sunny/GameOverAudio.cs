using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
