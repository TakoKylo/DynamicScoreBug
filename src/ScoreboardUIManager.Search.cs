using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void FilterUIElements(string searchText)
        {
            try
            {
                if (_mainScrollView == null || _mainScrollView.contentContainer == null)
                    return;

                // If search is empty, show everything
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    ShowAllElements(_mainScrollView.contentContainer);
                    return;
                }

                searchText = searchText.ToLower();

                // Go through each top-level child
                foreach (var child in _mainScrollView.contentContainer.Children())
                {
                    bool isSection = child.ClassListContains("section");

                    if (isSection)
                    {
                        // This is a section - check its rows individually
                        bool hasVisibleRows = false;
                        bool isFirstChild = true;

                        foreach (var sectionChild in child.Children())
                        {
                            // First child in a section is usually the section label - always show it
                            if (isFirstChild)
                            {
                                sectionChild.style.display = DisplayStyle.Flex;
                                isFirstChild = false;
                                continue;
                            }

                            // For all other children (the actual setting rows), check if they match
                            bool rowMatches = RowContainsSearchText(sectionChild, searchText);
                            sectionChild.style.display = rowMatches ? DisplayStyle.Flex : DisplayStyle.None;

                            if (rowMatches)
                                hasVisibleRows = true;
                        }

                        // Hide entire section if no rows matched
                        child.style.display = hasVisibleRows ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                    else
                    {
                        // Not a section, just a standalone row
                        bool matches = RowContainsSearchText(child, searchText);
                        child.style.display = matches ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Scoreboard] Error filtering UI: " + ex.Message);
            }
        }

        private bool RowContainsSearchText(VisualElement row, string searchText)
        {
            try
            {
                // Get all text content from this row - labels, text values, button text, dropdown values
                string rowText = GetAllVisibleText(row).ToLower();
                return rowText.Contains(searchText);
            }
            catch
            {
                return false;
            }
        }

        private string GetAllVisibleText(VisualElement element)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Check this element itself
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                sb.Append(label.text).Append(" ");
            else if (element is UITK.TextField textField && !string.IsNullOrEmpty(textField.value))
                sb.Append(textField.value).Append(" ");
            else if (element is Button button && !string.IsNullOrEmpty(button.text))
                sb.Append(button.text).Append(" ");
            else if (element is UITK.DropdownField dropdown && !string.IsNullOrEmpty(dropdown.value))
                sb.Append(dropdown.value).Append(" ");

            // Check immediate children only (not deep recursive)
            foreach (var child in element.Children())
            {
                if (child is Label childLabel && !string.IsNullOrEmpty(childLabel.text))
                    sb.Append(childLabel.text).Append(" ");
                else if (child is UITK.TextField childTextField && !string.IsNullOrEmpty(childTextField.value))
                    sb.Append(childTextField.value).Append(" ");
                else if (child is Button childButton && !string.IsNullOrEmpty(childButton.text))
                    sb.Append(childButton.text).Append(" ");
                else if (child is UITK.DropdownField childDropdown && !string.IsNullOrEmpty(childDropdown.value))
                    sb.Append(childDropdown.value).Append(" ");
            }

            return sb.ToString();
        }

        private void ShowAllElements(VisualElement container)
        {
            foreach (var child in container.Children())
            {
                child.style.display = DisplayStyle.Flex;
            }
        }

        private void RefreshUI()
        {
            // Don't reload config from disk - just recreate UI with current config
            // This prevents losing in-memory changes like newly added presets

            // Save current scroll position
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
            ShowUI();

            // Restore scroll position
            if (_mainScrollView != null)
            {
                _mainScrollView.scrollOffset = new Vector2(0, scrollOffset);
            }
        }
    }
}
