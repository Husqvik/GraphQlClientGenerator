using GraphQlClientGenerator.Console;
using System.CommandLine;

var commandLineConfiguration = new CommandLineConfiguration(Commands.GenerateCommand);
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) =>
{
    commandLineConfiguration.Output.WriteLine("Control + C pressed");
    cancellationTokenSource.Cancel();
    args.Cancel = true;
};

return await commandLineConfiguration.InvokeAsync(args, cancellationTokenSource.Token);