using System;
using System.Numerics;
using ImGuiNET;

using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ClickLib.Clicks;

namespace MatoyasBroom.Windows;

public unsafe class MateriaWindow : Window, IDisposable
{
    enum ExtractionState { None, Confirmation, Loading }

    private Plugin Plugin;

    public static bool IsMateriaMenuOpen() => Plugin.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero;
    public static bool IsMateriaMenuDialogOpen() => Plugin.GameGui.GetAddonByName("MaterializeDialog", 1) != IntPtr.Zero;

    private static int MaxLoadingFrames = 30;
    private ExtractionState extractionState = ExtractionState.None;
    private int loadingFrames = 0;
    private bool extracting = false;

    public MateriaWindow(Plugin plugin) : base(
        "Materia Extraction Helper", 
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize)
    {
        ShowCloseButton = false;
        Position = new Vector2(700, 240);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(90, 40),
            MaximumSize = new Vector2(90, 40)
        };

        Plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (!IsMateriaMenuOpen())
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 14);
        }
    }

    public override void Update()
    {
        if (extracting)
        {
            Extract();
        }
    }

    public override void Draw()
    {
        if (!IsMateriaMenuOpen())
        {
            IsOpen = false;
            extracting = false;
        }

        ImGui.PushStyleColor(ImGuiCol.Button, Plugin.red);
        if (ImGui.Button(extracting ? "Processing" : "Extract All!") && !extracting)
        {
            extracting = true;
        }
        ImGui.PopStyleColor();
    }

    private void Extract()
    {
        if (Plugin.PlayerOccupied())
            return;

        if (IsMateriaMenuDialogOpen())
        {
            // Dialog state
            extractionState = ExtractionState.Confirmation;
            try
            {
                var materializePTR = Plugin.GameGui.GetAddonByName("MaterializeDialog", 1);
                if (materializePTR == IntPtr.Zero)
                    return;

                ClickMaterializeDialog.Using(materializePTR).Materialize();
            }
            catch { }
        }

        switch (extractionState)
        {
            case ExtractionState.None:
                var materializePTR = Plugin.GameGui.GetAddonByName("Materialize", 1);
                if (materializePTR == IntPtr.Zero)
                    return;

                var materalizeWindow = (AtkUnitBase*)materializePTR;
                if (materalizeWindow == null)
                    return;

                var values = stackalloc AtkValue[2];
                values[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 2,
                };
                values[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 0,
                };

                materalizeWindow->FireCallback(1, values);
                extractionState = ExtractionState.None;
                break;
            case ExtractionState.Confirmation:
                // Just finished confirmation, throttle and wait for list to populate
                loadingFrames = 0;
                extractionState = ExtractionState.Loading;
                break;
            case ExtractionState.Loading:
                // Wait for the list to repopulate
                loadingFrames += 1;
                if (loadingFrames > MaxLoadingFrames)
                {
                    extractionState = ExtractionState.None;
                    loadingFrames = 0;
                }
                break;
            default:
                break;
        }
    }
}
