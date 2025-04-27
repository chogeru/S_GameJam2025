using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

[Serializable]
public class SettingsData
{
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public int resolutionIndex = 0;
    public bool postProcessing = true;
}

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button settingsBtn;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform settingsPanelRT;
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private Button closeBtn;

    [SerializeField] private Button soundBtn;
    [SerializeField] private Button resolutionBtn;
    [SerializeField] private Button graphicsBtn;

    [SerializeField] private GameObject soundPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
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

    private Dictionary<Button, Vector3> initialScales = new Dictionary<Button, Vector3>();

    private void Awake()
    {
        RecordInitialScales();

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

        settingsBtn.onClick.AddListener(OnSettingsBtnClicked);
        closeBtn.onClick.AddListener(CloseSettingsWindow);
        soundBtn.onClick.AddListener(() => TransitionPanels(categoryPanel, soundPanel));
        resolutionBtn.onClick.AddListener(() => TransitionPanels(categoryPanel, resolutionPanel));
        graphicsBtn.onClick.AddListener(() => TransitionPanels(categoryPanel, graphicsPanel));
        backFromSoundBtn.onClick.AddListener(() => TransitionPanels(soundPanel, categoryPanel));
        backFromResBtn.onClick.AddListener(() => TransitionPanels(resolutionPanel, categoryPanel));
        backFromGfxBtn.onClick.AddListener(() => TransitionPanels(graphicsPanel, categoryPanel));

        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        PopulateResolutions();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        postFxToggle.onValueChanged.AddListener(OnPostFxChanged);

        ResetButtonScalesInstant();

        AddHoverAnimation(soundBtn);
        AddHoverAnimation(resolutionBtn);
        AddHoverAnimation(graphicsBtn);
        AddHoverAnimation(closeBtn);
    }

    private void RecordInitialScales()
    {
        initialScales[settingsBtn] = settingsBtn.transform.localScale;
        initialScales[closeBtn] = closeBtn.transform.localScale;
        initialScales[soundBtn] = soundBtn.transform.localScale;
        initialScales[resolutionBtn] = resolutionBtn.transform.localScale;
        initialScales[graphicsBtn] = graphicsBtn.transform.localScale;
    }

    private void ResetButtonScalesInstant()
    {
        settingsBtn.transform.localScale = initialScales[settingsBtn];
        closeBtn.transform.localScale = initialScales[closeBtn];
    }

    private void OnDisable() => SaveSettings();

    private void OnSettingsBtnClicked()
    {
        var initScale = initialScales[settingsBtn];
        settingsBtn.transform
            .DOScale(initScale * 0.9f, 0.1f).SetEase(Ease.InOutQuad)
            .OnComplete(() => settingsBtn.transform
                .DOScale(initScale, 0.1f).SetEase(Ease.InOutQuad)
                .OnComplete(OpenSettingsWindow));
    }

    private void OpenSettingsWindow()
    {
        ResetButtonScalesInstant();

        settingsBtn.gameObject.SetActive(false);
        settingsPanel.SetActive(true);
        categoryPanel.SetActive(true);
        closeBtn.gameObject.SetActive(true);

        settingsPanelRT.localScale = Vector3.one * 0.7f;
        settingsPanelRT.localRotation = Quaternion.Euler(0, 0, -7f);
        var seq = DOTween.Sequence();
        seq.Append(settingsPanelRT
            .DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        seq.Join(settingsPanelRT
            .DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack));
        seq.Play();
    }

    private void CloseSettingsWindow()
    {
        closeBtn.interactable = false;

        HideAllSubPanels();

        var seq = DOTween.Sequence();
        seq.Append(settingsPanelRT
            .DOScale(0.94f, 0.08f)
            .SetEase(Ease.InSine));
        seq.Append(settingsPanelRT
            .DOScale(0.5f, 0.45f)
            .SetEase(Ease.InBack));
        seq.Join(settingsPanelRT
            .DOAnchorPosY(-Screen.height, 0.45f)
            .SetEase(Ease.InBack));
        seq.Join(settingsPanelRT
            .DORotate(new Vector3(0, 0, 15f), 0.45f)
            .SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            settingsPanel.SetActive(false);
            settingsBtn.gameObject.SetActive(true);

            settingsPanelRT.anchoredPosition = Vector2.zero;
            settingsPanelRT.localScale = Vector3.one;
            settingsPanelRT.localRotation = Quaternion.identity;

            closeBtn.interactable = true;
            closeBtn.gameObject.SetActive(false);
        });
    }

    private void HideAllSubPanels()
    {
        categoryPanel.SetActive(false);
        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);
    }


    private void TransitionPanels(GameObject fromPanel, GameObject toPanel)
    {
        var fromRT = fromPanel.GetComponent<RectTransform>();
        var toRT = toPanel.GetComponent<RectTransform>();

        toPanel.SetActive(true);
        toRT.anchoredPosition = new Vector2(1920, 0);

        var seq = DOTween.Sequence();

        seq.Join(fromRT
            .DOAnchorPosX(-1920, 0.4f)
            .SetEase(Ease.InOutCubic));

        seq.Join(toRT
            .DOAnchorPosX(0, 0.4f)
            .SetEase(Ease.InOutCubic));

        seq.OnComplete(() =>
        {
            fromPanel.SetActive(false);
            fromRT.anchoredPosition = Vector2.right * 1920;
        });

        seq.Play();
    }

    private void ResetCategoryButtonScales()
    {
        soundBtn.transform.DOScale(initialScales[soundBtn], 0.2f).SetEase(Ease.InOutQuad);
        resolutionBtn.transform.DOScale(initialScales[resolutionBtn], 0.2f).SetEase(Ease.InOutQuad);
        graphicsBtn.transform.DOScale(initialScales[graphicsBtn], 0.2f).SetEase(Ease.InOutQuad);
    }

    private void AddHoverAnimation(Button btn)
    {
        var initScale = initialScales[btn];
        var trigger = btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => btn.transform
            .DOScale(initScale * 1.1f, 0.15f)
            .SetEase(Ease.InOutQuad));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => btn.transform
            .DOScale(initScale, 0.15f));
        trigger.triggers.Add(exit);
    }
    private float SliderValueToDecibel(float value)
    {
        if (value <= 0f) return -80f;
        return Mathf.Log10(value) * 20f;
    }
    private void ApplyAllSettings()
    {
        bgmSlider.value = data.bgmVolume;
        sfxSlider.value = data.sfxVolume;
        bgmValueText.text = data.bgmVolume.ToString("F2");
        sfxValueText.text = data.sfxVolume.ToString("F2");
        resolutionDropdown.value = data.resolutionIndex;
        postFxToggle.isOn = data.postProcessing;

        audioMixer.SetFloat("BGM", SliderValueToDecibel(data.bgmVolume));
        audioMixer.SetFloat("SFX", SliderValueToDecibel(data.sfxVolume));
        ApplyPostFX(data.postProcessing);
    }

    private void PopulateResolutions()
    {
        availableResolutions = Screen.resolutions;
        var options = new List<string>();
        foreach (var r in availableResolutions)
        {
            int hz = Mathf.RoundToInt((float)r.refreshRateRatio.numerator / r.refreshRateRatio.denominator);
            options.Add($"{r.width}×{r.height}");
        }
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, options.Count - 1);
        resolutionDropdown.RefreshShownValue();
        ApplyResolution(resolutionDropdown.value);
    }

    private void OnBgmChanged(float value) { data.bgmVolume = value; bgmValueText.text = value.ToString("F2"); audioMixer.SetFloat("BGM", SliderValueToDecibel(value)); SaveSettings(); }
    private void OnSfxChanged(float value) { data.sfxVolume = value; sfxValueText.text = value.ToString("F2"); audioMixer.SetFloat("SFX", SliderValueToDecibel(value)); SaveSettings(); }
    private void OnResolutionChanged(int idx) { data.resolutionIndex = idx; ApplyResolution(idx); SaveSettings(); }
    private void OnPostFxChanged(bool en) { data.postProcessing = en; ApplyPostFX(en); SaveSettings(); }

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
        catch
        {
            data = new SettingsData();
        }
    }

    private static byte[] EncryptAes(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create(); aes.Key = key; aes.IV = iv;
        using var enc = aes.CreateEncryptor(); return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static byte[] DecryptAes(byte[] cipher, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create(); aes.Key = key; aes.IV = iv;
        using var dec = aes.CreateDecryptor(); return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
