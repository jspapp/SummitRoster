using SettingsExtender;
using Unity.Mathematics;
using UnityEngine.Localization;
using Zorro.Settings;
using System.Collections.Generic;

namespace ProgressMap
{
    // --- Enums for our dropdown settings ---
    public enum RosterDisplayMode { Full, Centered }
    public enum RosterDisplayPosition { Left, Right, Top, Bottom }

    // --- Setting Classes for the "Roster" page ---

    public class RosterDisplayModeSetting : EnumSetting<RosterDisplayMode>, IExposedSetting
    {
        public string GetDisplayName() => "Display Mode";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override RosterDisplayMode GetDefaultValue() => RosterDisplayMode.Full;
        public override void ApplyValue() { }
        public override List<LocalizedString> GetLocalizedChoices() { return null; }
    }

    public class RosterDisplayPositionSetting : EnumSetting<RosterDisplayPosition>, IExposedSetting
    {
        public string GetDisplayName() => "Display Position";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override RosterDisplayPosition GetDefaultValue() => RosterDisplayPosition.Left;
        public override void ApplyValue() { }
        public override List<LocalizedString> GetLocalizedChoices() { return null; }
    }

    public class BarLengthSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "Bar Length";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 800f;
        protected override float2 GetMinMaxValue() => new float2(100f, 2000f);
        public override void ApplyValue() { }
    }

    public class BarThicknessSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "Bar Thickness";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 10f;
        protected override float2 GetMinMaxValue() => new float2(1f, 50f);
        public override void ApplyValue() { }
    }

    public class OffsetSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "Offset from Edge";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 60f;
        protected override float2 GetMinMaxValue() => new float2(0f, 500f);
        public override void ApplyValue() { }
    }

    public class LabelFontSizeSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "Label Font Size";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 18f;
        protected override float2 GetMinMaxValue() => new float2(8f, 48f);
        public override void ApplyValue() { }
    }

    public class PeakFontSizeSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "PEAK Font Size";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 24f;
        protected override float2 GetMinMaxValue() => new float2(10f, 64f);
        public override void ApplyValue() { }
    }

    public class PlayerLimitSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName() => "Player Limit";
        public string GetCategory() => SettingsRegistry.GetPageId("Roster");
        protected override float GetDefaultValue() => 32f;
        protected override float2 GetMinMaxValue() => new float2(1f, 32f);
        public override void ApplyValue() { }
    }
}