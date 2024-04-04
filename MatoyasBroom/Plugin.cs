using System.IO;

using Dalamud.Interface.Windowing;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using MatoyasBroom.Windows;
using System.Numerics;

namespace MatoyasBroom
{
    public sealed unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "ListMaster";
        private const string MainCommand = "/plm";
        public Vector4 green = new(0.4f, 1.0f, 0.4f, 1.0f);
        public Vector4 red = new(1.0f, 0.4f, 0.4f, 1.0f);

        [PluginService] private DalamudPluginInterface PluginInterface { get; init; }
        [PluginService] private ICommandManager CommandManager { get; init; }
        [PluginService] public static IFramework Framework { get; private set; }
        [PluginService] public static ICondition Condition { get; private set; }
        [PluginService] public static IDataManager Data { get; private set; }
        [PluginService] public static IGameGui GameGui { get; private set; }
        [PluginService] public static IChatGui Chat { get; private set; }
        public Configuration Configuration { get; init; }

        // Windows
        public WindowSystem WindowSystem = new("ListMaster");
        private ConfigWindow ConfigWindow { get; init; }
        private DesynthWindow DesynthWindow { get; init; }
        private MateriaWindow MateriaWindow { get; init; }

        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

            ConfigWindow = new ConfigWindow(this);
            DesynthWindow = new DesynthWindow(this);
            MateriaWindow = new MateriaWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(DesynthWindow);
            WindowSystem.AddWindow(MateriaWindow);

            CommandManager.AddHandler(MainCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open config or something"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += OnFrameworkUpdate;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            DesynthWindow.Dispose();
            MateriaWindow.Dispose();

            Framework.Update -= OnFrameworkUpdate;

            CommandManager.RemoveHandler(MainCommand);
        }

        private unsafe void OnCommand(string command, string args)
        {
            switch (command)
            {
                case MainCommand:
                    DrawConfigUI();
                    break;
                default:
                    break;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (DesynthWindow.IsDesynthMenuOpen())
            {
                DesynthWindow.IsOpen = true;
            }
            if (MateriaWindow.IsMateriaMenuOpen())
            {
                MateriaWindow.IsOpen = true;
            }
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
        public static bool PlayerOccupied()
        {
            return Condition[ConditionFlag.Occupied]
               || Condition[ConditionFlag.Occupied30]
               || Condition[ConditionFlag.Occupied33]
               || Condition[ConditionFlag.Occupied38]
               || Condition[ConditionFlag.Occupied39]
               || Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Condition[ConditionFlag.OccupiedInEvent]
               || Condition[ConditionFlag.OccupiedInQuestEvent]
               || Condition[ConditionFlag.OccupiedSummoningBell];
        }
    }
}
