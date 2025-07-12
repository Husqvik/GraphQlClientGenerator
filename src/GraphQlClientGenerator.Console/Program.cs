using GraphQlClientGenerator.Console;
using System.CommandLine;

var generateCommand = new CommandLineConfiguration(Commands.GenerateCommand());
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress +=
    (_, args) =>
    {
        generateCommand.Output.WriteLine("Control + C pressed");
        cancellationTokenSource.Cancel();
        args.Cancel = true;
    };

return await generateCommand.InvokeAsync(args, cancellationTokenSource.Token);