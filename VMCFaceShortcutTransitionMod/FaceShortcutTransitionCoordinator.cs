using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VMC;

namespace VMCFaceShortcutTransitionMod
{
    internal sealed class FaceShortcutTransitionCoordinator
    {
        private readonly MonoBehaviour _host;
        private Coroutine _runningTransition;
        private int _transitionVersion;

        private readonly Dictionary<string, float> _lastAppliedValues =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private readonly FieldInfo _blendShapeKeyStringField =
            typeof(FaceController).GetField("BlendShapeKeyString", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly FieldInfo _currentShapeKeysField =
            typeof(FaceController).GetField("CurrentShapeKeys", BindingFlags.Instance | BindingFlags.NonPublic);

        public FaceShortcutTransitionCoordinator(MonoBehaviour host)
        {
            _host = host;
        }

        public void Stop()
        {
            _transitionVersion++;
            if (_runningTransition != null)
            {
                _host.StopCoroutine(_runningTransition);
                _runningTransition = null;
            }
        }

        public void StartTransition(
            FaceController faceController,
            List<string> faceNames,
            List<float> faceStrength,
            bool stopBlink,
            float transitionSec,
            int tickMs)
        {
            if (faceController == null || faceNames == null || faceStrength == null)
            {
                return;
            }

            if (faceNames.Count == 0 || faceNames.Count != faceStrength.Count)
            {
                return;
            }

            var normalizedNames = new List<string>(faceNames.Count);
            var normalizedTargets = new List<float>(faceStrength.Count);
            for (var i = 0; i < faceNames.Count; i++)
            {
                var name = faceNames[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var caseSensitiveName = faceController.GetCaseSensitiveKeyName(name);
                normalizedNames.Add(caseSensitiveName);
                normalizedTargets.Add(Mathf.Clamp01(faceStrength[i]));
            }

            if (normalizedNames.Count == 0)
            {
                return;
            }

            var startValues = CaptureStartValues(faceController, normalizedNames);

            _transitionVersion++;
            var thisTransitionVersion = _transitionVersion;

            if (_runningTransition != null)
            {
                _host.StopCoroutine(_runningTransition);
                _runningTransition = null;
            }

            _runningTransition = _host.StartCoroutine(RunTransition(
                thisTransitionVersion,
                faceController,
                normalizedNames,
                startValues,
                normalizedTargets,
                stopBlink,
                Mathf.Max(0f, transitionSec),
                Mathf.Max(1, tickMs)));
        }

        private IEnumerator RunTransition(
            int transitionVersion,
            FaceController faceController,
            List<string> names,
            List<float> startValues,
            List<float> targetValues,
            bool stopBlink,
            float durationSec,
            int tickMs)
        {
            if (durationSec <= 0f)
            {
                Apply(faceController, names, targetValues, stopBlink);
                if (transitionVersion == _transitionVersion)
                {
                    _runningTransition = null;
                }
                yield break;
            }

            var currentValues = new List<float>(startValues);
            while (currentValues.Count < targetValues.Count)
            {
                currentValues.Add(0f);
            }

            var tickSec = Mathf.Max(0.001f, tickMs / 1000f);
            var elapsed = 0f;
            var nextApplyTime = 0f;

            while (elapsed < durationSec)
            {
                if (transitionVersion != _transitionVersion)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed + 0.0001f >= nextApplyTime)
                {
                    var t = Mathf.Clamp01(elapsed / durationSec);
                    for (var i = 0; i < currentValues.Count; i++)
                    {
                        currentValues[i] = Mathf.Lerp(startValues[i], targetValues[i], t);
                    }

                    Apply(faceController, names, currentValues, stopBlink);
                    nextApplyTime += tickSec;
                }

                yield return null;
            }

            if (transitionVersion == _transitionVersion)
            {
                Apply(faceController, names, targetValues, stopBlink);
                _runningTransition = null;
            }
        }

        private void Apply(FaceController faceController, List<string> names, List<float> values, bool stopBlink)
        {
            faceController.SetFace(names, values, stopBlink);
            for (var i = 0; i < names.Count; i++)
            {
                _lastAppliedValues[names[i]] = values[i];
            }
        }

        private List<float> CaptureStartValues(FaceController faceController, List<string> names)
        {
            var result = new List<float>(names.Count);

            IDictionary keyMap = null;
            IDictionary currentMap = null;

            if (_blendShapeKeyStringField != null)
            {
                keyMap = _blendShapeKeyStringField.GetValue(faceController) as IDictionary;
            }

            if (_currentShapeKeysField != null)
            {
                currentMap = _currentShapeKeysField.GetValue(faceController) as IDictionary;
            }

            foreach (var name in names)
            {
                var value = 0f;

                if (keyMap != null && currentMap != null && keyMap.Contains(name))
                {
                    var keyObject = keyMap[name];
                    if (keyObject != null && currentMap.Contains(keyObject))
                    {
                        value = ConvertToFloat(currentMap[keyObject]);
                    }
                    else if (_lastAppliedValues.TryGetValue(name, out var cached))
                    {
                        value = cached;
                    }
                }
                else if (_lastAppliedValues.TryGetValue(name, out var cached))
                {
                    value = cached;
                }

                result.Add(Mathf.Clamp01(value));
            }

            return result;
        }

        private static float ConvertToFloat(object value)
        {
            if (value == null)
            {
                return 0f;
            }

            if (value is float f)
            {
                return f;
            }

            if (value is double d)
            {
                return (float)d;
            }

            if (value is int i)
            {
                return i;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return 0f;
            }
        }
    }
}
