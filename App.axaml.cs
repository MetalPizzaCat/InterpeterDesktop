//#define DO_PRE_GENERATION
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;


namespace Nema
{
    public partial class App : Application
    {
        public override void Initialize()
        {

            AvaloniaXamlLoader.Load(this);
#if DO_PRE_GENERATION
            Interpreter.ProcessorCommandsInfo info = new Interpreter.ProcessorCommandsInfo();
            info.Commands["mvi"] = new Interpreter.CommandInfo()
            {
                Arguments = new System.Collections.Generic.List<Interpreter.CommandArgumentType>()
                {
                    Interpreter.CommandArgumentType.RegisterName,
                    Interpreter.CommandArgumentType.Int8
                }
            };
            if (!System.IO.Directory.Exists("./Configuration"))
            {
                System.IO.Directory.CreateDirectory("./Configuration");
            }
            System.IO.File.WriteAllText("./Configuration/CommandInfo.json", Newtonsoft.Json.JsonConvert.SerializeObject(info));
#endif
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}