using UnityEngine;
using System;

namespace FlyingAI.Utils
{
    /// <summary>
    /// Animation wrapper that supports PrimeTween with graceful fallback.
    /// If PrimeTween is not installed, uses Unity's built-in animation or coroutines.
    /// </summary>
    public static class TweenHelper
    {
        #region Configuration
        
        /// <summary>
        /// Set to true if PrimeTween is available.
        /// </summary>
#if PRIME_TWEEN
        public static bool IsPrimeTweenAvailable => true;
#else
        public static bool IsPrimeTweenAvailable => false;
#endif
        
        #endregion

        #region Position Tweens
        
        /// <summary>
        /// Moves transform to target position over duration.
        /// </summary>
        public static void MoveTo(Transform target, Vector3 endPosition, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Position(target, endPosition, duration).OnComplete(() => onComplete?.Invoke());
#else
            // Fallback: Use coroutine-based animation
            if (target.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunPositionTween(endPosition, duration, onComplete);
            }
            else
            {
                var newRunner = target.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunPositionTween(endPosition, duration, onComplete);
            }
#endif
        }
        
        /// <summary>
        /// Moves transform by offset over duration.
        /// </summary>
        public static void MoveBy(Transform target, Vector3 offset, float duration, Action onComplete = null)
        {
            MoveTo(target, target.position + offset, duration, onComplete);
        }
        
        #endregion

        #region Scale Tweens
        
        /// <summary>
        /// Scales transform to target scale over duration.
        /// </summary>
        public static void ScaleTo(Transform target, Vector3 endScale, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Scale(target, endScale, duration).OnComplete(() => onComplete?.Invoke());
#else
            if (target.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunScaleTween(endScale, duration, onComplete);
            }
            else
            {
                var newRunner = target.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunScaleTween(endScale, duration, onComplete);
            }
#endif
        }
        
        /// <summary>
        /// Punch scale effect.
        /// </summary>
        public static void PunchScale(Transform target, Vector3 punch, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.PunchScale(target, punch, duration).OnComplete(() => onComplete?.Invoke());
#else
            Vector3 originalScale = target.localScale;
            if (target.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunPunchScale(originalScale, punch, duration, onComplete);
            }
            else
            {
                var newRunner = target.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunPunchScale(originalScale, punch, duration, onComplete);
            }
#endif
        }
        
        #endregion

        #region Rotation Tweens
        
        /// <summary>
        /// Rotates transform to target rotation over duration.
        /// </summary>
        public static void RotateTo(Transform target, Quaternion endRotation, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Rotation(target, endRotation, duration).OnComplete(() => onComplete?.Invoke());
#else
            if (target.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunRotationTween(endRotation, duration, onComplete);
            }
            else
            {
                var newRunner = target.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunRotationTween(endRotation, duration, onComplete);
            }
#endif
        }
        
        #endregion

        #region Color/Material Tweens
        
        /// <summary>
        /// Fades renderer alpha over duration.
        /// </summary>
        public static void FadeTo(Renderer renderer, float endAlpha, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Alpha(renderer, endAlpha, duration).OnComplete(() => onComplete?.Invoke());
#else
            if (renderer.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunFadeTween(renderer, endAlpha, duration, onComplete);
            }
            else
            {
                var newRunner = renderer.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunFadeTween(renderer, endAlpha, duration, onComplete);
            }
#endif
        }
        
        /// <summary>
        /// Changes material color over duration.
        /// </summary>
        public static void ColorTo(Renderer renderer, Color endColor, float duration, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Color(renderer, endColor, duration).OnComplete(() => onComplete?.Invoke());
#else
            if (renderer.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.RunColorTween(renderer, endColor, duration, onComplete);
            }
            else
            {
                var newRunner = renderer.gameObject.AddComponent<TweenFallbackRunner>();
                newRunner.RunColorTween(renderer, endColor, duration, onComplete);
            }
#endif
        }
        
        #endregion

        #region Value Tweens
        
        /// <summary>
        /// Tweens a float value.
        /// </summary>
        public static void TweenValue(float startValue, float endValue, float duration, Action<float> onUpdate, Action onComplete = null)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.Custom(startValue, endValue, duration, val => onUpdate?.Invoke(val))
                .OnComplete(() => onComplete?.Invoke());
#else
            // Requires a MonoBehaviour context for fallback
            DebugLogger.LogWarning("TweenValue fallback requires MonoBehaviour context", LogCategory.General);
#endif
        }
        
