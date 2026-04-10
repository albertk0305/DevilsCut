using System.Collections.Generic;
using UnityEngine;
using System;

//글로벌 매니저에 붙어서 언어 패치해주는 코드
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    public TextAsset localizationCSV;

    public enum Language { Korean, English }
    public Language currentLanguage = Language.Korean;
    public Action OnLanguageChanged;

    private Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();

    private void Awake()
    {
        // 싱글톤 및 DontDestroyOnLoad 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings(); // 게임 시작 시 저장된 설정 불러오기
            LoadCSV();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LoadCSV()
    {
        if (localizationCSV == null) return;
        string[] rows = localizationCSV.text.Split('\n');
        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;
            string row = rows[i].TrimEnd('\r', '\n');
            string[] columns = row.Split(',');
            if (columns.Length >= 3)
            {
                dictionary[columns[0]] = new string[] { columns[1], columns[2] };
            }
        }
        DevLog.Log("다국어 데이터 로드 및 적용 완료!");
    }

    // --- 저장 및 불러오기 핵심 로직 ---
    private void LoadSettings()
    {
        // "SelectedLanguage"라는 키로 저장된 값을 가져옴 (없으면 1:영어)
        int savedLang = PlayerPrefs.GetInt("SelectedLanguage", 1);
        currentLanguage = (Language)savedLang;
    }

    public void SetKorean()
    {
        currentLanguage = Language.Korean;
        PlayerPrefs.SetInt("SelectedLanguage", 0); // 0을 저장
        PlayerPrefs.Save(); // 물리적 저장 장치에 기록
        OnLanguageChanged?.Invoke();
    }

    public void SetEnglish()
    {
        currentLanguage = Language.English;
        PlayerPrefs.SetInt("SelectedLanguage", 1); // 1을 저장
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public string GetText(string key)
    {
        if (dictionary.TryGetValue(key, out string[] texts))
        {
            return currentLanguage == Language.Korean ? texts[0] : texts[1];
        }
        return key;
    }
}