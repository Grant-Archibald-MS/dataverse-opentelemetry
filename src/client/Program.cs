using CommandLine;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;


Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => Run(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));


void Run(Options opts)
{
    var url = opts.Url;
    if (string.IsNullOrEmpty(url))
    {
        Console.WriteLine("What is Url of your dataverse?");
        url = Console.ReadLine();
    }

    var token = opts.Token;
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine($"What is your OAUTH access token for {url}?");
        token = Console.ReadLine();
    }

    var api = new Uri(url);

    var serviceClient = new ServiceClient(api, (url) => Task.FromResult(token));

    if (serviceClient.IsReady)
    {
        // Create an account entity
        Entity account = new Entity("account");
        account["name"] = "New Account Name";
        account["telephone1"] = "123-456-7890";
        account["emailaddress1"] = "email@example.com";

        // Create an OrganizationRequest for creating the account
        OrganizationRequest createRequest = new OrganizationRequest("Create")
        {
            ["Target"] = account
        };

        // Add the tag query string argument to the request
        createRequest["tag"] = Guid.NewGuid();

        // Execute the request
        OrganizationResponse response = serviceClient.Execute(createRequest);

        // Get the created account ID
        Guid accountId = (Guid)response.Results["id"];
        Console.WriteLine($"Account created with ID: {accountId}");
    }
    else
    {
        Console.WriteLine("Failed to connect to Dataverse.");
    }
}

void HandleParseError(IEnumerable<Error> errs)
{
    foreach (var err in errs) {
        Console.WriteLine(err);
    }    
}