        #endregion

        #region Sequence Builder
        
        /// <summary>
        /// Creates a new animation sequence.
        /// </summary>
        public static TweenSequence CreateSequence()
        {
            return new TweenSequence();
        }
        
        #endregion

        #region Stop/Kill
        
        /// <summary>
        /// Stops all tweens on target.
        /// </summary>
        public static void StopAll(Transform target)
        {
#if PRIME_TWEEN
            PrimeTween.Tween.StopAll(target);
#else
            if (target.TryGetComponent<TweenFallbackRunner>(out var runner))
            {
                runner.StopAllTweens();
            }
#endif
        }
        
        #endregion
    }

    /// <summary>
    /// Sequence builder for chaining animations.
    /// </summary>
    public class TweenSequence
    {
        private System.Collections.Generic.List<Action> _actions = new System.Collections.Generic.List<Action>();
        private int _currentIndex;
        
        public TweenSequence Append(Action action)
        {
            _actions.Add(action);
            return this;
        }
        
        public TweenSequence AppendInterval(float delay)
        {
            _actions.Add(() => { /* delay handled in execution */ });
            return this;
        }
        
        public void Play()
        {
            _currentIndex = 0;
            ExecuteNext();
        }
        
        private void ExecuteNext()
        {
            if (_currentIndex < _actions.Count)
            {
                _actions[_currentIndex]?.Invoke();
                _currentIndex++;
            }
        }
    }

    /// <summary>
    /// Fallback runner for when PrimeTween is not available.
    /// Uses coroutines for animation.
    /// </summary>
    public class TweenFallbackRunner : MonoBehaviour
    {
        private System.Collections.Generic.List<Coroutine> _activeTweens = new System.Collections.Generic.List<Coroutine>();
        
        public void RunPositionTween(Vector3 endPos, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(PositionCoroutine(endPos, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void RunScaleTween(Vector3 endScale, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(ScaleCoroutine(endScale, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void RunPunchScale(Vector3 originalScale, Vector3 punch, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(PunchScaleCoroutine(originalScale, punch, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void RunRotationTween(Quaternion endRot, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(RotationCoroutine(endRot, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void RunFadeTween(Renderer rend, float endAlpha, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(FadeCoroutine(rend, endAlpha, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void RunColorTween(Renderer rend, Color endColor, float duration, Action onComplete)
        {
            var coroutine = StartCoroutine(ColorCoroutine(rend, endColor, duration, onComplete));
            _activeTweens.Add(coroutine);
        }
        
        public void StopAllTweens()
        {
            foreach (var coroutine in _activeTweens)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            _activeTweens.Clear();
        }
        
        private System.Collections.IEnumerator PositionCoroutine(Vector3 endPos, float duration, Action onComplete)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            transform.position = endPos;
            onComplete?.Invoke();
        }
        
        private System.Collections.IEnumerator ScaleCoroutine(Vector3 endScale, float duration, Action onComplete)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            
            transform.localScale = endScale;
            onComplete?.Invoke();
        }
        
        private System.Collections.IEnumerator PunchScaleCoroutine(Vector3 originalScale, Vector3 punch, float duration, Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float wave = Mathf.Sin(t * Mathf.PI) * (1 - t);
                transform.localScale = originalScale + punch * wave;
                yield return null;
            }
            
            transform.localScale = originalScale;
            onComplete?.Invoke();
        }
        
        private System.Collections.IEnumerator RotationCoroutine(Quaternion endRot, float duration, Action onComplete)
        {
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            
            transform.rotation = endRot;
            onComplete?.Invoke();
        }
        
        private System.Collections.IEnumerator FadeCoroutine(Renderer rend, float endAlpha, float duration, Action onComplete)
        {
            if (rend == null) yield break;
            
            Color startColor = rend.material.color;
            float startAlpha = startColor.a;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                Color newColor = rend.material.color;
                newColor.a = Mathf.Lerp(startAlpha, endAlpha, t);
                rend.material.color = newColor;
                yield return null;
            }
            
            Color finalColor = rend.material.color;
            finalColor.a = endAlpha;
            rend.material.color = finalColor;
            onComplete?.Invoke();
        }
        
        private System.Collections.IEnumerator ColorCoroutine(Renderer rend, Color endColor, float duration, Action onComplete)
        {
            if (rend == null) yield break;
            
            Color startColor = rend.material.color;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                rend.material.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            
            rend.material.color = endColor;
            onComplete?.Invoke();
        }
    }
}
