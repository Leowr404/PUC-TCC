using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public bool isMusic;
    Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;

        var am = AudioManager.instancia;
        string key = isMusic ? AudioManager.MusicVolumeKey : AudioManager.SFXVolumeKey;
        slider.SetValueWithoutNotify(am.GetSavedVolume(key));

        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnChanged(float v)
    {
        if (isMusic) AudioManager.instancia.SetMusicVolume(v);
        else AudioManager.instancia.SetSFXVolume(v);
    }

    void OnDisable() => AudioManager.instancia?.SaveSettings(); // salva ao fechar o menu
}