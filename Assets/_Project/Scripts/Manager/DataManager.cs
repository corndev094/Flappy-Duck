using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DataManager : Singleton<DataManager> {
    [Header("ScriptableObjects Data")]
    [field: SerializeField] public DuckList DuckList { get; private set; }
    [field: SerializeField] public LevelListSO LevelList { get; private set;}
    [field: SerializeField] public LevelSO EmptyLevel { get; private set;}
    [field: SerializeField] public LevelSO OnlineLevel { get; private set;}

    [Header("Auto Save")]
    [SerializeField] private bool autoSave;
    [SerializeField] private float autoSaveAfter = 15f;

    private CancellationTokenSource autoSaveCts;

    public LevelSO FindLevelDataById(int id)
    {
        if (LevelList == null) return null;
        return LevelList.List.Find(l => l.ID == id);
    }

    #region AutoSave
    void Start()
    {
        InitData();
        if (autoSave) AutoSave().Forget();
    }

    private void InitData()
    {
        if (PlayerPrefs.GetInt(ConstantString.INIT_DATA) == 0)
        {
            PlayerPrefs.SetInt(ConstantString.INIT_DATA, 1);
            PlayerPrefs.Save();
            SaveUnlockedSkin(DuckSkinID.Normal, true);
        }
    }

    private async UniTask AutoSave()
    {
        autoSaveCts?.Cancel();
        autoSaveCts?.Dispose();
        autoSaveCts = new CancellationTokenSource();
        var cts = autoSaveCts;
        float time = 0;
        while (!cts.IsCancellationRequested)
        {
            time += Time.deltaTime;
            if (time >= autoSaveAfter)
            {
                time = 0;
                PlayerPrefs.Save();
            }
            await UniTask.Yield();
        }
    }

    private void StopAutoSave()
    {
        autoSaveCts?.Cancel();
        autoSaveCts?.Dispose();
        autoSaveCts = null;
    }
    #endregion

    #region Save/Load
    public static void Save(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    public static void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
    }
    #endregion

    #region Currency
    public void SaveCurrency(string key, int value)
    {
        value = Mathf.Clamp(value, 0, 999999);
        PlayerPrefs.SetInt(string.Format(ConstantString.CURRENCY, key), value);
        PlayerPrefs.Save();
    }

    public int GetCurrency(string key)
    {
        return PlayerPrefs.GetInt(string.Format(ConstantString.CURRENCY, key), 0);
    }
    #endregion

    #region Skin
    public void SaveUnlockedSkin(DuckSkinID skinId, bool unlocked)
    {
        var id = (int)skinId;
        PlayerPrefs.SetInt(string.Format(ConstantString.SKIN, id.ToString()), unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSkinUnlocked(DuckSkinID skinId)
    {
        int id = (int)skinId;
        var key = string.Format(ConstantString.SKIN, id);
        var isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;
        return isUnlocked;
    }

    public void SaveLastSelectedDuck(int duckId)
    {
        PlayerPrefs.SetInt(ConstantString.LAST_SELECT_DUCK, duckId);
        PlayerPrefs.Save();
    }

    public int GetLastSelectedDuck()
    {
        return PlayerPrefs.GetInt(ConstantString.LAST_SELECT_DUCK, 0);
    }
    
    #endregion

    #region Level
    public void SaveHighestLevel(int levelId)
    {
        if (levelId > GetHighestLevel())
        {
            PlayerPrefs.SetInt(ConstantString.HIGHEST_LEVEL, levelId);
            PlayerPrefs.Save();
        }
    }

    public int GetHighestLevel()
    {
        return PlayerPrefs.GetInt(ConstantString.HIGHEST_LEVEL, 1); // Default level 1 is unlocked
    }

    public bool IsLevelUnlocked(int levelId)
    {
        return levelId <= GetHighestLevel();
    }

    public void SaveLastDuckLevelPos(int levelId)
    {
        PlayerPrefs.SetInt(ConstantString.LAST_DUCK_LEVEL_POS, levelId);
        PlayerPrefs.Save();
    }

    public int GetLastDuckLevelPos()
    {
        return PlayerPrefs.GetInt(ConstantString.LAST_DUCK_LEVEL_POS, 1);
    }

    public void SaveCoinCollected(int coinPointId, bool collected)
    {
        PlayerPrefs.SetInt(string.Format("game.coin_collected_{0}", coinPointId), collected ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsCoinCollected(int coinPointId)
    {
        return PlayerPrefs.GetInt(string.Format("game.coin_collected_{0}", coinPointId), 0) == 1;
    }
    #endregion

    #region Graphic Settings

    public void SaveContrast(float contrast){
        PlayerPrefs.SetFloat(ConstantString.CONTRAST, contrast);
        PlayerPrefs.Save();
    }

    public void SaveBrightness(float brightness){
        PlayerPrefs.SetFloat(ConstantString.BRIGHTNESS, brightness);
        PlayerPrefs.Save();
    }

    public void SaveScreenMode(string screenMode){
        PlayerPrefs.SetString(ConstantString.SCREEN_MODE, screenMode);
        PlayerPrefs.Save();
    }

    public void SaveResolution(string resolution){
        PlayerPrefs.SetString(ConstantString.RESOLUTION, resolution);
        PlayerPrefs.Save();
    }

    public void SaveFps(int fps){
        PlayerPrefs.SetInt(ConstantString.FPS, fps);
        PlayerPrefs.Save();
    }

    public void SaveVSync(int vsync){
        PlayerPrefs.SetInt(ConstantString.VSYNC, vsync);
        PlayerPrefs.Save();
    }

    public float GetContrast(){
        return PlayerPrefs.GetFloat(ConstantString.CONTRAST, 0f);
    }

    public float GetBrightness(){
        return PlayerPrefs.GetFloat(ConstantString.BRIGHTNESS, 0f);
    }

    public string GetScreenMode(){
        return PlayerPrefs.GetString(ConstantString.SCREEN_MODE, "Exclusive");
    }

    public string GetResolution(){
        return PlayerPrefs.GetString(ConstantString.RESOLUTION, "1920x1080");
    }

    public int GetFps(){
        return PlayerPrefs.GetInt(ConstantString.FPS, 60);
    }

    public bool GetVSync(){
        return PlayerPrefs.GetInt(ConstantString.VSYNC, 0) == 1;
    }


    #endregion

    #region Sound Settings

    public void SaveMasterVolume(float volume){
        PlayerPrefs.SetFloat(ConstantString.SOUND_MASTER, volume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume(){
        return PlayerPrefs.GetFloat(ConstantString.SOUND_MASTER, 1f);
    }

    public void SaveMusicVolume(float volume){
        PlayerPrefs.SetFloat(ConstantString.SOUND_MUSIC, volume);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume(){
        return PlayerPrefs.GetFloat(ConstantString.SOUND_MUSIC, 1f);
    }

    public void SaveSfxVolume(float volume){
        PlayerPrefs.SetFloat(ConstantString.SOUND_SFX, volume);
        PlayerPrefs.Save();
    }

    public float GetSfxVolume(){
        return PlayerPrefs.GetFloat(ConstantString.SOUND_SFX, 1f);
    }

    #endregion

    #region Localization

    public void SaveLanguage(string language){
        PlayerPrefs.SetString(ConstantString.LANGUAGE, language);
    }

    public string GetLanguage()
    {
        return PlayerPrefs.GetString(ConstantString.LANGUAGE, "en");
    }

    #endregion

    #region Utility
    [ContextMenu("Create New Data")]
    public void CreateNewData()
    {
        DeleteAllData();
        SaveUnlockedSkin(DuckSkinID.Normal, true);
        SaveCurrency(ConstantString.COIN, 1);
    }

    public bool CanBuySkin(int price)
    {
        return GetCurrency(ConstantString.COIN) >= price;
    }
    #endregion

    #region Unity Events
    void OnDestroy()
    {
        StopAutoSave();
    }

    void OnApplicationQuit()
    {
        StopAutoSave();
        PlayerPrefs.Save();
    }
    #endregion
}

public enum DuckSkinID
{
    Normal = 0,
    Rambo = 1
}
