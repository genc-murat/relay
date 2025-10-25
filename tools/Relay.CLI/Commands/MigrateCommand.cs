using System.CommandLine;

namespace Relay.CLI.Commands;

/// <summary>
/// Migration command - Automated migration from MediatR to Relay
/// </summary>
public static class MigrateCommand
{
    public static Command Create() => MigrateCommandBuilder.Create();


}
