using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // General Variables
    public static AudioManager instance;
    [SerializeField] private SoundSetHolder soundsetHolder = null;

    // Menu variables
    public bool IsSoundtrackEnabled = true;
    public SoundCategory soundtrackCategory = SoundCategory.MenuSoundtrack;

    // Start is called before the first frame update
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Setting up the sounds
        foreach(SoundSet set in soundsetHolder.container)
            foreach(Sound s in set.soundSet)
            {
                s.Volume = set.volume;
                s.Loop = set.loop;
                s.SetAudioSource(gameObject);
            }
    }

    private bool AnySoundtrackPlaying()
    {
        foreach (SoundCategory category in new List<SoundCategory>() { SoundCategory.MenuSoundtrack, SoundCategory.MatchSoundtrack })
            foreach (Sound s in soundsetHolder[category].soundSet)
                if (s.IsPlaying)
                    return true;
        return false;
    }

    private void Update()
    {
        if(IsSoundtrackEnabled)
            if (AnySoundtrackPlaying() == false)
                PlayRandomSoundtrack();
    }

    /// <summary>
    /// Play the sound named name found within the AudioManager's array.
    /// </summary>
    public void PlayGlobalSound(SoundCategory category, string name)
    {
        Sound s = soundsetHolder[category][name];
        if (s != null)
        {
            s.Play();
        }
        else
            Debug.LogWarning($"No sound containing the substring '{name}' could be found in the sound category '{category}'!");
    }

    /// <summary>
    /// Create a temporary empty game object used only for playing the sound. The object will automatically despawn once the sound 
    /// finished playing.
    /// </summary>
    /// <param name="sound"> The Sound file you wish to play </param>
    /// <param name="position"> The position in world space you want the sound to be played from. This matters only if it is set as a 3D sound. </param>
    public static GameObject CreateSoundObject(Sound sound, Vector3 position)
    {
        // Create a new gameObject used only for playing the sound
        GameObject soundObject = new GameObject("[SoundObject]" + sound.Name);
        soundObject.transform.position = position;
        
        // Starts playing the sound
        sound.SetAudioSource(soundObject).Play();
        
        // Give the object a lifetime equal to the duration of the sound
        soundObject.AddComponent<Lifetime>().Seconds = sound.Length;

        return soundObject;
    }
    public static void CreateSoundObject(Sound sound)
    {
        CreateSoundObject(sound, Camera.main.transform.position);
    }

    /// <summary>
    /// Stop all the sounds from the given category
    /// </summary>
    public void StopAllGlobalSounds(SoundCategory category)
    {
        foreach(Sound s in soundsetHolder[category].soundSet)
            s.Stop();
    }

    public void PlayRandomSoundtrack()
    {
        var playableSoundtracks = soundsetHolder[soundtrackCategory].soundSet.Where(s => s.AlreadyPlayed == false) as List<Sound>;
        if (playableSoundtracks == null || playableSoundtracks.Count == 0)
        {
            soundsetHolder[soundtrackCategory].AllSoundsAlreadyPlayed = false;
            playableSoundtracks = soundsetHolder[soundtrackCategory].soundSet;
        }
        playableSoundtracks.ElementAt(UnityEngine.Random.Range(0, playableSoundtracks.Count)).Play();
    }

    public void StopAllPlayingSoundtracks()
    {
        foreach (SoundCategory category in new List<SoundCategory>() { SoundCategory.MenuSoundtrack, SoundCategory.MatchSoundtrack })
            StopAllGlobalSounds(category);
    }

    public bool ToggleSoundtrack()
    {
        IsSoundtrackEnabled = !IsSoundtrackEnabled;
        switch(IsSoundtrackEnabled)
        {
            case false:
                StopAllPlayingSoundtracks();
                break;


            case true:
                PlayRandomSoundtrack();
                break;
        }

        return IsSoundtrackEnabled;
    }
}
