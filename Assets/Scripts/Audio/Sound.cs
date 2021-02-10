using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SoundCategory { MenuSoundtrack, MatchSoundtrack, Announcer, UI, Environment, Unknown }

[System.Serializable]
public class Sound
{
    [SerializeField] 
    private AudioClip clip = null;
    private AudioSource source = null;

    public float Volume { get; set; } = 1f;
    public bool Loop { get; set; } = false;
    public bool AlreadyPlayed { get; set; } = false;
    public bool IsPaused { get; private set; } = false;

    public AudioSource SetAudioSource(GameObject gameObject)
    {
        // Adding the audio source component to the gameObject. Passes all the values inserted into the inspector to each sound
        // to the added AudioSource component. Also, each Sound class has a reference attached to its AudioSource. 
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Volume;
        source.loop = Loop;

        return source;
    }

    public void Play() { source.Play(); AlreadyPlayed = true; }
    public void Stop() => source.Stop();
    public void Pause() { if (source.isPlaying) { source.Pause(); IsPaused = true; } }
    public void Resume() { if (IsPaused) { source.UnPause(); IsPaused = false; } }
    public bool IsPlaying => source.isPlaying;
    public string Name => clip.name;
    public float Length => clip.length;
}

[System.Serializable]
public class SoundList
{
    [HideInInspector] public string name;
    public SoundCategory category = SoundCategory.Unknown;
    [Range(0f, 1f)]
    public float volume = 0.25f;
    public bool loop = false;
    public List<Sound> sounds = null;


    /// <summary>
    /// get => Checks if all the sounds in this list have already been played.
    /// set => Sets all the sounds in this list to already played or not.
    /// </summary>
    public bool AllSoundsAlreadyPlayed
    {
        get => sounds.All(sound => sound.AlreadyPlayed);
        set => sounds.ToList().ForEach(sound => sound.AlreadyPlayed = value);
    }

    /// <summary>
    /// Get a random sound in the set which contains the given string within its clip name.
    /// </summary>
    /// <param name="name"> The string to search for. </param>
    /// <returns> The sound you're looking for. </returns>
    public Sound this[string name]
    {
        get
        {
            var ans_sounds = this.sounds.Where(s => s.Name.ToLower().Contains(name.ToLower()));
            return ans_sounds.ElementAt(UnityEngine.Random.Range(0, ans_sounds.Count()));
        }
    }
}

[System.Serializable]
public class SoundListHolder
{
    [SerializeField] public List<SoundList> soundLists = null;

    public SoundList this[SoundCategory category] => soundLists.First(list => list.category == category);
}
