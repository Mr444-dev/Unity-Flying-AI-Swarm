using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Pooling;
using FlyingAI.Utils;

namespace FlyingAI.Audio
{
    /// <summary>
    /// Pooled audio source for efficient sound playback.
    /// </summary>
    public class PooledAudioSource
    {
        public AudioSource Source;
        public float EndTime;
        public bool IsActive => Time.time < EndTime;
    }

    /// <summary>
    /// Centralized audio manager for flying AI sound effects.
    /// Uses pooling for efficient playback of many simultaneous sounds.
    /// </summary>
    public class FlyingAIAudioManager : MonoBehaviour
    {
        #region Singleton
        
        private static FlyingAIAudioManager _instance;
        public static FlyingAIAudioManager Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Audio Pools")]
        [SerializeField] private int _poolSize = 20;
        [SerializeField] private float _maxDistance = 100f;
        [SerializeField] private float _spatialBlend = 1f;
        
        [Header("Sound Clips")]
        [SerializeField] private AudioClip[] _weaponFireSounds;
        [SerializeField] private AudioClip[] _explosionSounds;
        [SerializeField] private AudioClip[] _hitSounds;
        [SerializeField] private AudioClip[] _engineSounds;
        [SerializeField] private AudioClip[] _deathSounds;
        [SerializeField] private AudioClip[] _abilitySounds;
        [SerializeField] private AudioClip[] _formationSounds;
        
        [Header("Volume Settings")]
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _weaponVolume = 0.8f;
        [SerializeField] private float _explosionVolume = 1f;
        [SerializeField] private float _hitVolume = 0.7f;
        [SerializeField] private float _engineVolume = 0.3f;
        [SerializeField] private float _deathVolume = 0.9f;
        
        [Header("Pitch Variation")]
        [SerializeField] private float _pitchVariation = 0.1f;
        
        [Header("Culling")]
        [SerializeField] private int _maxSimultaneousSounds = 10;
        [SerializeField] private float _soundCooldown = 0.05f;
        
        #endregion

        #region Private Fields
        
        private List<PooledAudioSource> _audioPool = new List<PooledAudioSource>();
        private Dictionary<AudioClip, float> _lastPlayTime = new Dictionary<AudioClip, float>();
        private int _currentPlayCount;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializePool();
        }
        
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        
        private void LateUpdate()
        {
            // Count active sounds
            _currentPlayCount = 0;
            foreach (var pooled in _audioPool)
            {
                if (pooled.IsActive) _currentPlayCount++;
            }
        }
        
        #endregion

        #region Initialization
        
        private void InitializePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject go = new GameObject($"PooledAudio_{i}");
                go.transform.SetParent(transform);
                
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = _spatialBlend;
                source.maxDistance = _maxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
                
                _audioPool.Add(new PooledAudioSource
                {
                    Source = source,
                    EndTime = 0
                });
            }
            
            DebugLogger.Log($"Audio pool initialized with {_poolSize} sources", LogCategory.General);
        }
        
        #endregion

        #region Public API
        
        public void PlayWeaponFire(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_weaponFireSounds, position, _weaponVolume * volumeScale);
        }
        
        public void PlayExplosion(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_explosionSounds, position, _explosionVolume * volumeScale);
        }
        
        public void PlayHit(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_hitSounds, position, _hitVolume * volumeScale);
        }
        
        public void PlayDeath(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_deathSounds, position, _deathVolume * volumeScale);
        }
        
        public void PlayAbility(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_abilitySounds, position, volumeScale);
        }
        
        public void PlayFormation(Vector3 position, float volumeScale = 1f)
        {
            PlayRandomFromArray(_formationSounds, position, volumeScale);
        }
        
        /// <summary>
        /// Plays a specific clip at position.
        /// </summary>
        public void PlayClip(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            
            // Cooldown check
            if (_lastPlayTime.TryGetValue(clip, out float lastTime))
            {
                if (Time.time - lastTime < _soundCooldown) return;
            }
            
            // Simultaneous sounds limit
            if (_currentPlayCount >= _maxSimultaneousSounds) return;
            
            var pooled = GetAvailableSource();
            if (pooled == null) return;
            
            pooled.Source.transform.position = position;
            pooled.Source.clip = clip;
            pooled.Source.volume = volume * _masterVolume;
            pooled.Source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            pooled.Source.Play();
            pooled.EndTime = Time.time + clip.length;
            
            _lastPlayTime[clip] = Time.time;
        }
        
        /// <summary>
        /// Plays looping engine sound, returns the audio source for manual stop.
        /// </summary>
        public AudioSource PlayEngineLoop(Vector3 position)
        {
            if (_engineSounds == null || _engineSounds.Length == 0) return null;
            
            var pooled = GetAvailableSource();
            if (pooled == null) return null;
            
            AudioClip clip = _engineSounds[Random.Range(0, _engineSounds.Length)];
            
            pooled.Source.transform.position = position;
            pooled.Source.clip = clip;
            pooled.Source.volume = _engineVolume * _masterVolume;
            pooled.Source.loop = true;
            pooled.Source.Play();
            pooled.EndTime = float.MaxValue;
            
            return pooled.Source;
        }
        
        /// <summary>
        /// Stops a looping sound.
        /// </summary>
        public void StopLoop(AudioSource source)
        {
            if (source == null) return;
            
            source.Stop();
            source.loop = false;
            
            foreach (var pooled in _audioPool)
            {
                if (pooled.Source == source)
                {
                    pooled.EndTime = 0;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Sets master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }
        
        #endregion

        #region Private Methods
        
        private void PlayRandomFromArray(AudioClip[] clips, Vector3 position, float volume)
        {
            if (clips == null || clips.Length == 0) return;
            
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlayClip(clip, position, volume);
        }
        
        private PooledAudioSource GetAvailableSource()
        {
            foreach (var pooled in _audioPool)
            {
                if (!pooled.IsActive)
                {
                    return pooled;
                }
            }
            
            // All in use - find oldest
            PooledAudioSource oldest = _audioPool[0];
            foreach (var pooled in _audioPool)
            {
                if (pooled.EndTime < oldest.EndTime)
                {
                    oldest = pooled;
                }
            }
            
            oldest.Source.Stop();
            return oldest;
        }
        
        #endregion
    }
}
