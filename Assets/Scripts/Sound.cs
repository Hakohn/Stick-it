using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SoundCategory { MenuSoundtrack, MatchSoundtrack, Announcer, UI, Environment, Unknown }

[System.Serializable]
public class Sound
{
    [SerializeField] 
    private AudioClip clip;
    private AudioSource source;

    public float Volume { get; set; } = 1f;
    public bool Loop { get; set; } = false;
    public bool AlreadyPlayed { get; set; } = false;

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
    public bool IsPlaying => source.isPlaying;
    public string Name => clip.name;
    public float Length => clip.length;
}

[System.Serializable]
public class SoundSet
{
    public SoundCategory category = SoundCategory.Unknown;
    [Range(0f, 1f)]
    public float volume = 0.25f;
    public bool loop = false;
    public List<Sound> soundSet = null;


    /// <summary>
    /// get => Checks if all the sounds in this list have already been played.
    /// set => Sets all the sounds in this list to already played or not.
    /// </summary>
    public bool AllSoundsAlreadyPlayed
    {
        get => soundSet.All(sound => sound.AlreadyPlayed);
        set => soundSet.ToList().ForEach(sound => sound.AlreadyPlayed = value);
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
            var sounds = soundSet.Where(s => s.Name.ToLower().Contains(name.ToLower()));
            return sounds.ElementAt(UnityEngine.Random.Range(0, sounds.Count()));
        }
    }
}

[System.Serializable]
public class SoundSetHolder
{
    [SerializeField] public List<SoundSet> container = null;

    public SoundSet this[SoundCategory category] => container.First(set => set.category == category);
}
