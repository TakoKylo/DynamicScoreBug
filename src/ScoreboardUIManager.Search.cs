using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // SEARCH box support (matches Competitive Adjustments). Rows built by the
        // Make*Row helpers tag themselves via MarkSearchable; ApplySearchFilter
        // shows/hides them on the active tab as the query changes.

        // Returns the content container for the active tab, so the search filter
        // only touches the rows the user is currently looking at.
        private UITK.VisualElement ActiveTabContainer()
        {
            switch (_activeTab)
            {
                case ScoreboardTab.General: return _generalContent;
                case ScoreboardTab.Presets: return _presetsContent;
                case ScoreboardTab.Advanced: return _advancedContent;
                case ScoreboardTab.Tests: return _testsContent;
                default: return null;
            }
        }

        // Tags a row so the SEARCH box can show/hide it by its label text. The
        // searchable text is stored in userData; the "cfg-row" class lets
        // ApplySearchFilter collect every row regardless of which builder made it.
        private static void MarkSearchable(UITK.VisualElement row, string title)
        {
            if (row == null) return;
            row.userData = StripRichText(title);
            row.AddToClassList("cfg-row");
        }

        // Strips <...> rich-text tags but keeps their inner text, so a query like
        // "size" doesn't match the "<size=12>" markup some labels carry, while the
        // visible hint words ("leave empty for team color") stay searchable.
        private static string StripRichText(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return Regex.Replace(s, "<[^>]+>", "");
        }

        // Filters the active tab's rows by the SEARCH query. An empty query shows
        // everything; a non-empty query hides non-matching rows and the section
        // headers, so the results read as one flat list. Tabs with no rows (the
        // button-only Presets/Tests tabs) are left untouched.
        private void ApplySearchFilter()
        {
            var container = ActiveTabContainer();
            if (container == null) return;

            var rows = container.Query(className: "cfg-row").ToList();
            if (rows.Count == 0) return; // nothing row-like to filter on this tab

            string q = (_searchQuery ?? "").Trim().ToLowerInvariant();
            bool searching = q.Length > 0;

            foreach (var row in rows)
            {
                string title = (row.userData as string ?? "").ToLowerInvariant();
                bool match = !searching || title.Contains(q);
                row.style.display = match ? UITK.DisplayStyle.Flex : UITK.DisplayStyle.None;
            }

            foreach (var header in container.Query(className: "cfg-header").ToList())
                header.style.display = searching ? UITK.DisplayStyle.None : UITK.DisplayStyle.Flex;
        }

        private void RefreshUI()
        {
            // Don't reload config from disk - just recreate UI with current config.
            // This prevents losing in-memory changes like newly added presets.

            // Remember the active tab and scroll position so a rebuild (e.g. after
            // applying a preset from the PRESETS tab) doesn't snap the user back to
            // GENERAL / the top of the list.
            var prevTab = _activeTab;
            float scrollOffset = 0f;
            if (_mainScrollView != null)
            {
                scrollOffset = _mainScrollView.scrollOffset.y;
            }

            // Recreate the entire UI
            if (_overlayBackground != null && _overlayBackground.parent != null)
            {
                _overlayBackground.RemoveFromHierarchy();
            }
            _overlayBackground = null;
            _configPanel = null;
            _isUIVisible = false;

            CreateUI();
            SwitchToTab(prevTab); // CreateUI defaults to GENERAL; restore the user's tab
            ShowUI();

            // Restore scroll position
            if (_mainScrollView != null)
            {
                _mainScrollView.scrollOffset = new Vector2(0, scrollOffset);
            }
        }
    }
}
