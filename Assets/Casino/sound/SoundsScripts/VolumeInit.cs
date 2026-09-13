using UnityEngine;
using UnityEngine.Audio;

public class VolumeInit : MonoBehaviour
{
    public string volumeParametr = "MasterVolume";
    [SerializeField] public AudioMixer volumeMixer;
    void Start()
    {
        var volumeValue = PlayerPrefs.GetFloat(volumeParametr, volumeParametr == "Slots" ? 0 : -80f);
        volumeMixer.SetFloat(volumeParametr, volumeValue);
    }
}
