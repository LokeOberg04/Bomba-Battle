using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : NetworkBehaviour
{
    private AudioSource audioSource;

    public AudioClip gunslingerRevolverShot;

    public AudioClip gunslingerShotgunShot;
    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    [Rpc(SendTo.Everyone)]
    public void gunslingerShotSoundRpc(int index)
    {
        audioSource.PlayOneShot(index == 0 ? gunslingerRevolverShot : gunslingerShotgunShot);
    }

}
