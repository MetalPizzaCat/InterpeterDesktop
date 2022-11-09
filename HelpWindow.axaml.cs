using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using Emulator;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Nema
{
    public class HelpCommandInfo
    {
        private string _name;

        private string _arguments;

        /// <summary>
        /// User written note that explains what this command does
        /// </summary>
        private string _note;

        public string Name => _name;
        public string Arguments => _arguments;
        public string Note => _note;

        public HelpCommandInfo(string name, string arguments, string note)
        {
            _name = name;
            _arguments = arguments;
            _note = note;
        }
    }

    public partial class HelpWindow : Window
    {
        private ObservableCollection<HelpCommandInfo> _commands = new ObservableCollection<HelpCommandInfo>();
        public ObservableCollection<HelpCommandInfo> Commands => _commands;
        public HelpWindow()
        {
            InitializeComponent();


            string infoText = System.IO.File.ReadAllText("./Configuration/CommandInfo.json");
            ProcessorCommandsInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessorCommandsInfo>(infoText) ?? throw new NullReferenceException("Unable to process configuration");
            foreach (var command in info.Commands)
            {
                string arguments = string.Empty;
                foreach (CommandArgumentType arg in command.Value.Arguments)
                {
                    switch (arg)
                    {
                        case CommandArgumentType.RegisterName:
                            arguments += "A-L";
                            break;
                        case CommandArgumentType.Int8:
                            arguments += "D8";
                            break;
                        case CommandArgumentType.Int16:
                            arguments += "D16";
                            break;
                        case CommandArgumentType.LabelName:
                            arguments += "Label";
                            break;
                    }
                    arguments += ",";
                }
                arguments = arguments.TrimEnd(',');
                Commands.Add(new HelpCommandInfo(command.Key, arguments, command.Value.Note ?? string.Empty));
            }
            this.DataContext = this;
        }
    }
}