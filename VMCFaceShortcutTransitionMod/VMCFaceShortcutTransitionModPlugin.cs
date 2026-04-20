using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityMemoryMappedFile;
using VMC;
using VMCMod;

namespace VMCFaceShortcutTransitionMod
{
    [VMCPlugin(
        Name: "VMCFaceShortcutTransitionMod",
        Version: "0.1.0",
        Author: "Rynan4818",
        Description: "Adds transition to face shortcut actions via Harmony patch.",
        AuthorURL: "https://github.com/rynan4818",
        PluginURL: "https://github.com/rynan4818/VMCFaceShortcutTransitionMod")]
    public class VMCFaceShortcutTransitionModPlugin : MonoBehaviour
    {
        private const string HarmonyId = "com.github.rynan4818.VMCFaceShortcutTransitionMod";
        private const string ConfigFileName = "VMCFaceShortcutTransitionMod.json";

        public static VMCFaceShortcutTransitionModPlugin Instance { get; private set; }

        private Harmony _harmony;
        private Config _config;
        private string _configPath;
        private FaceShortcutTransitionCoordinator _coordinator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            LoadConfig();

            _coordinator = new FaceShortcutTransitionCoordinator(this);

            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("[VMCFaceShortcutTransitionMod] Harmony patch applied.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            _coordinator?.Stop();

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        [OnSetting]
        public void OnSetting()
        {
            LoadConfig();
            Debug.Log("[VMCFaceShortcutTransitionMod] Config reloaded.");
        }

        internal bool HandleDoKeyActionPrefix(ControlWPFWindow window, KeyAction action)
        {
            if (_config == null || _config.Enable == false)
            {
                return true;
            }

            if (!IsFaceShortcutAction(action))
            {
                return true;
            }

            if (!IsTransitionTarget(action))
            {
                _coordinator.Stop();
                return true;
            }

            var transitionSec = ResolveTransitionSec(action);
            if (transitionSec <= 0f)
            {
                _coordinator.Stop();
                return true;
            }

            if (window == null || window.faceController == null)
            {
                return true;
            }

            ApplyOriginalSideEffects(window, action);

            _coordinator.StartTransition(
                window.faceController,
                action.FaceNames,
                action.FaceStrength,
                action.StopBlink,
                transitionSec,
                _config.TickMs);

            return false;
        }

        private static bool IsFaceShortcutAction(KeyAction action)
        {
            if (action == null)
            {
                return false;
            }

            if (!action.FaceAction)
            {
                return false;
            }

            if (action.FaceNames == null || action.FaceStrength == null)
            {
                return false;
            }

            if (action.FaceNames.Count == 0 || action.FaceNames.Count != action.FaceStrength.Count)
            {
                return false;
            }

            return true;
        }

        private bool IsTransitionTarget(KeyAction action)
        {
            if (!IsFaceShortcutAction(action))
            {
                return false;
            }

            if (_config.ApplyOnlyWhenSoftChangeFalse && action.SoftChange)
            {
                return false;
            }

            return true;
        }

        private float ResolveTransitionSec(KeyAction action)
        {
            if (_config.ActionRules != null && !string.IsNullOrWhiteSpace(action.Name))
            {
                var rule = _config.ActionRules.FirstOrDefault(x =>
                    string.Equals(x.ActionName, action.Name, StringComparison.OrdinalIgnoreCase));

                if (rule != null)
                {
                    if (!rule.Enable)
                    {
                        return 0f;
                    }

                    return Mathf.Max(0f, rule.TransitionSec);
                }
            }

            return Mathf.Max(0f, _config.DefaultTransitionSec);
        }

        private static void ApplyOriginalSideEffects(ControlWPFWindow window, KeyAction action)
        {
            if (window.externalMotionReceivers != null)
            {
                foreach (var receiver in window.externalMotionReceivers)
                {
                    if (receiver != null)
                    {
                        receiver.DisableBlendShapeReception = action.DisableBlendShapeReception;
                    }
                }
            }

            if (window.LipSync != null)
            {
                window.LipSync.MaxLevel = action.LipSyncMaxLevel;
            }
        }

        private void LoadConfig()
        {
            try
            {
                var dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(dllDirectory))
                {
                    throw new InvalidOperationException("Failed to resolve mod DLL directory.");
                }

                _configPath = Path.Combine(dllDirectory, ConfigFileName);

                if (File.Exists(_configPath))
                {
                    _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
                }

                if (_config == null)
                {
                    _config = new Config();
                }

                EnsureConfigDefaults(_config);

                if (!File.Exists(_configPath))
                {
                    File.WriteAllText(_configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _config = new Config();
                EnsureConfigDefaults(_config);
            }
        }

        private static void EnsureConfigDefaults(Config config)
        {
            if (config.DefaultTransitionSec < 0f)
            {
                config.DefaultTransitionSec = 0f;
            }

            if (config.TickMs < 1)
            {
                config.TickMs = 1;
            }

            if (config.ActionRules == null)
            {
                config.ActionRules = new System.Collections.Generic.List<ActionRule>();
            }
        }
    }
}
