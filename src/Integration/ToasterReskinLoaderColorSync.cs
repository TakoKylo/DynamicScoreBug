using System;
using UnityEngine;
using CustomScoreboard;

namespace CustomScoreboard.Integration
{
    /// <summary>
    /// Syncs Scoreboard color configuration with ToasterReskinLoader.
    /// Allows Scoreboard to control both UI colors AND equipment colors via one hex config.
    /// </summary>
    public static class ToasterReskinLoaderColorSync
    {
        private static Type _toasterLoaderType;
        private static Type _reskinProfileManagerType;
        private static bool _isToasterLoaded = false;

        static ToasterReskinLoaderColorSync()
        {
            try
            {
                // Try to find ToasterReskinLoader types
                _toasterLoaderType = Type.GetType("ToasterReskinLoader.ToasterReskinLoaderAPI, ToasterReskinLoader");
                _reskinProfileManagerType = Type.GetType("ToasterReskinLoader.ReskinProfileManager, ToasterReskinLoader");
                
                if (_reskinProfileManagerType != null)
                {
                    _isToasterLoaded = true;
                    Debug.Log("[Scoreboard] ToasterReskinLoader detected - color sync enabled");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] ToasterReskinLoader not found or error initializing: {ex.Message}");
                _isToasterLoaded = false;
            }
        }

