using System;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ClickLib.Clicks;


namespace MatoyasBroom.Windows;

public unsafe class DesynthWindow : Window, IDisposable
{
    private Plugin Plugin;

    private static readonly int MaxLoadingFrames = 20;
    private int loadingFrames = 0;
    private AgentSalvage* agent;
    private bool desynthesizing = false;
    private bool[]? selections = null;
    private bool[]? enoughDesynthLevel = null;
    private bool[]? inGearSet = null;
    private bool excludeGearSetItems = true;
    private AgentSalvage.SalvageItemCategory currentCategory;


    public static bool IsDesynthMenuOpen() => Plugin.GameGui.GetAddonByName("SalvageItemSelector", 1) != IntPtr.Zero;
    public static bool IsDesynthDialogOpen() => Plugin.GameGui.GetAddonByName("SalvageDialog", 1) != IntPtr.Zero;
    private bool IsDataInitialized() => selections != null && enoughDesynthLevel != null && inGearSet != null;
    private bool NeedLoading() => !IsDataInitialized() || currentCategory != agent->SelectedCategory;

    public DesynthWindow(Plugin plugin) : base("Desynthesis Helper", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        ShowCloseButton = false;
        Position = new Vector2(1270, 280);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 430),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose()
    { 
    }

    public void Reset()
    {
        selections = null;
        enoughDesynthLevel = null;
        inGearSet = null;
        desynthesizing = false;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (!IsDesynthMenuOpen())
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 5);
        }
        Reset();
    }

    public override void Update()
    {
        if (!IsDesynthMenuOpen())
        {
            IsOpen = false;
            Reset();
        }

        agent = AgentSalvage.Instance();

        if (agent != null && loadingFrames >= MaxLoadingFrames)
        {
            selections = new bool[agent->ItemCount];
            enoughDesynthLevel = new bool[agent->ItemCount];
            inGearSet = new bool[agent->ItemCount];

            currentCategory = agent->SelectedCategory;
            loadingFrames = 0;
        }

        if (IsDataInitialized())
        {
            if (excludeGearSetItems)
            {
                for (var i = 0; i < selections.Length; i++)
                {
                    if (inGearSet[i])
                        selections[i] = false;
                }
            }
        }

        if (desynthesizing)
        {
            Desynthesize();
        }
    }

    public override void Draw()
    {
        ImGui.TextColored(Plugin.red, "This plugin is still in active development, it may accidentally desynthesize\nyour item. Use at your own risk! Please do NOT circulate!");
        DrawButtons();

        // Check if need to reinitialize selections
        if (loadingFrames < MaxLoadingFrames && (!IsDataInitialized() || currentCategory != agent->SelectedCategory))
        {
            ImGui.Text("Loading...");
            loadingFrames += 1;
        }
        else if (desynthesizing)
        {
            ImGui.Text("Processing...");
        }
        else
        {
            DrawTable();
        }
    }

    public void DrawButtons()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Plugin.red);
        if (ImGui.Button(desynthesizing ? "Processing" : "Desynthesize!") && !desynthesizing)
        {
            desynthesizing = true;
        }
        ImGui.PopStyleColor();

        ImGui.SameLine();
        if (ImGui.Button("Select all") && !desynthesizing && IsDataInitialized())
        {
            for (var i = 0; i < selections.Length; i++)
            {
                selections[i] = !excludeGearSetItems || !inGearSet[i];
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Unselect all") && !desynthesizing && IsDataInitialized())
        {
            for (var i = 0; i < selections.Length; i++)
            {
                selections[i] = false;
            }
        }
        ImGui.SameLine(ImGui.GetWindowWidth() - 170);
        ImGui.Checkbox("Exclude gear set items", ref excludeGearSetItems);

        ImGui.PushID(999);
        ImGui.Text("Items with enough desynthesis levels: ");
        ImGui.SameLine();
        if (ImGui.Button("Select all") && !desynthesizing && IsDataInitialized())
        {
            for (var i = 0; i < selections.Length; i++)
            {
                selections[i] = enoughDesynthLevel[i] && !(excludeGearSetItems && inGearSet[i]);
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            ImGui.SetTooltip("Desynthesizing these items will have an increased\nchance of receiving rare items.");
        }
        ImGui.SameLine();
        if (ImGui.Button("Select opposite") && !desynthesizing && IsDataInitialized())
        {
            for (var i = 0; i < selections.Length; i++)
            {
                selections[i] = !enoughDesynthLevel[i] && !(excludeGearSetItems && inGearSet[i]);
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            ImGui.SetTooltip("Desynthesizing these items will increase the\ndesynthesis skill of the corresponding job.");
        }
    }

    public void DrawTable()
    {
        if (ImGui.BeginTable("", 5, ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("  #  ", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
            ImGui.TableSetupColumn("Skill", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
            ImGui.TableSetupColumn("Gearset", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
            ImGui.TableHeadersRow();
            if (IsDataInitialized())
            {
                for (var i = 0; i < agent->ItemCount; ++i)
                {
                    var item = agent->ItemList + i;

                    ImGui.PushID(i + 1);
                    ImGui.TableNextRow();

                    // Checkbox
                    ImGui.TableNextColumn();
                    ImGui.Checkbox("", ref selections[i]);

                    // Name
                    ImGui.TableNextColumn();
                    var name = System.Text.Encoding.UTF8.GetString(item->Name);
                    ImGui.Text($"{name.Substring(14, name.Length - 24)}");

                    // Quantity
                    ImGui.TableNextColumn();
                    ImGui.Text($"  {item->Quantity}  ");

                    try
                    {
                        // Desynth level
                        ImGui.TableNextColumn();
                        var inventoryItem = InventoryManager.Instance()->GetInventoryContainer(item->InventoryType)->GetInventorySlot((int)item->InventorySlot);
                        var itemData = Plugin.Data.GetExcelSheet<Item>()?.GetRow(inventoryItem->ItemID);
                        var desynthLevel = UIState.Instance()->PlayerState.GetDesynthesisLevel(item->ClassJob);
                        enoughDesynthLevel[i] = desynthLevel > itemData.LevelItem.Row;
                        // ImGui.Checkbox("", ref enoughDesynthLevel[i]);
                        if (enoughDesynthLevel[i])
                            ImGui.ColorButton("", Plugin.green, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker);
                        else
                            ImGui.ColorButton("", Plugin.red, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker);

                        // Gear set
                        ImGui.TableNextColumn();
                        inGearSet[i] = GetGearSetWithItem(inventoryItem) != null;
                        if (inGearSet[i])
                        {
                            ImGui.Checkbox("", ref inGearSet[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("?");
                        ImGui.TableNextColumn();
                        ImGui.Text("?");
                    }

                    ImGui.PopID();
                }
            }
            ImGui.EndTable();
        }
    }

    private static RaptureGearsetModule.GearsetEntry* GetGearSetWithItem(InventoryItem* slot)
    {
        var gearSetModule = RaptureGearsetModule.Instance();
        var itemIdWithHQ = slot->ItemID;
        if ((slot->Flags & InventoryItem.ItemFlags.HQ) > 0) itemIdWithHQ += 1000000;
        for (var gs = 0; gs < 101; gs++)
        {
            var gearSet = gearSetModule->GetGearset(gs);
            if (gearSet == null) continue;
            if (gearSet->ID != gs) break;
            if (!gearSet->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
            foreach (var i in gearSet->ItemsSpan)
            {
                if (i.ItemID == itemIdWithHQ)
                {
                    return gearSet;
                }
            }
        }

        return null;
    }


    private void Desynthesize()
    {
        if (Plugin.PlayerOccupied())
            return;

        if (IsDesynthDialogOpen())
        {
            var salvageDialogPtr = Plugin.GameGui.GetAddonByName("SalvageDialog", 1);
            if (salvageDialogPtr == IntPtr.Zero)
                return;

            ClickSalvageDialog.Using(salvageDialogPtr).Desynthesize();
        }
        else if (IsDataInitialized())
        {
            // Find the index of the next item to be desynthesized
            var index = -1;
            for (var i = 0; i < selections.Length; ++i)
            {
                if (selections[i])
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                // Done
                Reset();
            }

            var salvageItemSelectorPtr = Plugin.GameGui.GetAddonByName("SalvageItemSelector", 1);
            if (salvageItemSelectorPtr == IntPtr.Zero)
                return;

            var salvageItemSelector = (AtkUnitBase*)salvageItemSelectorPtr;
            if (salvageItemSelector == null)
                return;

            // var list = salvageItemSelector->UldManager.NodeList[3];

            var values = stackalloc AtkValue[2];
            values[0] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 12,               // Somehow 12 is the correct action
            };
            values[1] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                UInt = (uint)index,         // Index of the item to be desynthesized
            };

            salvageItemSelector->FireCallback(1, values);

            // Remove index from lists
            var selectionsList = new List<bool>(selections);
            selectionsList.RemoveAt(index);
            selections = selectionsList.ToArray();

            var enoughDesynthLevelList = new List<bool>(enoughDesynthLevel);
            enoughDesynthLevelList.RemoveAt(index);
            enoughDesynthLevel = enoughDesynthLevelList.ToArray();

            var inGearSetList = new List<bool>(inGearSet);
            inGearSetList.RemoveAt(index);
            inGearSet = inGearSetList.ToArray();
        }
        else if (!IsDataInitialized())
        {
            Reset();
        }
    }
}
