using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using TMPro;

[Serializable]
public class SettingsData
{
    [LabelText("BGM ボリューム"), Range(0f, 1f)]
    public float bgmVolume = 1f;

    [LabelText("SFX ボリューム"), Range(0f, 1f)]
    public float sfxVolume = 1f;

    [LabelText("解像度インデックス")]
    public int resolutionIndex = 0;

    [LabelText("ポストプロセッシング")]
    public bool postProcessing = true;
}

public class SettingsMenuController : MonoBehaviour
{
    /* ───────────────── ルート/カテゴリ ───────────────── */
    [FoldoutGroup("UI/Root"), Required] public Button settingsBtn;      // 初期に唯一表示
    [FoldoutGroup("UI/Root"), Required] public GameObject settingsPanel; // 設定ウィンドウ（親）
    [FoldoutGroup("UI/Root"), Required] public GameObject categoryPanel; // 「サウンド」「画面」「グラフィック」ボタンを格納
    [FoldoutGroup("UI/Root"), Required] public Button closeBtn;         // 戻る（＝閉じる）

    [FoldoutGroup("UI/Category"), Required] public Button soundBtn;
    [FoldoutGroup("UI/Category"), Required] public Button resolutionBtn;
    [FoldoutGroup("UI/Category"), Required] public Button graphicsBtn;

    /* ───────────────── サウンド ───────────────── */
    [FoldoutGroup("UI/Sound"), Required] public GameObject soundPanel;
    [FoldoutGroup("UI/Sound"), Required] public Slider bgmSlider;
    [FoldoutGroup("UI/Sound"), Required] public Slider sfxSlider;
    [FoldoutGroup("UI/Sound"), Required] public AudioMixer audioMixer;
    [FoldoutGroup("UI/Sound"), Required] public Button backFromSoundBtn;

    /* ───────────────── 解像度 ───────────────── */
    [FoldoutGroup("UI/Resolution"), Required] public GameObject resolutionPanel;
    [FoldoutGroup("UI/Resolution"), Required] public TMP_Dropdown resolutionDropdown;
    [FoldoutGroup("UI/Resolution"), Required] public Button backFromResBtn;
    private Resolution[] availableResolutions;

    /* ───────────────── グラフィック ───────────────── */
    [FoldoutGroup("UI/Graphics"), Required] public GameObject graphicsPanel;
    [FoldoutGroup("UI/Graphics"), Required] public Toggle postFxToggle;
    [FoldoutGroup("UI/Graphics"), Required] public Button backFromGfxBtn;

    /* ───────────────── データ ───────────────── */
    [FoldoutGroup("Data"), HideLabel, HideReferenceObjectPicker]
    private SettingsData data = new SettingsData();

    private string settingsFilePath => Path.Combine(Application.persistentDataPath, "settings.dat");
    private readonly byte[] aesKey = Encoding.UTF8.GetBytes("0123456789ABCDEF");
    private readonly byte[] aesIV = Encoding.UTF8.GetBytes("FEDCBA9876543210");

    /* ───────────────────────────────────────────── */
    private void Awake()
    {
        // 最初は設定ボタンのみ
        settingsBtn.gameObject.SetActive(true);

        settingsPanel.SetActive(false);
        categoryPanel.SetActive(false);
        closeBtn.gameObject.SetActive(false);

        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);

        LoadSettings();
        ApplyAllSettings();
    }

    private void Start()
    {
        /* ───── 設定ボタン ───── */
        settingsBtn.onClick.AddListener(() =>
        {
            settingsBtn.gameObject.SetActive(false);
            settingsPanel.SetActive(true);
            categoryPanel.SetActive(true);
            closeBtn.gameObject.SetActive(true);
        });

        /* ───── 戻る(閉じる) ───── */
        closeBtn.onClick.AddListener(CloseSettingsWindow);

        /* ───── カテゴリ選択 ───── */
        soundBtn.onClick.AddListener(() => ShowPanel(soundPanel));
        resolutionBtn.onClick.AddListener(() => ShowPanel(resolutionPanel));
        graphicsBtn.onClick.AddListener(() => ShowPanel(graphicsPanel));

        /* ───── 各パネル→戻る ───── */
        backFromSoundBtn.onClick.AddListener(ReturnToCategories);
        backFromResBtn.onClick.AddListener(ReturnToCategories);
        backFromGfxBtn.onClick.AddListener(ReturnToCategories);

        /* ───── スライダー等 ───── */
        bgmSlider.onValueChanged.AddListener(v =>
        {
            data.bgmVolume = v;
            audioMixer.SetFloat("BGMVolume", Mathf.Lerp(-80f, 0f, v));
            SaveSettings();
        });
        sfxSlider.onValueChanged.AddListener(v =>
        {
            data.sfxVolume = v;
            audioMixer.SetFloat("SFXVolume", Mathf.Lerp(-80f, 0f, v));
            SaveSettings();
        });

        PopulateResolutions();
        resolutionDropdown.onValueChanged.AddListener(i =>
        {
            data.resolutionIndex = i;
            ApplyResolution(i);
            SaveSettings();
        });

        postFxToggle.onValueChanged.AddListener(on =>
        {
            data.postProcessing = on;
            ApplyPostFX(on);
            SaveSettings();
        });
    }

    private void OnDisable() => SaveSettings();

    /* ============================================================
       UI 切替ヘルパー
       ============================================================ */
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
        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);
        categoryPanel.SetActive(true);
    }

    private void CloseSettingsWindow()
    {
        settingsPanel.SetActive(false);
        categoryPanel.SetActive(false);
        closeBtn.gameObject.SetActive(false);

        soundPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        graphicsPanel.SetActive(false);

        settingsBtn.gameObject.SetActive(true);
    }

    /* ============================================================
       設定適用 / 解像度
       ============================================================ */
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
        var options = new System.Collections.Generic.List<string>();

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

    /* ============================================================
       AES + JSON 保存／読み込み
       ============================================================ */
    private void SaveSettings()
    {
        try
        {
            var json = JsonUtility.ToJson(data);
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = EncryptAes(plain, aesKey, aesIV);
            File.WriteAllBytes(settingsFilePath, cipher);
        }
        catch (Exception ex) { Debug.LogError($"設定保存失敗: {ex}"); }
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
            Debug.LogWarning($"設定読み込み失敗: {ex}");
            data = new SettingsData();
        }
    }

    private static byte[] EncryptAes(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key; aes.IV = iv;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static byte[] DecryptAes(byte[] cipher, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key; aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