        /// <summary>
        /// Syncs Scoreboard's hex colors to ToasterReskinLoader's team color system.
        /// Call this whenever your hex colors change in config.
        /// </summary>
        public static void SyncTeamColors(string blueHexColor, string redHexColor)
        {
            if (!_isToasterLoaded || _reskinProfileManagerType == null)
            {
                return;
            }

            try
            {
                // Parse hex colors
                Color blueColor = ParseHexColor(blueHexColor, new Color(0.231f, 0.51f, 0.965f, 1f));
                Color redColor = ParseHexColor(redHexColor, new Color(0.82f, 0.2f, 0.2f, 1f));

                // Get currentProfile
                var currentProfileProperty = _reskinProfileManagerType.GetProperty("currentProfile", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (currentProfileProperty == null)
                {
                    Debug.LogWarning("[Scoreboard] Could not find ReskinProfileManager.currentProfile");
                    return;
                }

                object profile = currentProfileProperty.GetValue(null);
                if (profile == null)
                {
                    Debug.LogWarning("[Scoreboard] ReskinProfileManager.currentProfile is null");
                    return;
                }

                // Set color fields on profile
                var blueTeamColorField = profile.GetType().GetField("blueTeamColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var redTeamColorField = profile.GetType().GetField("redTeamColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (blueTeamColorField != null)
                {
                    blueTeamColorField.SetValue(profile, blueColor);
                    Debug.Log($"[Scoreboard] Synced blue team color: {blueHexColor} -> {blueColor}");
                }

                if (redTeamColorField != null)
                {
                    redTeamColorField.SetValue(profile, redColor);
                    Debug.Log($"[Scoreboard] Synced red team color: {redHexColor} -> {redColor}");
                }

                // Notify ToasterReskinLoader to refresh (via reflection)
                NotifyToasterColorsChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Error syncing colors to ToasterReskinLoader: {ex}");
            }
        }

        /// <summary>
        /// Syncs minimap colors to ToasterReskinLoader.
        /// If minimap colors are empty, falls back to team colors (player) or team text colors (numbers).
        /// </summary>
        public static void SyncMinimapColors(string bluePlayerHex, string redPlayerHex, string blueNumberHex, string redNumberHex, string blueTeamColorHex, string redTeamColorHex, string blueTeamTextColorHex, string redTeamTextColorHex)
        {
            if (!_isToasterLoaded || _reskinProfileManagerType == null)
            {
                return;
            }

            try
            {
                // If minimap player colors are empty, use team colors
                if (string.IsNullOrEmpty(bluePlayerHex))
                    bluePlayerHex = blueTeamColorHex;
                if (string.IsNullOrEmpty(redPlayerHex))
                    redPlayerHex = redTeamColorHex;

                // If minimap number colors are empty, use team text colors
                if (string.IsNullOrEmpty(blueNumberHex))
                    blueNumberHex = blueTeamTextColorHex;
                if (string.IsNullOrEmpty(redNumberHex))
                    redNumberHex = redTeamTextColorHex;

                Color bluePlayerColor = ParseHexColor(bluePlayerHex, new Color(0.231f, 0.51f, 0.965f, 1f));
                Color redPlayerColor = ParseHexColor(redPlayerHex, new Color(0.82f, 0.2f, 0.2f, 1f));
                Color blueNumberColor = ParseHexColor(blueNumberHex, Color.white);
                Color redNumberColor = ParseHexColor(redNumberHex, Color.white);

                var currentProfileProperty = _reskinProfileManagerType.GetProperty("currentProfile", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                object profile = currentProfileProperty?.GetValue(null);
                if (profile == null)
                    return;

                var profileType = profile.GetType();

                // Try to set minimap player body colors
                var blueMinimapPlayerField = profileType.GetField("blueMinimapPlayerColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var redMinimapPlayerField = profileType.GetField("redMinimapPlayerColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (blueMinimapPlayerField != null)
                    blueMinimapPlayerField.SetValue(profile, bluePlayerColor);
                if (redMinimapPlayerField != null)
                    redMinimapPlayerField.SetValue(profile, redPlayerColor);

                // Try to set minimap number/text colors
                var blueMinimapNumberField = profileType.GetField("blueMinimapNumberColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var redMinimapNumberField = profileType.GetField("redMinimapNumberColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (blueMinimapNumberField != null)
                    blueMinimapNumberField.SetValue(profile, blueNumberColor);
                if (redMinimapNumberField != null)
                    redMinimapNumberField.SetValue(profile, redNumberColor);

                Debug.Log($"[Scoreboard] Synced minimap colors - Blue Player: {bluePlayerColor}, Red Player: {redPlayerColor}, Blue Number: {blueNumberColor}, Red Number: {redNumberColor}");

                // Notify ToasterReskinLoader
                NotifyToasterMinimapChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Error syncing minimap colors: {ex}");
            }
        }

        /// <summary>
        /// Gets the current blue team color from ToasterReskinLoader (or fallback default).
        /// Useful if you want to use ToasterReskinLoader's colors in your Scoreboard patches.
        /// </summary>
        public static Color GetBlueTeamColorFromToaster()
        {
            if (!_isToasterLoaded || _toasterLoaderType == null)
            {
                return new Color(0.231f, 0.51f, 0.965f, 1f); // Default blue
            }

            try
            {
                var blueColorProperty = _toasterLoaderType.GetProperty("BlueTeamColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (blueColorProperty != null)
                {
                    return (Color)blueColorProperty.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Could not get blue color from ToasterReskinLoader: {ex.Message}");
            }

            return new Color(0.231f, 0.51f, 0.965f, 1f);
        }

        /// <summary>
        /// Gets the current red team color from ToasterReskinLoader (or fallback default).
        /// </summary>
        public static Color GetRedTeamColorFromToaster()
        {
            if (!_isToasterLoaded || _toasterLoaderType == null)
            {
                return new Color(0.82f, 0.2f, 0.2f, 1f); // Default red
            }

            try
            {
                var redColorProperty = _toasterLoaderType.GetProperty("RedTeamColor", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (redColorProperty != null)
                {
                    return (Color)redColorProperty.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Could not get red color from ToasterReskinLoader: {ex.Message}");
            }

            return new Color(0.82f, 0.2f, 0.2f, 1f);
        }

        /// <summary>
        /// Checks if ToasterReskinLoader is loaded and available.
        /// </summary>
        public static bool IsToasterLoaderAvailable => _isToasterLoaded;

        /// <summary>
        /// Syncs Scoreboard's configured team names to ToasterReskinLoader's team name system.
        /// TRL uses these names in goal announcements and team select buttons.
        /// Call this whenever team names change in the Scoreboard config.
        /// </summary>
        public static void SyncTeamNames(string blueTeamName, string redTeamName)
        {
            if (!_isToasterLoaded || _reskinProfileManagerType == null)
                return;

            try
            {
                var currentProfileProperty = _reskinProfileManagerType.GetProperty("currentProfile",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                object profile = currentProfileProperty?.GetValue(null);
                if (profile == null)
                    return;

                var profileType = profile.GetType();

                var blueNameField = profileType.GetField("blueTeamName",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var redNameField = profileType.GetField("redTeamName",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (blueNameField != null) blueNameField.SetValue(profile, blueTeamName);
                if (redNameField != null) redNameField.SetValue(profile, redTeamName);

                Debug.Log($"[Scoreboard] Synced team names to TRL - Blue: '{blueTeamName}', Red: '{redTeamName}'");

                // Notify TRL to refresh team select buttons (which display team names)
                NotifyToasterColorsChanged();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Error syncing team names to ToasterReskinLoader: {ex.Message}");
            }
        }

        private static void NotifyToasterColorsChanged()
        {
            try
            {
                // Try to call NotifyTeamColorsChanged via reflection (it's internal)
                var notifyMethod = _reskinProfileManagerType?.DeclaringType?
                    .GetMethod("NotifyTeamColorsChanged", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                notifyMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Could not notify ToasterReskinLoader of color change: {ex.Message}");
            }
        }

        private static void NotifyToasterMinimapChanged()
        {
            try
            {
                // Try to call NotifyMinimapSettingsChanged via reflection (it's internal)
                var notifyMethod = _reskinProfileManagerType?.DeclaringType?
                    .GetMethod("NotifyMinimapSettingsChanged", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                notifyMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Could not notify ToasterReskinLoader of minimap change: {ex.Message}");
            }
        }

        private static Color ParseHexColor(string hexColor, Color fallback)
        {
            if (string.IsNullOrEmpty(hexColor)) return fallback;
            if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
            return ColorUtility.TryParseHtmlString(hexColor, out var color) ? color : fallback;
        }
    }
}
