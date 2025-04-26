using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using TMPro;
using System.Collections.Generic;

[Serializable]
public class SettingsData
{
    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public int resolutionIndex = 0;

    public bool postProcessing = true;
}

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private Button settingsBtn;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private Button closeBtn;

    [SerializeField] private Button soundBtn;
    [SerializeField] private Button resolutionBtn;
    [SerializeField] private Button graphicsBtn;

    [SerializeField] private GameObject soundPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Button backFromSoundBtn;

    [SerializeField] private GameObject resolutionPanel;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button backFromResBtn;
    private Resolution[] availableResolutions;

    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private Toggle postFxToggle;
    [SerializeField] private Button backFromGfxBtn;

    private SettingsData data = new SettingsData();

    private string settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.dat");
    private readonly byte[] aesKey = Encoding.UTF8.GetBytes("0123456789ABCDEF");
    private readonly byte[] aesIV = Encoding.UTF8.GetBytes("FEDCBA9876543210");

    private void Awake()
    {
        settingsBtn.gameObject.SetActive(true);
        settingsPanel.SetActive(false);
        categoryPanel.SetActive(false);
        closeBtn.gameObject.SetActive(false);
        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);
    }

    private void Start()
    {
        LoadSettings();
        ApplyAllSettings();

        settingsBtn.onClick.AddListener(OpenSettingsWindow);
        closeBtn.onClick.AddListener(CloseSettingsWindow);

        soundBtn.onClick.AddListener(() => ShowPanel(soundPanel));
        resolutionBtn.onClick.AddListener(() => ShowPanel(resolutionPanel));
        graphicsBtn.onClick.AddListener(() => ShowPanel(graphicsPanel));

        backFromSoundBtn.onClick.AddListener(ReturnToCategories);
        backFromResBtn.onClick.AddListener(ReturnToCategories);
        backFromGfxBtn.onClick.AddListener(ReturnToCategories);

        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        PopulateResolutions();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        postFxToggle.onValueChanged.AddListener(OnPostFxChanged);
    }

    private void OnDisable() => SaveSettings();

    private void OpenSettingsWindow()
    {
        settingsBtn.gameObject.SetActive(false);
        settingsPanel.SetActive(true);
        categoryPanel.SetActive(true);
        closeBtn.gameObject.SetActive(true);
    }

    private void ShowPanel(GameObject panel)
    {
        categoryPanel.SetActive(false);
        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);
        panel.SetActive(true);
    }

    private void ReturnToCategories()
    {
        ShowPanel(categoryPanel);
    }

    private void CloseSettingsWindow()
    {
        settingsPanel.SetActive(false);
        categoryPanel.SetActive(false);
        closeBtn.gameObject.SetActive(false);
        settingsBtn.gameObject.SetActive(true);
    }

    private void ApplyAllSettings()
    {
        bgmSlider.value = data.bgmVolume;
        sfxSlider.value = data.sfxVolume;
        resolutionDropdown.value = data.resolutionIndex;
        postFxToggle.isOn = data.postProcessing;

        audioMixer.SetFloat("BGMVolume", Mathf.Lerp(-80f, 0f, data.bgmVolume));
        audioMixer.SetFloat("SFXVolume", Mathf.Lerp(-80f, 0f, data.sfxVolume));
        ApplyPostFX(data.postProcessing);
    }

    private void PopulateResolutions()
    {
        availableResolutions = Screen.resolutions;
        var options = new List<string>();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            var ratio = r.refreshRateRatio;
            int hz = Mathf.RoundToInt((float)ratio.numerator / ratio.denominator);
            options.Add($"{r.width}×{r.height}@{hz}Hz");
        }
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, options.Count - 1);
        resolutionDropdown.RefreshShownValue();
        ApplyResolution(resolutionDropdown.value);
    }

    private void ApplyResolution(int idx)
    {
        var r = availableResolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }

    private void ApplyPostFX(bool on)
    {
        var vol = UnityEngine.Object.FindFirstObjectByType<Volume>();
        if (vol != null) vol.enabled = on;
    }

    private void OnBgmChanged(float value)
    {
        data.bgmVolume = value;
        audioMixer.SetFloat("BGMVolume", Mathf.Lerp(-80f, 0f, value));
        SaveSettings();
    }

    private void OnSfxChanged(float value)
    {
        data.sfxVolume = value;
        audioMixer.SetFloat("SFXVolume", Mathf.Lerp(-80f, 0f, value));
        SaveSettings();
    }

    private void OnResolutionChanged(int index)
    {
        data.resolutionIndex = index;
        ApplyResolution(index);
        SaveSettings();
    }

    private void OnPostFxChanged(bool enabled)
    {
        data.postProcessing = enabled;
        ApplyPostFX(enabled);
        SaveSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonUtility.ToJson(data);
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = EncryptAes(plain, aesKey, aesIV);
            File.WriteAllBytes(settingsFilePath, cipher);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save settings: {ex}");
        }
    }

    private void LoadSettings()
    {
        if (!File.Exists(settingsFilePath)) return;
        try
        {
            var cipher = File.ReadAllBytes(settingsFilePath);
            var plain = DecryptAes(cipher, aesKey, aesIV);
            data = JsonUtility.FromJson<SettingsData>(Encoding.UTF8.GetString(plain));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load settings: {ex}");
            data = new SettingsData();
        }
    }

    private static byte[] EncryptAes(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static byte[] DecryptAes(byte[] cipher, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
