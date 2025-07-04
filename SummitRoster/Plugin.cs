using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using SettingsExtender;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Settings;

namespace ProgressMap
{

    [BepInPlugin("SummitRoster", "Summit Roster", "1.1.3")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = null!;

        private void Awake()
        {
            Logger = base.Logger;
            SettingsRegistry.Register("Roster");
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        private void Start()
        {
            SettingsHandler.Instance.AddSetting(new RosterDisplayModeSetting());
            SettingsHandler.Instance.AddSetting(new RosterDisplayPositionSetting());
            SettingsHandler.Instance.AddSetting(new BarLengthSetting());
            SettingsHandler.Instance.AddSetting(new BarThicknessSetting());
            SettingsHandler.Instance.AddSetting(new OffsetSetting());
            SettingsHandler.Instance.AddSetting(new LabelFontSizeSetting());
            SettingsHandler.Instance.AddSetting(new PeakFontSizeSetting());
            SettingsHandler.Instance.AddSetting(new PlayerLimitSetting());
        }

        [HarmonyPatch(typeof(RunManager), "StartRun")]
        [HarmonyPostfix]
        static void Post_LoadIsland()
        {
            if (FindObjectOfType<ProgressMap>() == null)
            {
                var go = new GameObject("ProgressMap");
                go.AddComponent<ProgressMap>();
                DontDestroyOnLoad(go);
            }
        }
    }

    public class ProgressMap : MonoBehaviourPunCallbacks
    {
        private class PlayerUIElements
        {
            public GameObject RootGO;
            public GameObject MarkerGO;
            public GameObject TextGO;
        }

        private GameObject? overlay;
        private GameObject? peakGO;
        private TMP_FontAsset? mainFont;
        private readonly Dictionary<Character, PlayerUIElements> playerUIElements = new Dictionary<Character, PlayerUIElements>();
        private PlayerUIElements? summitLabel;

        private const float TotalMountainHeight = 1920f;
        // --- UPDATED: Summit threshold is now 7999m ---
        private const float SummitThreshold = 7999f;
        private const float DisplayRange = 100f;

        // State variables
        private RosterDisplayPosition currentPosition;
        private RosterDisplayMode currentMode;
        private float currentBarLength, currentBarThickness, currentOffset, currentLabelFontSize, currentPeakFontSize;
        private int currentPlayerLimit;

        private void Awake()
        {
            SetupUI();
        }

        void SetupUI()
        {
            if (overlay != null) { Destroy(overlay); }

            currentMode = SettingsHandler.Instance.GetSetting<RosterDisplayModeSetting>().Value;
            currentPosition = SettingsHandler.Instance.GetSetting<RosterDisplayPositionSetting>().Value;
            currentBarLength = SettingsHandler.Instance.GetSetting<BarLengthSetting>().Value;
            currentBarThickness = SettingsHandler.Instance.GetSetting<BarThicknessSetting>().Value;
            currentOffset = SettingsHandler.Instance.GetSetting<OffsetSetting>().Value;
            currentLabelFontSize = SettingsHandler.Instance.GetSetting<LabelFontSizeSetting>().Value;
            currentPeakFontSize = SettingsHandler.Instance.GetSetting<PeakFontSizeSetting>().Value;
            currentPlayerLimit = (int)SettingsHandler.Instance.GetSetting<PlayerLimitSetting>().Value;

            overlay = new GameObject("ProgressMapOverlay");
            overlay.transform.SetParent(transform);

            var canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            mainFont = FindObjectsOfType<TMP_FontAsset>(true).FirstOrDefault(a => a.faceInfo.familyName == "Daruma Drop One");

            var barGO = new GameObject("AltitudeBar");
            barGO.transform.SetParent(overlay.transform, false);
            var barRect = barGO.AddComponent<RectTransform>();
            var barImage = barGO.AddComponent<Image>();
            barImage.color = new Color(0.75f, 0.75f, 0.69f, 0.3f);

            peakGO = new GameObject("PeakText", typeof(RectTransform), typeof(TextMeshProUGUI));
            peakGO.transform.SetParent(barGO.transform, false);
            var peakText = peakGO.GetComponent<TextMeshProUGUI>();
            peakText.font = mainFont;
            peakText.text = "PEAK";
            peakText.fontSize = currentPeakFontSize;
            peakText.color = new Color(1f, 1f, 1f, 0.3f);
            peakText.alignment = TextAlignmentOptions.Center;
            var peakRect = peakGO.GetComponent<RectTransform>();

            // Load font
            if (mainFont == null)
            {
                TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                mainFont = fontAssets.FirstOrDefault(a => a.faceInfo.familyName == "Daruma Drop One");
            }

            switch (currentPosition)
            {
                case RosterDisplayPosition.Left:
                    barRect.anchorMin = new Vector2(0, 0.5f); barRect.anchorMax = new Vector2(0, 0.5f);
                    barRect.sizeDelta = new Vector2(currentBarThickness, currentBarLength);
                    barRect.anchoredPosition = new Vector2(currentOffset, 0);
                    peakRect.anchorMin = peakRect.anchorMax = new Vector2(0.5f, 1f); peakRect.pivot = new Vector2(0.5f, 0f);
                    peakRect.anchoredPosition = new Vector2(0, 10f);
                    break;
                case RosterDisplayPosition.Right:
                    barRect.anchorMin = new Vector2(1, 0.5f); barRect.anchorMax = new Vector2(1, 0.5f);
                    barRect.sizeDelta = new Vector2(currentBarThickness, currentBarLength);
                    barRect.anchoredPosition = new Vector2(-currentOffset, 0);
                    peakRect.anchorMin = peakRect.anchorMax = new Vector2(0.5f, 1f); peakRect.pivot = new Vector2(0.5f, 0f);
                    peakRect.anchoredPosition = new Vector2(0, 10f);
                    break;
                case RosterDisplayPosition.Top:
                    barRect.anchorMin = new Vector2(0.5f, 1f); barRect.anchorMax = new Vector2(0.5f, 1f);
                    barRect.sizeDelta = new Vector2(currentBarLength, currentBarThickness);
                    barRect.anchoredPosition = new Vector2(0, -currentOffset);
                    peakRect.anchorMin = peakRect.anchorMax = new Vector2(1f, 0.5f); peakRect.pivot = new Vector2(0f, 0.5f);
                    peakRect.anchoredPosition = new Vector2(10f, 0);
                    break;
                case RosterDisplayPosition.Bottom:
                    barRect.anchorMin = new Vector2(0.5f, 0); barRect.anchorMax = new Vector2(0.5f, 0);
                    barRect.sizeDelta = new Vector2(currentBarLength, currentBarThickness);
                    barRect.anchoredPosition = new Vector2(0, currentOffset);
                    peakRect.anchorMin = peakRect.anchorMax = new Vector2(1f, 0.5f); peakRect.pivot = new Vector2(0f, 0.5f);
                    peakRect.anchoredPosition = new Vector2(10f, 0);
                    break;
            }

            foreach (var uiElement in playerUIElements.Values) { Destroy(uiElement.RootGO); }
            if (summitLabel != null) { Destroy(summitLabel.RootGO); }
            playerUIElements.Clear();
            summitLabel = null;
        }

        private void LateUpdate()
        {
            if (SettingsHandler.Instance.GetSetting<RosterDisplayModeSetting>().Value != currentMode ||
                SettingsHandler.Instance.GetSetting<RosterDisplayPositionSetting>().Value != currentPosition ||
                SettingsHandler.Instance.GetSetting<BarLengthSetting>().Value != currentBarLength ||
                SettingsHandler.Instance.GetSetting<BarThicknessSetting>().Value != currentBarThickness ||
                SettingsHandler.Instance.GetSetting<OffsetSetting>().Value != currentOffset ||
                SettingsHandler.Instance.GetSetting<LabelFontSizeSetting>().Value != currentLabelFontSize ||
                SettingsHandler.Instance.GetSetting<PeakFontSizeSetting>().Value != currentPeakFontSize ||
                (int)SettingsHandler.Instance.GetSetting<PlayerLimitSetting>().Value != currentPlayerLimit)
            {
                SetupUI();
            }

            if (overlay == null || peakGO == null) return;

            peakGO.SetActive(currentMode == RosterDisplayMode.Full);

            var barTransform = overlay.transform.Find("AltitudeBar");
            if (barTransform == null) return;
            var barRect = barTransform.GetComponent<RectTransform>();

            foreach (var uiElement in playerUIElements.Values) { uiElement.RootGO.SetActive(false); }
            if (summitLabel != null) { summitLabel.RootGO.SetActive(false); }

            var localPlayer = Character.localCharacter;
            var allPlayers = Character.AllCharacters.Where(c => c != null && c.refs != null && c.refs.view != null && c.refs.stats != null && (c.refs.view.IsMine || !c.refs.view.Owner.IsInactive));

            // Use the SummitThreshold
            var summitPlayers = allPlayers.Where(c => c.refs.stats.heightInMeters >= SummitThreshold).ToList();
            var climbingPlayers = allPlayers.Where(c => c.refs.stats.heightInMeters < SummitThreshold && c.refs.stats.heightInMeters > 0f).ToList();

            List<Character> closestClimbers;
            if (localPlayer != null)
            {
                closestClimbers = climbingPlayers
                    .OrderBy(c => Vector3.Distance(localPlayer.transform.position, c.transform.position))
                    .ThenBy(c => c.refs.view.ViewID)
                    .ToList();
            }
            else
            {
                closestClimbers = climbingPlayers.OrderByDescending(c => c.refs.stats.heightInMeters).ThenBy(c => c.refs.view.ViewID).ToList();
            }

            int slotsForClimbers = currentPlayerLimit;
            if (summitPlayers.Any())
            {
                slotsForClimbers--;
            }

            var finalClimberList = closestClimbers.Take(slotsForClimbers).OrderBy(c => c.refs.stats.heightInMeters).ThenBy(c => c.refs.view.ViewID).ToList();

            float lastLabelPos = -float.MaxValue;
            float verticalSpacing = currentLabelFontSize + 4f;

            foreach (var character in finalClimberList)
            {
                if (!playerUIElements.ContainsKey(character)) { AddCharacter(character); }
                var ui = playerUIElements[character];

                ui.RootGO.SetActive(true);

                float height = character.refs.stats.heightInMeters;

                var markerImage = ui.MarkerGO.GetComponent<Image>();
                var labelText = ui.TextGO.GetComponent<TextMeshProUGUI>();

                labelText.text = $"{FilterPlayerName(character.refs.view.Owner.NickName)} {height:F0}m";
                labelText.color = character.refs.customization.PlayerColor;
                markerImage.color = character.refs.customization.PlayerColor;

                float normalized = GetNormalizedHeight(localPlayer, height);
                float pixelPos = Mathf.Lerp(-currentBarLength / 2f, currentBarLength / 2f, normalized);

                ui.RootGO.transform.SetParent(barRect, false);

                // --- DE-CLUTTERING ---
                if (currentPosition == RosterDisplayPosition.Left || currentPosition == RosterDisplayPosition.Right)
                {
                    ui.TextGO.SetActive(true);
                    if (lastLabelPos > -float.MaxValue && pixelPos < lastLabelPos + verticalSpacing)
                    {
                        pixelPos = lastLabelPos + verticalSpacing;
                    }
                    ui.RootGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, pixelPos);
                    lastLabelPos = pixelPos;

                    ui.MarkerGO.transform.localPosition = Vector2.zero;
                    ui.MarkerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(currentBarThickness + 15, 2);

                    ui.TextGO.GetComponent<RectTransform>().pivot = new Vector2(currentPosition == RosterDisplayPosition.Left ? 0f : 1f, 0.5f);
                    labelText.alignment = currentPosition == RosterDisplayPosition.Left ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
                    ui.TextGO.transform.localPosition = new Vector2(currentPosition == RosterDisplayPosition.Left ? (currentBarThickness / 2f) + 10 : -(currentBarThickness / 2f) - 10, 0);
                }
                else // Top or Bottom
                {
                    ui.RootGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(pixelPos, 0);
                    ui.MarkerGO.transform.localPosition = Vector2.zero;
                    ui.MarkerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(2, currentBarThickness + 15);
                    ui.TextGO.SetActive(false);
                }
            }

            if (summitPlayers.Any() && slotsForClimbers < currentPlayerLimit)
            {
                if (summitLabel == null) { summitLabel = CreateUIElements("Summit"); }

                summitLabel.RootGO.SetActive(true);
                summitLabel.RootGO.transform.SetParent(barRect, false);

                var labelText = summitLabel.TextGO.GetComponent<TextMeshProUGUI>();
                labelText.text = $"{summitPlayers.Count}";
                labelText.color = Color.yellow;

                var markerImage = summitLabel.MarkerGO.GetComponent<Image>();
                markerImage.color = Color.yellow;

                float pixelPos = currentBarLength / 2f;

                if (currentPosition == RosterDisplayPosition.Left || currentPosition == RosterDisplayPosition.Right)
                {
                    if (lastLabelPos > -float.MaxValue && pixelPos < lastLabelPos + verticalSpacing)
                    {
                        pixelPos = lastLabelPos + verticalSpacing;
                    }
                    summitLabel.RootGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, pixelPos);
                    summitLabel.MarkerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(currentBarThickness + 15, 2);
                    summitLabel.TextGO.SetActive(true);
                }
                else
                {
                    summitLabel.RootGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(pixelPos, 0);
                    summitLabel.MarkerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(2, currentBarThickness + 15);
                    summitLabel.TextGO.SetActive(false);
                }
            }
        }

