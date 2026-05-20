using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("First song to play")]
    public AudioClip firstSong;
    [Tooltip("Second song to play")]
    public AudioClip secondSong;
    [Tooltip("Volume for the background music")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;
    private bool isPlayingFirstSong = true;

    void Start()
    {
        // Create and set up AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false; // We'll handle looping manually
        audioSource.volume = volume;
        audioSource.playOnAwake = true;

        // Start playing the first song
        if (firstSong != null)
        {
            audioSource.clip = firstSong;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Check if the current song has finished playing
        if (!audioSource.isPlaying)
        {
            // Switch to the other song
            isPlayingFirstSong = !isPlayingFirstSong;
            audioSource.clip = isPlayingFirstSong ? firstSong : secondSong;
            audioSource.Play();
        }
    }

    // Method to change volume at runtime
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
} 