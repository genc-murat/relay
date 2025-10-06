namespace Relay.CLI.Commands.Models;

public class RequestInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public bool IsRecord { get; set; }
    public bool HasResponse { get; set; }
    public bool HasValidation { get; set; }
    public int ParameterCount { get; set; }
    public bool HasCaching { get; set; }
    public bool HasAuthorization { get; set; }
}


