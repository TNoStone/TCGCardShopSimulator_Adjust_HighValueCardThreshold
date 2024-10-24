using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;  // Added for logging
using UnityEngine;
using System.Collections;

namespace TCGCardShopSimulator_AdjustHighValueCardThreshold
{
    [BepInPlugin("com.TNoStone.adjusthighvaluecardthreshold", "Adjust High Value Card Threshold", "1.1.0")]
    public class AdjustHighValueCardThreshold : BaseUnityPlugin
    {
        private ConfigEntry<int> thresholdValue;
        private ConfigEntry<bool> enableDebugLogging;
        private ConfigEntry<bool> modEnabled;
        private CardOpeningSequence cardOpeningSequence;
        private bool isValueSet = false;
        private Coroutine debounceCoroutine;

        private static ManualLogSource logger;

        private void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("Adjust High Value Card Threshold");

            logger.LogInfo("Adjust High Value Card Threshold Mod Loaded, Created by TNoStone");

            string section = "High Value Card Settings";

            thresholdValue = Config.Bind(section, "High value threshold", 10, new ConfigDescription(
                "Sets the value for high-value cards, triggering special behavior.",
                new AcceptableValueRange<int>(1, 20000),
                new ConfigurationManagerAttributes { Order = 1 }));

            enableDebugLogging = Config.Bind(section, "Enable Debug Logging", false,
                "Enables detailed logs for debugging and troubleshooting purposes.");

            modEnabled = Config.Bind(section, "Mod Enabled", true,
                "Enables or disables the entire functionality of this mod.");

            if (enableDebugLogging.Value)
                logger.LogDebug("Mod initialized.");

            thresholdValue.SettingChanged += (sender, e) => OnThresholdChanged();
            modEnabled.SettingChanged += (sender, e) => OnModEnabledChanged();

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            if (enableDebugLogging.Value)
                logger.LogDebug($"Scene changed to {newScene.name}. Resetting state.");

            ResetState();
            TryFindCardOpeningSequence();
        }

        private void Update()
        {
            if (modEnabled.Value && IsInGameScene())
            {
                TryFindCardOpeningSequence();

                if (cardOpeningSequence != null && !isValueSet)
                {
                    AdjustThreshold();
                    logger.LogInfo($"Applied threshold value from saved config: {thresholdValue.Value}");
                    isValueSet = true;
                }
            }
        }

        private void TryFindCardOpeningSequence()
        {
            if (cardOpeningSequence == null)
            {
                if (enableDebugLogging.Value)
                    logger.LogDebug("Searching for CardOpeningSequence...");

                cardOpeningSequence = FindObjectOfType<CardOpeningSequence>();

                if (cardOpeningSequence != null)
                {
                    if (enableDebugLogging.Value)
                        logger.LogDebug("CardOpeningSequence found.");
                }
                else if (IsInGameScene())
                {
                    logger.LogError("CardOpeningSequence not found. Threshold adjustment may fail.");
                }
                else if (enableDebugLogging.Value)
                {
                    logger.LogDebug("CardOpeningSequence not found. Likely in the main menu.");
                }
            }
        }

        private bool IsInGameScene()
        {
            return CSingleton<CGameManager>.Instance.m_IsGameLevel;
        }

        private void OnThresholdChanged()
        {
            if (!modEnabled.Value) return;

            if (debounceCoroutine != null)
                StopCoroutine(debounceCoroutine);

            debounceCoroutine = StartCoroutine(DebounceThresholdChange());
        }

        private IEnumerator DebounceThresholdChange()
        {
            if (enableDebugLogging.Value)
                logger.LogDebug("Threshold change detected. Applying in 1s...");

            yield return new WaitForSeconds(1f);

            AdjustThreshold();
            logger.LogInfo($"Mod threshold value changed to: {thresholdValue.Value}");
        }

        private void OnModEnabledChanged()
        {
            if (modEnabled.Value)
            {
                logger.LogInfo("Mod Enabled");
                ResetState();
                TryFindCardOpeningSequence();
            }
            else
            {
                logger.LogInfo("Mod Disabled");
                ResetThreshold();
                ResetState();
            }
        }

        private void ResetState()
        {
            if (enableDebugLogging.Value)
                logger.LogDebug("Resetting mod state.");

            isValueSet = false;
            cardOpeningSequence = null;
        }

        private void AdjustThreshold()
        {
            if (cardOpeningSequence != null)
            {
                cardOpeningSequence.m_HighValueCardThreshold = thresholdValue.Value;
                logger.LogInfo($"Applied threshold value to game: {thresholdValue.Value}");
            }
            else if (enableDebugLogging.Value)
            {
                logger.LogDebug("CardOpeningSequence not available.");
            }
        }

        private void ResetThreshold()
        {
            if (cardOpeningSequence != null)
            {
                cardOpeningSequence.m_HighValueCardThreshold = 10;
                logger.LogInfo("Reset threshold to default: 10");
            }
            else
            {
                logger.LogError("Failed to reset threshold. CardOpeningSequence not found.");
            }
        }
    }
}
