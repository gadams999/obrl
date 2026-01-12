using System;
using WheelOverlay.ViewModels;
using WheelOverlay.Services;

namespace WheelOverlay.Models
{
    /// <summary>
    /// Validates layout configurations before rendering to prevent crashes.
    /// </summary>
    public static class LayoutValidator
    {
        /// <summary>
        /// Validates that the ViewModel is properly configured for vertical layout rendering.
        /// </summary>
        /// <param name="viewModel">The OverlayViewModel to validate</param>
        /// <returns>True if the ViewModel is valid for vertical layout, false otherwise</returns>
        public static bool ValidateVerticalLayout(OverlayViewModel viewModel)
        {
            if (viewModel == null)
            {
                LogService.Error("VerticalLayout: ViewModel is null");
                return false;
            }

            if (viewModel.Settings == null)
            {
                LogService.Error("VerticalLayout: Settings is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile == null)
            {
                LogService.Error("VerticalLayout: ActiveProfile is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile.TextLabels == null)
            {
                LogService.Error("VerticalLayout: TextLabels collection is null");
                return false;
            }

            LogService.Info($"VerticalLayout: Validation passed. Profile: {viewModel.Settings.ActiveProfile.Name}, Labels: {viewModel.Settings.ActiveProfile.TextLabels.Count}");
            return true;
        }

        /// <summary>
        /// Validates that the ViewModel is properly configured for horizontal layout rendering.
        /// </summary>
        /// <param name="viewModel">The OverlayViewModel to validate</param>
        /// <returns>True if the ViewModel is valid for horizontal layout, false otherwise</returns>
        public static bool ValidateHorizontalLayout(OverlayViewModel viewModel)
        {
            if (viewModel == null)
            {
                LogService.Error("HorizontalLayout: ViewModel is null");
                return false;
            }

            if (viewModel.Settings == null)
            {
                LogService.Error("HorizontalLayout: Settings is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile == null)
            {
                LogService.Error("HorizontalLayout: ActiveProfile is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile.TextLabels == null)
            {
                LogService.Error("HorizontalLayout: TextLabels collection is null");
                return false;
            }

            LogService.Info($"HorizontalLayout: Validation passed. Profile: {viewModel.Settings.ActiveProfile.Name}, Labels: {viewModel.Settings.ActiveProfile.TextLabels.Count}");
            return true;
        }

        /// <summary>
        /// Validates that the ViewModel is properly configured for grid layout rendering.
        /// </summary>
        /// <param name="viewModel">The OverlayViewModel to validate</param>
        /// <returns>True if the ViewModel is valid for grid layout, false otherwise</returns>
        public static bool ValidateGridLayout(OverlayViewModel viewModel)
        {
            if (viewModel == null)
            {
                LogService.Error("GridLayout: ViewModel is null");
                return false;
            }

            if (viewModel.Settings == null)
            {
                LogService.Error("GridLayout: Settings is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile == null)
            {
                LogService.Error("GridLayout: ActiveProfile is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile.TextLabels == null)
            {
                LogService.Error("GridLayout: TextLabels collection is null");
                return false;
            }

            var profile = viewModel.Settings.ActiveProfile;
            if (profile.GridRows <= 0 || profile.GridColumns <= 0)
            {
                LogService.Error($"GridLayout: Invalid grid dimensions. Rows: {profile.GridRows}, Columns: {profile.GridColumns}");
                return false;
            }

            LogService.Info($"GridLayout: Validation passed. Profile: {profile.Name}, Grid: {profile.GridRows}x{profile.GridColumns}, Labels: {profile.TextLabels.Count}");
            return true;
        }

        /// <summary>
        /// Validates that the ViewModel is properly configured for single layout rendering.
        /// </summary>
        /// <param name="viewModel">The OverlayViewModel to validate</param>
        /// <returns>True if the ViewModel is valid for single layout, false otherwise</returns>
        public static bool ValidateSingleLayout(OverlayViewModel viewModel)
        {
            if (viewModel == null)
            {
                LogService.Error("SingleLayout: ViewModel is null");
                return false;
            }

            if (viewModel.Settings == null)
            {
                LogService.Error("SingleLayout: Settings is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile == null)
            {
                LogService.Error("SingleLayout: ActiveProfile is null");
                return false;
            }

            if (viewModel.Settings.ActiveProfile.TextLabels == null)
            {
                LogService.Error("SingleLayout: TextLabels collection is null");
                return false;
            }

            LogService.Info($"SingleLayout: Validation passed. Profile: {viewModel.Settings.ActiveProfile.Name}, Labels: {viewModel.Settings.ActiveProfile.TextLabels.Count}");
            return true;
        }

        /// <summary>
        /// Validates layout configuration for any layout type.
        /// </summary>
        /// <param name="viewModel">The OverlayViewModel to validate</param>
        /// <param name="layout">The layout type to validate for</param>
        /// <returns>True if the ViewModel is valid for the specified layout, false otherwise</returns>
        public static bool ValidateLayout(OverlayViewModel viewModel, DisplayLayout layout)
        {
            return layout switch
            {
                DisplayLayout.Vertical => ValidateVerticalLayout(viewModel),
                DisplayLayout.Horizontal => ValidateHorizontalLayout(viewModel),
                DisplayLayout.Grid => ValidateGridLayout(viewModel),
                DisplayLayout.Single => ValidateSingleLayout(viewModel),
                _ => false
            };
        }
    }
}
