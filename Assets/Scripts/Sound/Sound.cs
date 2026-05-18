using UnityEngine;
using UnityEngine.UI;

public class Sound : MonoBehaviour
{
    
    AudioClip backgroundMusic;
    private float defaultVolume = 0.5f;


    public static Sound Instance { get; private set; }
    private AudioSource _audioSource;


    void Awake()
    {
        // µ¥Àý
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitAudio()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.loop = true;
        _audioSource.playOnAwake = true;
        
        _audioSource.volume = defaultVolume;

        if (backgroundMusic != null)
        {
           
            _audioSource.Play();
            
        }
        
    }

   
    public void OnVolumeSliderChange(float value)
    {
        if (_audioSource != null)
        {
            
            float vol = Mathf.Clamp01(value);
            _audioSource.volume = vol;
           
        }
        
    }
}