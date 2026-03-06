using System;
using System.Linq;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        private void TryRefreshItems(bool force)
        {
            _nextRefreshAt = UnityEngine.Time.realtimeSinceStartupAsDouble + (force ? 0.35 : 1.5);
            try
            {
                var core = Core.Get();
                if (core == null || core._componentsMap == null || core._componentsMap.Count == 0)
                {
                    return;
                }

                if (!force && _cacheReady)
                {
                    return;
                }

                _allItems.Clear();
                foreach (var kv in core._componentsMap)
                {
                    var component = kv.Value;
                    if (component == null)
                    {
                        continue;
                    }

                    string name;
                    try
                    {
                        name = component.GetName();
                    }
                    catch
                    {
                        name = component.name;
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = kv.Key.ToString();
                    }

                    _allItems.Add(new ItemEntry
                    {
                        Prefab = kv.Key,
                        Component = component,
                        Name = name
                    });
                }

                _allItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                _cacheReady = true;
                _itemsRevision++;

                if (force)
                {
                    ModLog.Info("Item cache rebuilt. Total items: " + _allItems.Count);
                }

                _panel?.RebuildItems();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Refresh item list failed: " + ex.Message);
            }
        }

        private void AssignToFirstHotbar(ItemEntry item)
        {
            try
            {
                if (item.Component == null)
                {
                    return;
                }

                var ui = UIManager._singleton;
                if (ui == null || ui._handComponentsList == null)
                {
                    ModLog.Warn("AssignToFirstHotbar aborted: UIManager/hotbar not ready.");
                    return;
                }

                ui._handComponentsList.SetItemAtIndex(0, item.Component);
                ui._handComponentsList.SetHandComponentText(item.Name);
                UIManager.PlaySoundUIClick();
            }
            catch (Exception ex)
            {
                ModLog.Error("Assign to hotbar failed: " + ex);
            }
        }

        private void UnlockAllItems()
        {
            try
            {
                if (_allItems.Count == 0)
                {
                    ModLog.Warn("Unlock All Items aborted: item list is empty.");
                    return;
                }

                var ui = UIManager._singleton;
                if (ui == null || ui._handComponentsList == null)
                {
                    ModLog.Warn("Unlock All Items aborted: hotbar is not ready.");
                    return;
                }

                int countToAssign = Math.Min(10, _allItems.Count);
                var indices = Enumerable.Range(0, _allItems.Count).ToList();

                for (int i = indices.Count - 1; i > 0; i--)
                {
                    int swapIndex = _rng.Next(i + 1);
                    int temp = indices[i];
                    indices[i] = indices[swapIndex];
                    indices[swapIndex] = temp;
                }

                int assigned = 0;
                for (int slot = 0; slot < countToAssign; slot++)
                {
                    var entry = _allItems[indices[slot]];
                    if (entry.Component == null)
                    {
                        continue;
                    }

                    ui._handComponentsList.SetItemAtIndex(slot, entry.Component);
                    assigned++;
                }

                if (assigned > 0)
                {
                    string previewName = _allItems[indices[0]].Name;
                    ui._handComponentsList.SetHandComponentText(previewName);
                    UIManager.PlaySoundUIClick();
                }

                ModLog.Info($"Unlock All Items completed: assigned {assigned} components to slots 1-10.");
            }
            catch (Exception ex)
            {
                ModLog.Error("Unlock All Items failed: " + ex);
            }
        }

        private void SyncMaterialsState(bool force)
        {
            _nextMaterialsSyncAt = UnityEngine.Time.realtimeSinceStartupAsDouble + (force ? 0.15 : 0.75);

            try
            {
                var core = Core.Get();
                if (core == null || core._componentsMap == null || core._componentsMap.Count == 0)
                {
                    return;
                }

                if (InfiniteAllMaterials)
                {
                    foreach (var component in core._componentsMap.Values)
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        if (!_originalAvailableAmounts.ContainsKey(component))
                        {
                            _originalAvailableAmounts[component] = component._availableAmount;
                        }

                        if (component._availableAmount < 999)
                        {
                            component._availableAmount = 999;
                        }
                    }

                    core.RefreshSharedAvailableComponents();
                    core.RefreshPrivateAvailableComponents();
                    return;
                }

                if (_originalAvailableAmounts.Count == 0)
                {
                    return;
                }

                foreach (var kv in _originalAvailableAmounts)
                {
                    if (kv.Key != null)
                    {
                        kv.Key._availableAmount = kv.Value;
                    }
                }

                _originalAvailableAmounts.Clear();
                core.RefreshSharedAvailableComponents();
                core.RefreshPrivateAvailableComponents();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Sync materials failed: " + ex.Message);
            }
        }
    }
}
