using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

using RotationSolver.UI.SearchableConfigs;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

public partial class RotationConfigWindow
{
    private string _searchText = string.Empty;
    private ISearchable[] _searchResults = [];

    internal static SearchableCollection _allSearchable = new();

    private void SearchingBox()
    {
        if (ImGui.InputTextWithHint("##Rotation Solver Reborn Search Box", UiString.ConfigWindow_Searching.GetDescription(), ref _searchText, 128, ImGuiInputTextFlags.AutoSelectAll))
        {
            _searchResults = _allSearchable.SearchItems(_searchText);
        }
    }

    #region Basic
    private static void DrawBasic()
    {
        _baseHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _baseHeader = new(new Dictionary<Func<string>, Action>
    {
        { UiString.ConfigWindow_Basic_Timer.GetDescription, DrawBasicTimer },
        { UiString.ConfigWindow_Basic_Others.GetDescription, DrawBasicOthers },
    });

    private static void DrawBasicTimer()
    {
        _allSearchable.DrawItems(Configs.BasicTimer);
    }

    private static readonly Dictionary<int, bool> _isOpen = [];

    private static void DrawBasicOthers()
    {
        _allSearchable.DrawItems(Configs.BasicParams);
    }
    #endregion

    #region UI
    private static void DrawUI()
    {
        _UIHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _UIHeader = new(new Dictionary<Func<string>, Action>
    {
        {
            UiString.ConfigWindow_UI_Information.GetDescription,
            () => _allSearchable.DrawItems(Configs.UiInformation)
        },
        {
            UiString.ConfigWindow_UI_Windows.GetDescription,
            () => _allSearchable.DrawItems(Configs.UiWindows)
        },
    });

    #endregion

    #region Auto
    private const int HeaderSize = 18;

    /// <summary>
    /// Draws the auto section of the configuration window.
    /// </summary>
    private void DrawAuto()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Auto_Description.GetDescription());
        _autoHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _autoHeader = new(new Dictionary<Func<string>, Action>
    {
        { UiString.ConfigWindow_Basic_AutoSwitch.GetDescription, DrawBasicAutoSwitch },
        { UiString.ConfigWindow_Auto_ActionUsage.GetDescription, DrawActionUsageControl },
        { UiString.ConfigWindow_Auto_HealingCondition.GetDescription, DrawHealingActionCondition },
    })
    {
        HeaderSize = HeaderSize,
    };

    private static void DrawBasicAutoSwitch()
    {
        _allSearchable.DrawItems(Configs.BasicAutoSwitch);
    }

    /// <summary>
    /// Draws the Action Usage and Control section.
    /// </summary>
    private static void DrawActionUsageControl()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Auto_ActionUsage_Description.GetDescription());
        ImGui.Separator();
        _allSearchable.DrawItems(Configs.AutoActionUsage);
    }

    /// <summary>
    /// Draws the healing action condition section.
    /// </summary>
    private static void DrawHealingActionCondition()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Auto_HealingCondition_Description.GetDescription());
        ImGui.Separator();
        _allSearchable.DrawItems(Configs.HealingActionCondition);
    }
    #endregion

    #region Target
    private static void DrawTarget()
    {
        _targetHeader?.Draw();
    }

    /// <summary>
    /// Header group for target-related configurations.
    /// </summary>
    private static readonly CollapsingHeaderGroup _targetHeader = new(new Dictionary<Func<string>, Action>
    {
    { UiString.ConfigWindow_Target_Config.GetDescription, DrawTargetConfig },
    { UiString.ConfigWindow_List_Hostile.GetDescription, DrawTargetHostile },
    });

    /// <summary>
    /// Draws the target configuration items.
    /// </summary>
    private static void DrawTargetConfig()
    {
        _allSearchable.DrawItems(Configs.TargetConfig);
    }

    private static void DrawTargetHostile()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add Hostile"))
        {
            Service.Config.TargetingTypes.Add(TargetingType.Big);
        }
        ImGui.SameLine();
        ImGui.TextWrapped(UiString.ConfigWindow_Param_HostileDesc.GetDescription());

        for (int i = 0; i < Service.Config.TargetingTypes.Count; i++)
        {
            TargetingType targetType = Service.Config.TargetingTypes[i];
            string key = $"TargetingTypePopup_{i}";

            void Delete()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
            }

            void Up()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
                Service.Config.TargetingTypes.Insert(Math.Max(0, i - 1), targetType);
            }

            void Down()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
                Service.Config.TargetingTypes.Insert(Math.Min(Service.Config.TargetingTypes.Count - 1, i + 1), targetType);
            }

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                (UiString.ConfigWindow_List_Remove.GetDescription(), Delete, pairsArray2),
                (UiString.ConfigWindow_Actions_MoveUp.GetDescription(), Up, pairsArray0),
                (UiString.ConfigWindow_Actions_MoveDown.GetDescription(), Down, pairsArray1));

            string[] names = Enum.GetNames<TargetingType>();
            int targetingType = (int)Service.Config.TargetingTypes[i];
            string text = UiString.ConfigWindow_Param_HostileCondition.GetDescription();
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(text).X + (30 * Scale));
            if (ImGui.Combo(text + "##HostileCondition" + i, ref targetingType, names, names.Length))
            {
                Service.Config.TargetingTypes[i] = (TargetingType)targetingType;
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
                (Delete, new[] { VirtualKey.DELETE }),
                (Up, new[] { VirtualKey.UP }),
                (Down, new[] { VirtualKey.DOWN }));
        }
    }
    #endregion

    #region Stacks
    private void DrawStacks()
    {
        if (ImGui.BeginTabBar("StacksTabBar"))
        {
            if (ImGui.BeginTabItem("Hostile Priority"))
            {
                DrawHostilePriority();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawHostilePriority()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add Priority Target"))
        {
            Service.Config.PriorityTargets.Add(new PriorityTargetConfig());
        }
        ImGui.SameLine();
        ImGui.TextWrapped("Add targets to prioritize.");

        ImGui.Separator();
        DrawHostileList();
    }

    private void DrawHostileList()
    {
        // Header
        ImGui.Text("On");
        ImGui.SameLine();
        ImGui.Text("Prio");
        ImGui.SameLine();
        ImGui.Text("Target Name (Partial Match)");

        for (int i = 0; i < Service.Config.PriorityTargets.Count; i++)
        {
            var item = Service.Config.PriorityTargets[i];
            string key = $"PriorityTarget_{i}";

            ImGui.PushID(key);

            // Enabled toggle
            bool enabled = item.Enabled;
            if (ImGui.Checkbox("##Enabled", ref enabled))
            {
                item.Enabled = enabled;
            }
            ImGui.SameLine();

            // Move Up/Down Buttons
            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, "Up") && i > 0)
            {
                (Service.Config.PriorityTargets[i], Service.Config.PriorityTargets[i - 1]) = (Service.Config.PriorityTargets[i - 1], Service.Config.PriorityTargets[i]);
            }
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, "Down") && i < Service.Config.PriorityTargets.Count - 1)
            {
                (Service.Config.PriorityTargets[i], Service.Config.PriorityTargets[i + 1]) = (Service.Config.PriorityTargets[i + 1], Service.Config.PriorityTargets[i]);
            }
            ImGui.SameLine();

            // Priority
            int priority = item.Priority;
            ImGui.SetNextItemWidth(60 * Scale);
            if (ImGui.InputInt("##Priority", ref priority, 0))
            {
                item.Priority = priority;
            }
            ImGui.SameLine();

            // Name
            string name = item.Name;
            ImGui.SetNextItemWidth(150 * Scale);
            if (ImGui.InputTextWithHint("##Name", "Name (0 for ID)", ref name, 64))
            {
                item.Name = name;
            }
            ImGui.SameLine();

            // Object ID
            if (string.IsNullOrEmpty(name))
            {
                int objId = (int)item.ObjectId;
                ImGui.SetNextItemWidth(80 * Scale);
                if (ImGui.InputInt("##ObjID", ref objId, 0))
                {
                    item.ObjectId = (uint)objId;
                }
                ImGui.SameLine();
            }
            else
            {
                // Full Match Toggle
                bool full = item.FullMatch;
                if (ImGui.Checkbox("Full Match", ref full))
                {
                    item.FullMatch = full;
                }
                ImGui.SameLine();
            }

            // Delete
            if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, "Delete"))
            {
                Service.Config.PriorityTargets.RemoveAt(i);
                i--;
            }

            ImGui.PopID();
        }
    }
    #endregion

    #region Extra
    private static void DrawExtra()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Extra_Description.GetDescription());
        _extraHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _extraHeader = new(new Dictionary<Func<string>, Action>
    {
    { UiString.ConfigWindow_EventItem.GetDescription, DrawEventTab },
    { UiString.ConfigWindow_Internal.GetDescription, DrawInternalTab },
    {
        UiString.ConfigWindow_Extra_Others.GetDescription,
        () => _allSearchable.DrawItems(Configs.Extra)
    },
    });
    private static readonly string[] pairsArray0 = ["↑"];
    private static readonly string[] pairsArray1 = ["↓"];
    private static readonly string[] pairsArray2 = ["Delete"];

    private static void DrawInternalTab()
    {
        ImGui.Text($"Configs/Backups location: {Svc.PluginInterface.ConfigFile.Directory}");

        if (ImGui.Button("Backup Configs"))
        {
            Service.Config.Backup();
        }

        if (ImGui.Button("Restore Configs"))
        {
            Service.Config.Restore();
        }
    }

    private static void DrawEventTab()
    {
        if (ImGui.Button(UiString.ConfigWindow_Events_AddEvent.GetDescription()))
        {
            Service.Config.Events.Add(new ActionEventInfo());
        }
        ImGui.SameLine();

        ImGui.TextWrapped(UiString.ConfigWindow_Events_Description.GetDescription());

        ImGui.Text(UiString.ConfigWindow_Events_DutyStart.GetDescription());
        ImGui.SameLine();
        Service.Config.DutyStart.DisplayMacro();

        ImGui.Text(UiString.ConfigWindow_Events_DutyEnd.GetDescription());
        ImGui.SameLine();
        Service.Config.DutyEnd.DisplayMacro();

        ImGui.Separator();

        for (int i = 0; i < Service.Config.Events.Count; i++)
        {
            ActionEventInfo eve = Service.Config.Events[i];
            eve.DisplayEvent();

            ImGui.SameLine();

            if (ImGui.Button($"{UiString.ConfigWindow_Events_RemoveEvent.GetDescription()}##RemoveEvent{eve.GetHashCode()}"))
            {
                Service.Config.Events.RemoveAt(i);
                i--; // Adjust index after removal
            }
            ImGui.Separator();
        }
    }
    #endregion
}