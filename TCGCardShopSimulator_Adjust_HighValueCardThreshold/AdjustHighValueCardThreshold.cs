using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;

namespace TCGCardShopSimulator_Adjust_HighValueCardThreshold
{
    [BepInPlugin("com.TNoStone.adjusthighvaluecardthreshold", "Adjust High Value Card Threshold", "1.0.0")]
    public class AdjustHighValueCardThreshold : BaseUnityPlugin
    {
        private ConfigEntry<float> thresholdValue;
        private bool isValueSet = false;

        private void Awake()
        {
            thresholdValue = Config.Bind("",
                        "High value threshold",
                        10f,
                        new ConfigDescription("changes the value of what the game considers to be a high value card, so that it only makes a noise and pauses on the card whenever it is at or above the value of your choice",
                        new AcceptableValueRange<float>(1f, 20000f),
                        new ConfigurationManagerAttributes { Order = 1 }));

            Logger.LogInfo("Adjust High Value Card Threshold mod is loaded!");
            AdjustThreshold();

            thresholdValue.SettingChanged += (sender, e) =>
            {
                AdjustThreshold(); 
            };
        }

        private void Update()
        {
            if (!isValueSet && CSingleton<CGameManager>.Instance.m_IsGameLevel)
            {
                AdjustThreshold();
                isValueSet = true;
            }
        }
        private void AdjustThreshold()
        {
            var cardOpeningSequence = FindObjectOfType<CardOpeningSequence>();
            if (cardOpeningSequence != null)
            {
                FieldInfo thresholdField = typeof(CardOpeningSequence).GetField("m_HighValueCardThreshold", BindingFlags.Instance | BindingFlags.NonPublic);
                if (thresholdField != null && thresholdField.FieldType == typeof(float))
                {
                    thresholdField.SetValue(cardOpeningSequence, thresholdValue.Value);
                    Logger.LogInfo($"High Value Card Threshold set to {thresholdValue.Value}");
                }
                else
                {
                    Logger.LogError("Field 'm_HighValueCardThreshold' not found or has an unexpected type in CardOpeningSequence.");
                }
            }
            else
            {
                Logger.LogWarning("CardOpeningSequence not found.");
            }
        }
    }
}
