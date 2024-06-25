using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using System;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using FFXIVClientStructs.FFXIV.Client.UI;


namespace SamplePlugin;

public unsafe sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IFramework framework { get; set; } = null!;
    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    private  IChatGui chat { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    public readonly WindowSystem WindowSystem = new("Foodge");
    private static int timeElapsed = DateTime.Now.ToLocalTime().Minute;

    private bool updateFlag = false;
    //default value is 20
    private int durationOption = 20;
    private int durationSave;
    private uint reminderOption = 6;
    private float durationSeconds;

    private  PluginCommandManager<Plugin> comm;
    private IClientState clientState;
    private Character* cs; 
    private uint StatusFood { get; set; } = 48;

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] ITextureProvider textureProvider, 
        IClientState clientState,
        IChatGui chat)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        this.clientState = clientState;
        this.chat = chat;
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        this.reminderOption = (uint)Configuration.reminderOption;
        this.durationOption = Configuration.durationOption;
        this.durationSave = durationOption;
        this.ConfigWindow = new ConfigWindow(this);

        if(this.clientState.LocalPlayer is not null){
        cs = (Character*) this.clientState.LocalPlayer.Address;
        }
        WindowSystem.AddWindow(ConfigWindow);

        var world = this.clientState.LocalPlayer?.CurrentWorld.GameData;

        this.comm = new PluginCommandManager<Plugin>(this, commandManager);
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        framework.Update += this.OnFrameworkTick;
        
    }

    [Command("/foodge")]
    [HelpMessage("Foodge configuration")]
    public void FoodgeConfig(string command, string args)
    {   
            ToggleConfigUI();
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        framework.Update -= this.OnFrameworkTick;
    }


    private void OnFrameworkTick(IFramework ifw)
    {
            //TODO: RESET on refood OR if new set duration < old duration
            if (updateFlag.Equals(true) && !cs->GetStatusManager()->HasStatus(48)) {
                updateFlag = false;
            }

            if(timeElapsed != DateTime.Now.ToLocalTime().Minute) {
            if(durationSave != Configuration.durationOption) {
                updateFlag = false;
                durationSave = Configuration.durationOption;
            }
            //48 is the ID of the Well Fed status
            durationSeconds = GetStatusTimeRemaining(48);
            if (durationSeconds != 0.0f)
        {

            if((Math.Floor(durationSeconds/60) < durationOption) && (Math.Floor(durationSeconds/60) != 0) && updateFlag.Equals(false)) {
                chat.Print($"Food duration is at {Math.Floor(durationSeconds/60)} minutes. Consider extending the buff.");
                playReminder();
                updateFlag = true;
            }
            else if(Math.Floor(durationSeconds/60) < 20.0 && Math.Floor(durationSeconds/60) == 0 && updateFlag.Equals(false)){
                chat.Print($"Food duration is less than 1 minute. Consider extending the buff.");
                playReminder();
                updateFlag = true;
            }
            
        }
        timeElapsed = DateTime.Now.ToLocalTime().Minute;
        }
    }
    private void playReminder() {
        UIModule.PlayChatSoundEffect((uint)reminderOption);
    }

    private void OnCommand(string command, string args)
    {
        //empty
    }
    public float GetStatusTimeRemaining(uint statusId)
    {
        if (cs->GetStatusManager()->HasStatus(statusId))
        {
            var statusIndex = cs->GetStatusManager()->GetStatusIndex(statusId);
            return cs->GetStatusManager()->GetRemainingTime(statusIndex);
        }

        return 0.0f;
    }
    public void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
