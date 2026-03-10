using System;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        private sealed partial class FlatModPanel
        {
            private Text _buildingCollisionToggleText;
            private ButtonRef _buildingCollisionToggleButton;

            private void BuildBuildingSection(GameObject section)
            {
                Text title = UIFactory.CreateLabel(section, "BuildingTitle", "Building", TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 0.98f), true, 15);
                UIFactory.SetLayoutElement(title.gameObject, minHeight: 22, flexibleWidth: 9999);

                _buildingCollisionToggleButton = UIFactory.CreateButton(section, "BuildingCollisionToggle", string.Empty, new Color(0.18f, 0.2f, 0.32f, 1f));
                UIFactory.SetLayoutElement(_buildingCollisionToggleButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                _buildingCollisionToggleText = _buildingCollisionToggleButton.ButtonText;
                _buildingCollisionToggleButton.OnClick = (Action)Delegate.Combine(_buildingCollisionToggleButton.OnClick, (Action)delegate
                {
                    BuildingModConfig.DisablePlacementCollisions = !BuildingModConfig.DisablePlacementCollisions;
                    RefreshBuildingControls();
                });

                RefreshBuildingControls();
            }

            private void RefreshBuildingControls()
            {
                if (_buildingCollisionToggleText != null)
                {
                    _buildingCollisionToggleText.text = BuildingModConfig.DisablePlacementCollisions
                        ? "[x] Disable Placing Collisions"
                        : "[ ] Disable Placing Collisions";
                }

                if (_buildingCollisionToggleButton != null)
                {
                    Image image = _buildingCollisionToggleButton.GameObject.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = BuildingModConfig.DisablePlacementCollisions
                            ? new Color(0.2f, 0.36f, 0.52f, 1f)
                            : new Color(0.18f, 0.2f, 0.32f, 1f);
                    }
                }
            }
        }
    }
}
