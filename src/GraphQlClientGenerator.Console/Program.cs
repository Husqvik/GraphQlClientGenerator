using GraphQlClientGenerator.Console;
using System.CommandLine;

var commandLineConfiguration = new CommandLineConfiguration(Commands.GenerateCommand);
return await commandLineConfiguration.InvokeAsync(args);