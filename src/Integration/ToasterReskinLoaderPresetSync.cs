using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomScoreboard.Integration
{
    /// <summary>
    /// Bridges Scoreboard team presets to ToasterReskinLoader's preset system
    /// (the preset feature TRL added in 2026 - namespace ToasterReskinLoader.presets).
    ///
    /// A Scoreboard team preset can optionally name a TRL preset; when that team preset is
    /// applied to the blue or red side, the matching TRL preset is applied to the same side.
    ///
    /// Everything is done via reflection so the Scoreboard keeps compiling and running against
    /// older TRL builds (or with no TRL installed). When the preset API is absent every method
    /// here degrades to a safe no-op and the linking UI simply doesn't appear.
    /// </summary>
    public static class ToasterReskinLoaderPresetSync
    {
        private static readonly Type _presetStoreType;
        private static readonly Type _presetApplierType;
        private static readonly Type _presetType;
        private static readonly Type _presetTeamType;
        private static readonly bool _available;

        static ToasterReskinLoaderPresetSync()
        {
            try
            {
                _presetStoreType = Type.GetType("ToasterReskinLoader.presets.PresetStore, ToasterReskinLoader");
                _presetApplierType = Type.GetType("ToasterReskinLoader.presets.PresetApplier, ToasterReskinLoader");
                _presetType = Type.GetType("ToasterReskinLoader.presets.Preset, ToasterReskinLoader");
                _presetTeamType = Type.GetType("ToasterReskinLoader.presets.PresetTeam, ToasterReskinLoader");

                _available = _presetStoreType != null && _presetApplierType != null
                    && _presetType != null && _presetTeamType != null;

                if (_available)
                    Debug.Log("[Scoreboard] ToasterReskinLoader preset system detected - preset linking enabled");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] ToasterReskinLoader preset system not available: {ex.Message}");
                _available = false;
            }
        }

        /// <summary>True when the installed TRL build exposes the preset API.</summary>
        public static bool IsPresetSystemAvailable => _available;

        /// <summary>
        /// Returns the names of all user + pack presets known to TRL (sorted, de-duplicated).
        /// Empty list when the preset system is unavailable.
        /// </summary>
        public static List<string> GetPresetNames()
        {
            var names = new List<string>();
            if (!_available) return names;

            try
            {
                CollectNames(names, "LoadUserPresets");
                CollectNames(names, "LoadPackPresets");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Scoreboard] Failed to read TRL presets: {ex.Message}");
            }

            return names
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Finds the named TRL preset and applies it to the requested side (blue or red).
        /// Returns true if a matching preset was found and applied. Safe no-op (false) when the
        /// preset system is unavailable, the name is empty, or no preset matches.
        /// </summary>
        public static bool ApplyPresetByName(string presetName, bool toBlueSide)
        {
            if (!_available || string.IsNullOrEmpty(presetName)) return false;

            try
            {
                object preset = FindPresetByName(presetName);
                if (preset == null)
                {
                    Debug.LogWarning($"[Scoreboard] TRL preset '{presetName}' not found - skipping");
                    return false;
                }

                object team = ParseTeam(toBlueSide ? "Blue" : "Red");

                // PresetApplyResult Apply(Preset preset, PresetTeam targetTeam = PresetTeam.None)
                var applyMethod = _presetApplierType.GetMethod("Apply",
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { _presetType, _presetTeamType }, null);
                if (applyMethod == null)
                {
                    Debug.LogWarning("[Scoreboard] TRL PresetApplier.Apply(Preset, PresetTeam) not found");
                    return false;
                }

                applyMethod.Invoke(null, new[] { preset, team });
                Debug.Log($"[Scoreboard] Applied TRL preset '{presetName}' to {(toBlueSide ? "blue" : "red")} side");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Error applying TRL preset '{presetName}': {ex}");
                return false;
            }
        }

        private static void CollectNames(List<string> names, string loaderMethodName)
        {
            var presets = InvokeLoader(loaderMethodName);
            if (presets == null) return;

            var nameProp = _presetType.GetProperty("PresetName",
                BindingFlags.Public | BindingFlags.Instance);
            if (nameProp == null) return;

            foreach (var preset in presets)
            {
                if (preset == null) continue;
                names.Add(nameProp.GetValue(preset) as string);
            }
        }

        private static object FindPresetByName(string presetName)
        {
            var nameProp = _presetType.GetProperty("PresetName",
                BindingFlags.Public | BindingFlags.Instance);
            if (nameProp == null) return null;

            foreach (var loaderMethodName in new[] { "LoadUserPresets", "LoadPackPresets" })
            {
                var presets = InvokeLoader(loaderMethodName);
                if (presets == null) continue;

                foreach (var preset in presets)
                {
                    if (preset == null) continue;
                    var name = nameProp.GetValue(preset) as string;
                    if (string.Equals(name, presetName, StringComparison.Ordinal))
                        return preset;
                }
            }
            return null;
        }

        private static IEnumerable InvokeLoader(string loaderMethodName)
        {
            var method = _presetStoreType.GetMethod(loaderMethodName,
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            return method?.Invoke(null, null) as IEnumerable;
        }

        private static object ParseTeam(string member)
        {
            try { return Enum.Parse(_presetTeamType, member); }
            catch { return Enum.Parse(_presetTeamType, "None"); }
        }
    }
}
