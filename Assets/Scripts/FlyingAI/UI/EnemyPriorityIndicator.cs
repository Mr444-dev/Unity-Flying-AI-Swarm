using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;

namespace FlyingAI.UI
{
    /// <summary>
    /// Visual indicator for enemy priority/importance.
    /// Shows icons and effects based on priority level.
    /// Uses TweenHelper for plugin-safe animations.
    /// </summary>
    public class EnemyPriorityIndicator : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("References")]
        [SerializeField] private FlyingEnemyBase _enemy;
        [SerializeField] private Transform _indicatorRoot;
        [SerializeField] private SpriteRenderer _priorityIcon;
        [SerializeField] private SpriteRenderer _eliteIcon;
        
        [Header("Settings")]
        [SerializeField] private float _bobAmplitude = 0.2f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _scaleMultiplier = 1.5f;
        
        [Header("Colors")]
        [SerializeField] private Color _lowPriorityColor = Color.green;
        [SerializeField] private Color _normalPriorityColor = Color.yellow;
        [SerializeField] private Color _highPriorityColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _criticalPriorityColor = Color.red;
        
        #endregion

        #region Private Fields
        
        private Camera _mainCamera;
        private int _lastPriority = -1;
        private Coroutine _pulseCoroutine;
        private bool _isPulsing;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            _mainCamera = Camera.main;
            
            if (_enemy == null)
            {
                _enemy = GetComponentInParent<FlyingEnemyBase>();
            }
            
            if (_enemy != null)
            {
                _enemy.OnPriorityChanged += OnPriorityChanged;
                UpdateVisuals(_enemy.CurrentPriority);
            }
            
            if (_eliteIcon != null && _enemy != null)
            {
                _eliteIcon.gameObject.SetActive(_enemy.IsElite);
            }
        }
        
        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.OnPriorityChanged -= OnPriorityChanged;
            }
            StopPulsing();
        }
        
        private void LateUpdate()
        {
            if (_indicatorRoot != null && _mainCamera != null)
            {
                // Billboard effect
                _indicatorRoot.LookAt(_mainCamera.transform);
                
                // Bobbing
                float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
                _indicatorRoot.localPosition = new Vector3(0, 2f + bob, 0);
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void OnPriorityChanged(FlyingEnemyBase enemy, int oldPri, int newPri)
        {
            if (newPri != _lastPriority)
            {
                UpdateVisuals(newPri);
                PlayPriorityChangeAnimation(oldPri < newPri);
            }
        }
        
        #endregion

        #region Visual Updates
        
        private void UpdateVisuals(int priority)
        {
            _lastPriority = priority;
            
            if (_priorityIcon == null) return;
            
            Color color = GetPriorityColor(priority);
            _priorityIcon.color = color;
            
            // Scale based on priority
            float scale = Mathf.Lerp(0.5f, _scaleMultiplier, priority / 10f);
            _indicatorRoot.localScale = Vector3.one * scale;
            
            // Start pulse for high priority
            if (priority >= 7)
            {
                StartPulsing();
            }
            else
            {
                StopPulsing();
            }
        }
        
        private Color GetPriorityColor(int priority)
        {
            if (priority >= 9) return _criticalPriorityColor;
            if (priority >= 7) return _highPriorityColor;
            if (priority >= 4) return _normalPriorityColor;
            return _lowPriorityColor;
        }
        
        private void PlayPriorityChangeAnimation(bool increased)
        {
            if (_indicatorRoot == null) return;
            
            Vector3 punch = increased ? Vector3.one * 0.3f : Vector3.one * -0.2f;
            TweenHelper.PunchScale(_indicatorRoot, punch, 0.3f);
            
            if (_priorityIcon != null)
            {
                Color flashColor = increased ? Color.white : Color.gray;
                Color targetColor = GetPriorityColor(_lastPriority);
                
                // Flash then return to normal
                StartCoroutine(FlashColorCoroutine(flashColor, targetColor));
            }
        }
        
        private System.Collections.IEnumerator FlashColorCoroutine(Color flashColor, Color targetColor)
        {
            if (_priorityIcon == null) yield break;
            
            _priorityIcon.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            
            // Lerp back to target
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _priorityIcon.color = Color.Lerp(flashColor, targetColor, elapsed / duration);
                yield return null;
            }
            _priorityIcon.color = targetColor;
        }
        
        private void StartPulsing()
        {
            if (_isPulsing) return;
            _isPulsing = true;
            _pulseCoroutine = StartCoroutine(PulseCoroutine());
        }
        
        private void StopPulsing()
        {
            _isPulsing = false;
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            
            if (_indicatorRoot != null)
            {
                _indicatorRoot.localScale = Vector3.one * Mathf.Lerp(0.5f, _scaleMultiplier, _lastPriority / 10f);
            }
        }
        
        private System.Collections.IEnumerator PulseCoroutine()
        {
            if (_indicatorRoot == null) yield break;
            
            float baseScale = Mathf.Lerp(0.5f, _scaleMultiplier, _lastPriority / 10f);
            float maxScale = baseScale * 1.2f;
            float pulseSpeed = 2f;
            
            while (_isPulsing)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
                float scale = Mathf.Lerp(baseScale, maxScale, t);
                _indicatorRoot.localScale = Vector3.one * scale;
                yield return null;
            }
        }
        
        #endregion
    }
}
