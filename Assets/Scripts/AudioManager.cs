using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // General Variables
    public static AudioManager instance;
    [SerializeField] private Sound[] Sounds = null;
    private List<Sound> menuSoundtracks = new List<Sound>();
    private List<string> menuSoundtrackNamesBlacklist = new List<string>();
    private List<Sound> multiplayerSoundtracks = new List<Sound>();
    private List<string> multiplayerSoundtrackNamesBlacklist = new List<string>();

    // Menu variables
    public bool isSoundtrackEnabled = true;
    public enum ThemeType { MENU, MULTIPLAYER };
    public ThemeType themeType = ThemeType.MENU;

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
        foreach(Sound sound in Sounds)
        {
            if (sound.clip.name.Contains("MenuTheme"))
                menuSoundtracks.Add(sound);
            else if (sound.clip.name.Contains("MultiplayerTheme"))
                multiplayerSoundtracks.Add(sound);

            sound.SetAsAudioSourceToGameObject(gameObject);
        }
    }

    private void Update()
    {
        if(isSoundtrackEnabled)
        {
            bool noSoundtrackPlaying = true;
            switch (themeType)
            {
                case ThemeType.MENU:
                    foreach (Sound soundtrack in menuSoundtracks)
                        if (soundtrack.source.isPlaying)
                        {
                            noSoundtrackPlaying = false;
                            break;
                        }
                        break;


                case ThemeType.MULTIPLAYER:
                    foreach (Sound soundtrack in multiplayerSoundtracks)
                        if (soundtrack.source.isPlaying)
                        {
                            noSoundtrackPlaying = false;
                            break;
                        }
                    break;
            }

            if (noSoundtrackPlaying)
                PlayRandomSoundtrack();
        }
    }

    /// <summary>
    /// Play the sound named name found within the AudioManager's array.
    /// </summary>
    public void PlayGlobalSound(string name)
    {
        Sound s = Array.Find(Sounds, sound => sound.clip.name == name);
        if (s != null)
            s.source.Play();
        else
            Debug.LogWarning("The sound named " + name + " could not be found!");
    }

    /// <summary>
    /// Create a temporary empty game object used only for playing the sound sound. The object will automatically despawn once the sound 
    /// finished playing.
    /// </summary>
    /// <param name="sound"> The Sound file you wish to play </param>
    /// <param name="position"> The position in world space you want the sound to be played from. This matters only if it is set as a 3D sound. </param>
    public GameObject CreateSoundObject(Sound sound, Vector3 position)
    {
        // Create a new gameObject used only for playing the sound
        GameObject soundObject = new GameObject("[SoundObject]" + sound.clip.name);
        soundObject.transform.position = position;
        
        // Starts playing the sound
        sound.SetAsAudioSourceToGameObject(soundObject).Play();
        
        // Give the object a lifetime equal to the duration of the sound
        soundObject.AddComponent<Lifetime>().Seconds = sound.clip.length;

        return soundObject;
    }
    public void CreateSoundObject(Sound sound)
    {
        CreateSoundObject(sound, Vector3.zero);
    }

    /// <summary>
    /// Stop all the sounds containing substring found within the AudioManager's array.
    /// </summary>
    public void StopGlobalSoundsContaining(string substring)
    {
        foreach(Sound s in Sounds)
        {
            if (s.clip.name.Contains(substring))
            {
                s.source.Stop();
            }
        }
    }

    public Sound PlayRandomSoundtrack()
    {
        Sound soundtrackToPlay = null;
        switch (themeType)
        {
            case ThemeType.MENU:
                if (menuSoundtrackNamesBlacklist.Count == menuSoundtracks.Count)
                    menuSoundtrackNamesBlacklist.Clear();

                do
                {
                    soundtrackToPlay = menuSoundtracks[UnityEngine.Random.Range(0, menuSoundtracks.Count)];
                } while (menuSoundtrackNamesBlacklist.Contains(soundtrackToPlay.clip.name));
                menuSoundtrackNamesBlacklist.Add(soundtrackToPlay.clip.name);

                soundtrackToPlay.source.Play();
                break;


            case ThemeType.MULTIPLAYER:
                if (multiplayerSoundtrackNamesBlacklist.Count == multiplayerSoundtracks.Count)
                    multiplayerSoundtrackNamesBlacklist.Clear();

                do
                {
                    soundtrackToPlay = multiplayerSoundtracks[UnityEngine.Random.Range(0, multiplayerSoundtracks.Count)];
                } while (multiplayerSoundtrackNamesBlacklist.Contains(soundtrackToPlay.clip.name));
                multiplayerSoundtrackNamesBlacklist.Add(soundtrackToPlay.clip.name);

                soundtrackToPlay.source.Play();
                break;
        }

        return soundtrackToPlay;
    }

    public void StopAllPlayingSoundtracks()
    {
        menuSoundtrackNamesBlacklist.Clear();
        foreach (Sound sound in menuSoundtracks)
            if (sound.source.isPlaying)
                sound.source.Stop();

        multiplayerSoundtrackNamesBlacklist.Clear();
        foreach (Sound sound in multiplayerSoundtracks)
            if (sound.source.isPlaying)
                sound.source.Stop();
    }

    public bool ToggleSoundtrack()
    {
        isSoundtrackEnabled = !isSoundtrackEnabled;
        switch(isSoundtrackEnabled)
        {
            case false:
                StopAllPlayingSoundtracks();
                break;


            case true:
                PlayRandomSoundtrack();
                break;
        }

        return isSoundtrackEnabled;
    }
}
