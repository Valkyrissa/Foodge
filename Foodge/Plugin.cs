using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Memory;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using System;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Statuses;
using System.Dynamic;
using Dalamud.Game.ClientState.Objects.Types;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

using FFXIVClientStructs.FFXIV.Client.UI;


namespace SamplePlugin;

public unsafe sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/pmycommand";
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
    private uint reminderOption = 6;

    private  PluginCommandManager<Plugin> comm;
    private IClientState clientState;
    private Character* character; 
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

        this.ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        var world = this.clientState.LocalPlayer?.CurrentWorld.GameData;

        // you might normally want to embed resources and load them from the manifest stream
        var file = new FileInfo(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "lizzer.png"));


        this.comm = new PluginCommandManager<Plugin>(this, commandManager);
         PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        framework.Update += this.OnFrameworkTick;
    }

    public unsafe void openFriendList(){
            FFXIVClientStructs.FFXIV.Client.UI.UIModule.Instance()->ExecuteMainCommand(13);
    }

    [Command("/stfu")]
    [HelpMessage("Sets game volume. Takes percent of volume as argument (default 50).")]
    public void ExampleCommand1(string command, string args)
    {   
        EnvironmentManager em = new EnvironmentManager();
        int val;
        if(Int32.TryParse(args, out val).Equals(false)){           
            val = 50;
        }
        this.chat.Print($"Changing base volumes to {val}");
        foreach(var soundChannel in Enum.GetValues<EnvironmentManager.SoundChannel>()){
        em.SetVolume(soundChannel, val, true);
        }
    }

    [Command("/foodge")]
    [HelpMessage("Message")]
    public void FoodDuration(string command, string args)
    {   
            playReminder();
            var cs = (Character*) this.clientState.LocalPlayer.Address;
            new CharaLib(cs);
            //48 is the ID of the Well Fed status.
          if (cs->GetStatusManager()->HasStatus(48))
        {
            var statusIndex = cs->GetStatusManager()->GetStatusIndex(StatusFood);
            var durationSeconds = cs->GetStatusManager()->GetRemainingTime(statusIndex);

            chat.Print($"Food duration left in minutes: {Math.Floor(durationSeconds/60)}");
            //Add option to customize duration.
            if(Math.Floor(durationSeconds/60) < 20.0 && Math.Floor(durationSeconds/60) != 0) {
                chat.Print($"Food duration is at {Math.Floor(durationSeconds/60)} minutes. Consider extending the buff.");
            }
            else if(Math.Floor(durationSeconds/60) < 20.0 && Math.Floor(durationSeconds/60) == 0){
                chat.Print($"Food duration is less than 1 minute. Consider extending the buff.");
            }
            
        }
        chat.Print($"{timeElapsed}");
    }

    [Command("/foodtimer")]
    [HelpMessage("Message")]
    public void SetDuration(string command, string args)
    {   
        //TODO: what if duration negative? How does TryParse evaluate args?
        int val;
        if(Int32.TryParse(args, out val).Equals(false)) {           
            val = 20;
        }
        else if(val > 30) {
            val = 30;
        }
        durationOption = val;
        chat.Print($"Duration set to {durationOption} minutes.");
    }

    [Command("/foodse")]
    [HelpMessage("Message")]
    public void SetFoodSoundEffect(string command, string args)
    {   
        int val;
        if(Int32.TryParse(args, out val).Equals(false)) {           
            chat.Print($"Value must be a number between 1 and 16.");
        }
        else if(val > 16 || val < 1) {
            chat.Print($"Value must be a number between 1 and 16.");
        }
        else {
            reminderOption = (uint)val;
            chat.Print($"Sound effect set to {reminderOption}.");
        }

    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        framework.Update -= this.OnFrameworkTick;
    }


    private void OnFrameworkTick(IFramework ifw)
    {
            //TODO: RESET on refood OR if new set duration < old duration
            if(timeElapsed != DateTime.Now.ToLocalTime().Minute) {
            //chat.Print($"{timeElapsed}");    
            var cs = (Character*) this.clientState.LocalPlayer.Address;
            new CharaLib(cs);
            var foodCheck = cs->GetStatusManager()->HasStatus(48);
            //48 is the ID of the Well Fed status.
            if (foodCheck.Equals(true))
        {
            var statusIndex = cs->GetStatusManager()->GetStatusIndex(StatusFood);
            var durationSeconds = cs->GetStatusManager()->GetRemainingTime(statusIndex);

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
            else if(updateFlag.Equals(true)) {
                updateFlag = false;
        }
        timeElapsed = DateTime.Now.ToLocalTime().Minute;
        }
    }
    private void playReminder() {
        UIModule.PlayChatSoundEffect((uint)reminderOption);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
    }
    public bool HasFood(uint statusId) => character->GetStatusManager()->HasStatus(statusId);
    public void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
