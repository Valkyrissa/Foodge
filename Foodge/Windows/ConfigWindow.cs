using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    
    public ConfigWindow(Plugin plugin) : base("Foodge Settings###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(640, 480);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
        
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        var reminderOption = Configuration.reminderOption;
        if (ImGui.InputInt("Set sound effect", ref reminderOption))
        {   
            Configuration.reminderOption = reminderOption;
            Configuration.Save();
        }

        var durationOption = Configuration.durationOption;
        if (ImGui.InputInt("Set threshold for reminder", ref durationOption))
        {   
            Configuration.durationOption = durationOption;
            Configuration.Save();
        }
    }
}

