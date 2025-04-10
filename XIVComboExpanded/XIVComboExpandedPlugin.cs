using System;

using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVComboExpanded.Interface;
using XIVComboExpandedPlugin.Attributes;
using XIVComboExpandedPlugin.Interface;

namespace XIVComboExpandedPlugin;

/// <summary>
/// Main plugin implementation.
/// </summary>
public sealed class XIVComboExpandedPlugin : IDalamudPlugin
{
    private const string Command = "/pcombo";

    public readonly WindowSystem windowSystem;
    public readonly ConfigWindow configWindow;
    public readonly OneTimeModal oneTimeModal;

    /// <summary>
    /// Initializes a new instance of the <see cref="XIVComboExpandedPlugin"/> class.
    /// </summary>
    /// <param name="pluginInterface">Dalamud plugin interface.</param>
    /// <param name="sigScanner">Dalamud signature scanner.</param>
    /// <param name="gameInteropProvider">Dalamud game interop provider.</param>
    public XIVComboExpandedPlugin(
        IDalamudPluginInterface pluginInterface,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider)
    {
        pluginInterface.Create<Service>();

        Service.Configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Service.Address = new PluginAddressResolver();
        Service.Address.Setup((SigScanner)sigScanner);

        Service.ComboCache = new CustomComboCache();
        Service.IconReplacer = new IconReplacer(gameInteropProvider);

        this.configWindow = new(this);
        this.oneTimeModal = new(this);
        this.windowSystem = new("XIVComboExpanded");
        this.windowSystem.AddWindow(this.configWindow);
        this.windowSystem.AddWindow(this.oneTimeModal);

        Service.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;

        if (Service.Configuration.OneTimePopUp) this.oneTimeModal.IsOpen = true;

        Service.CommandManager.AddHandler(Command, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the XIV Combo Expanded main interface.",
            ShowInHelp = true,
        });
    }

    public string Name => "XIV Combo Expanded";

    /// <inheritdoc/>
    public void Dispose()
    {
        Service.CommandManager.RemoveHandler(Command);

        Service.Interface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;

        Service.IconReplacer?.Dispose();
        Service.ComboCache?.Dispose();
    }


    public void OnOpenConfigUi()
    {
        if (Service.Configuration.AutoJobChange)
        {
            string job = Service.ClientState.LocalPlayer?.ClassJob.RowId != null ? CustomComboInfoAttribute.JobIDToName((byte)Service.ClientState.LocalPlayer?.ClassJob.RowId) : Service.Configuration.CurrentJobTab;
            if (job == "Disciples of the Hand" || Service.Configuration.CurrentJobTab == "Disciples of the Hand")
                job = "Adventurer";
            Service.Configuration.CurrentJobTab = job;
        }

        this.configWindow.Toggle();

    }

    private void OnCommand(string command, string arguments)
    {
        var argumentsParts = arguments.Split();

        switch (argumentsParts[0])
        {
            case "setall":
                {
                    foreach (var preset in Enum.GetValues<CustomComboPreset>())
                    {
                        Service.Configuration.EnabledActions.Add(preset);
                    }

                    Service.ChatGui.Print("All SET");
                    Service.Configuration.Save();
                    break;
                }

            case "unsetall":
                {
                    foreach (var preset in Enum.GetValues<CustomComboPreset>())
                    {
                        Service.Configuration.EnabledActions.Remove(preset);
                    }

                    Service.ChatGui.Print("All UNSET");
                    Service.Configuration.Save();
                    break;
                }

            case "set":
                {
                    var targetPreset = argumentsParts[1].ToLowerInvariant();
                    foreach (var preset in Enum.GetValues<CustomComboPreset>())
                    {
                        if (preset.ToString().ToLowerInvariant() != targetPreset)
                            continue;

                        Service.Configuration.EnabledActions.Add(preset);
                        Service.ChatGui.Print($"{preset} SET");
                    }

                    Service.Configuration.Save();
                    break;
                }

            case "secrets":
                {
                    Service.Configuration.UnlockSecretCombos = !Service.Configuration.UnlockSecretCombos;

                    Service.ChatGui.Print(Service.Configuration.UnlockSecretCombos
                        ? $"Secret combos are now unlocked."
                        : $"Secret combos are now locked away.");

                    Service.Configuration.Save();
                    break;
                }

            case "toggle":
                {
                    var targetPreset = argumentsParts[1].ToLowerInvariant();
                    foreach (var preset in Enum.GetValues<CustomComboPreset>())
                    {
                        if (preset.ToString().ToLowerInvariant() != targetPreset)
                            continue;

                        if (Service.Configuration.EnabledActions.Contains(preset))
                        {
                            Service.Configuration.EnabledActions.Remove(preset);
                            Service.ChatGui.Print($"{preset} UNSET");
                        }
                        else
                        {
                            Service.Configuration.EnabledActions.Add(preset);
                            Service.ChatGui.Print($"{preset} SET");
                        }
                    }

                    Service.Configuration.Save();
                    break;
                }

            case "unset":
                {
                    var targetPreset = argumentsParts[1].ToLowerInvariant();
                    foreach (var preset in Enum.GetValues<CustomComboPreset>())
                    {
                        if (preset.ToString().ToLowerInvariant() != targetPreset)
                            continue;

                        Service.Configuration.EnabledActions.Remove(preset);
                        Service.ChatGui.Print($"{preset} UNSET");
                    }

                    Service.Configuration.Save();
                    break;
                }

            default:

                if (Service.Configuration.AutoJobChange)
                {
                    string job = Service.ClientState.LocalPlayer?.ClassJob.RowId != null ? CustomComboInfoAttribute.JobIDToName((byte)Service.ClientState.LocalPlayer?.ClassJob.RowId) : Service.Configuration.CurrentJobTab;
                    if (job == "Disciples of the Hand" || Service.Configuration.CurrentJobTab == "Disciples of the Hand")
                        job = "Adventurer";
                    Service.Configuration.CurrentJobTab = job;
                }

                this.configWindow.Toggle();
                break;
        }

        Service.Configuration.Save();
    }
}
