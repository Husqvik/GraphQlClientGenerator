using System.CommandLine;
using GraphQlClientGenerator.Console;

var invocationConfiguration = new InvocationConfiguration();
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress +=
    (_, args) =>
    {
        invocationConfiguration.Output.WriteLine("Control + C pressed");
        cancellationTokenSource.Cancel();
        args.Cancel = true;
    };

return await Commands.GenerateCommand().Parse(args).InvokeAsync(invocationConfiguration, cancellationTokenSource.Token);