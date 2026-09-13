using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    public string volumeParametr = "MasterVolume";
    [SerializeField] public AudioMixer volumeMixer;
    [SerializeField] public Slider volumeSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float _multiplier = 20f;
    private float _volumeValue;
    private void Awake()
    {
        volumeSlider.onValueChanged.AddListener(HadleSliderValueChanged);
    }
    private void HadleSliderValueChanged(float value)
    {
        var volumeValue = Mathf.Log10(value) * _multiplier;
        volumeMixer.SetFloat(volumeParametr, volumeValue);
    }
    void Start()
    {
        _volumeValue = PlayerPrefs.GetFloat(volumeParametr, Mathf.Log10(volumeSlider.value) * _multiplier);
        volumeSlider.value = Mathf.Pow(10f, _volumeValue / _multiplier);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(volumeParametr, _volumeValue);
    }
}
