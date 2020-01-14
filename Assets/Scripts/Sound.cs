using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    public AudioClip clip;
    [HideInInspector] public AudioSource source;

    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;

    public AudioSource SetAsAudioSourceToGameObject(GameObject gameObject)
    {
        // Adding the audio source component to the gameObject. Passes all the values inserted into the inspector to each sound
        // to the added AudioSource component. Also, each Sound class has a reference attached to its AudioSource. 
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;

        return source;
    }
}