        private string FilterPlayerName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return Regex.Replace(name, @"[^a-zA-Z0-9\s._-]", "").Trim();
        }

        private float GetNormalizedHeight(Character? localPlayer, float height)
        {
            if (currentMode == RosterDisplayMode.Full)
            {
                return Mathf.InverseLerp(0f, TotalMountainHeight, height);
            }
            else
            {
                float localH = localPlayer?.refs.stats.heightInMeters ?? height;
                float logH = Mathf.Log(Mathf.Max(1f, localH));
                float logMin = logH - Mathf.Log(DisplayRange);
                float logMax = logH + Mathf.Log(DisplayRange);
                float logValue = Mathf.Log(Mathf.Max(1f, height));
                return Mathf.Clamp01(Mathf.InverseLerp(logMin, logMax, logValue));
            }
        }

        private PlayerUIElements CreateUIElements(string id)
        {
            var rootGO = new GameObject($"Label_{id}");
            rootGO.AddComponent<RectTransform>();

            var markerGO = new GameObject("Marker");
            markerGO.transform.SetParent(rootGO.transform, false);
            markerGO.AddComponent<Image>();
            markerGO.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(rootGO.transform, false);
            var labelText = textGO.AddComponent<TextMeshProUGUI>();
            labelText.font = mainFont;
            labelText.fontSize = currentLabelFontSize;

            return new PlayerUIElements { RootGO = rootGO, MarkerGO = markerGO, TextGO = textGO };
        }

        private void AddCharacter(Character character)
        {
            if (playerUIElements.ContainsKey(character)) return;
            playerUIElements[character] = CreateUIElements($"Player_{character.refs.view.ViewID}");
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) { StartCoroutine(WaitAndAddPlayer(newPlayer)); }

        private IEnumerator WaitAndAddPlayer(Photon.Realtime.Player newPlayer)
        {
            yield return new WaitUntil(() => PlayerHandler.GetPlayerCharacter(newPlayer) != null);
            AddCharacter(PlayerHandler.GetPlayerCharacter(newPlayer));
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player leavingPlayer)
        {
            var character = PlayerHandler.GetPlayerCharacter(leavingPlayer);
            if (character != null && playerUIElements.TryGetValue(character, out var ui))
            {
                Destroy(ui.RootGO);
                playerUIElements.Remove(character);
            }
        }
    }
}