using CommandLine;

public class Options
{
    [Option('u', "url", HelpText = "Dataverse API URL.")]
    public string Url { get; set; }

    [Option('t', "token", HelpText = "Authentication token.")]
    public string Token { get; set; }
}