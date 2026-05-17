using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource audioSourcePref;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource bgMusic;

    [Header("Background Music")] 
    [SerializeField] private AudioClip[] bgTracks;
    [SerializeField] private bool randomBgTrackOnStart;
    [SerializeField] private bool repeatAll;

    private ObjectPool<AudioSource> audioSourcePool;
    private const string MASTER_PARAM = "master";
    private const string MUSIC_PARAM = "music";
    private const string SFX_PARAM = "sfx";

    private void Start()
    {
        SetMasterVolume(DataManager.Instance.GetMasterVolume());
        SetMusicVolume(DataManager.Instance.GetMusicVolume());
        SetSfxVolume(DataManager.Instance.GetSfxVolume());
        audioSourcePool =
            new ObjectPool<AudioSource>(CreateAS, GetAS, ReleaseAS, DestroyAS, false, 10, 20);
        if (bgMusic != null && bgTracks.Length > 0)
        {
            if (randomBgTrackOnStart) bgMusic.clip = bgTracks[UnityEngine.Random.Range(0, bgTracks.Length)];
            else bgMusic.clip = bgTracks[0];
        }
        bgMusic?.Play();
        if (repeatAll) RepeatBgMusic();
    }

#region AudioSource Pool
        private void DestroyAS(AudioSource obj)
        {
            if (obj == null) return;
            Destroy(obj.gameObject);
        }

        private void ReleaseAS(AudioSource obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
        }

        private void GetAS(AudioSource obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(true);
        }

        private AudioSource CreateAS()
        {
            AudioSource audioSource = Instantiate(audioSourcePref);
            audioSource.gameObject.SetActive(false);
            
            return audioSource;
        }
#endregion

#region Volume Controls

    public void EnableMasterVolume(bool enable)
    {
        audioMixer.SetFloat(MASTER_PARAM, enable ? 0 : -80);
        DataManager.Instance.SaveMasterVolume(enable ? 1f : 0);
    }

    public void EnableMusicVolume(bool enable)
    {
        audioMixer.SetFloat(MUSIC_PARAM, enable ? 0 : -80);
        DataManager.Instance.SaveMusicVolume(enable ? 1f : 0);
    }

    public void EnableSfxVolume(bool enable)
    {
        audioMixer.SetFloat(SFX_PARAM, enable ? 0 : -80);
        DataManager.Instance.SaveSfxVolume(enable ? 1f : 0);
    }

    public bool IsMasterVolumeEnabled()
    {
        return DataManager.Instance.GetMasterVolume() > 0.0001f;
    }

    public bool IsMusicVolumeEnabled()
    {
        return DataManager.Instance.GetMusicVolume() > 0.0001f;
    }

    public bool IsSfxVolumeEnabled()
    {
        return DataManager.Instance.GetSfxVolume() > 0.0001f;
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1);
        audioMixer.SetFloat(MASTER_PARAM, Mathf.Log10(value)*20);
        DataManager.Instance.SaveMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1);
        audioMixer.SetFloat(MUSIC_PARAM, Mathf.Log10(value)*20);
        DataManager.Instance.SaveMusicVolume(value);
    }
    
    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1);
        audioMixer.SetFloat(SFX_PARAM, Mathf.Log10(value)*20);
        DataManager.Instance.SaveSfxVolume(value);
    }

#endregion

#region Background Music Controls
        public void PlayBgMusic(AudioClip clip = null, float volume = 1)
        {
            if (bgMusic == null) return;
            if (clip == null && bgTracks.Length > 0) bgMusic.clip = bgTracks[UnityEngine.Random.Range(0, bgTracks.Length)];
            else bgMusic.clip = clip;
            bgMusic.volume = volume;
            bgMusic.mute = false;
            bgMusic.Play();
        }

        public void MuteBgMusic()
        {
            if (bgMusic == null) return;
            bgMusic.mute = true;
        }

        public void UnmuteBgMusic()
        {
            if (bgMusic == null) return;
            bgMusic.mute = false;
        }

        public void StopBgMusic()
        {
            if (bgMusic == null) return;
            bgMusic?.Stop();
        }

        public void ResumeBgMusic()
        {
            if (bgMusic == null) return;
            bgMusic?.Play();
        }

    private bool IsBgMusicFinished()
    {
        if (bgMusic == null || bgMusic.clip == null) return false;
        return !bgMusic.isPlaying && Mathf.Approximately(bgMusic.time, bgMusic.clip.length);
    }

    private async void RepeatBgMusic()
    {
        while (this != null)
        {
            if (IsBgMusicFinished())
            {
                if (bgTracks.Length == 0) return;

                int currentIndex = Array.IndexOf(bgTracks, bgMusic.clip);
                int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % bgTracks.Length;
                bgMusic.clip = bgTracks[nextIndex];
                bgMusic.Play();
            }
            await UniTask.Yield();
        }
    }
#endregion  

#region SFX Controls
        public void PlaySFX(AudioClip audioClip, float volume = 0.7f, bool randomPitch = false)
        {
            if (audioClip == null || audioSourcePool == null) return;
            AudioSource audioSource = audioSourcePool.Get();
            // audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups(MASTER_PARAM)[2];
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            // if (randomPitch) audioSource.pitch = Random.Range(-3, 3);
            // else audioSource.pitch = 1;
            audioSource.Play();
            ReleaseAudioSourceToPoolAfter(audioSource, audioSource.clip.length).Forget();
        }

        private async UniTask ReleaseAudioSourceToPoolAfter(AudioSource audioSource, float clipLength)
        {
            float pitch = audioSource == null ? 1f : Mathf.Max(Mathf.Abs(audioSource.pitch), 0.01f);
            await UniTask.Delay(TimeSpan.FromSeconds(clipLength / pitch));
            if (audioSource == null || audioSourcePool == null) return;
            audioSourcePool.Release(audioSource);
        }
#endregion
}
