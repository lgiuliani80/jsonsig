using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;

const string APPNAME = "jsonsig";

if (args.Length != 2) 
{
    Console.Error.WriteLine("Syntax:");
    Console.Error.WriteLine($"  - sign   =>  {APPNAME} sign <json-file>");
    Console.Error.WriteLine($"  - verify =>  {APPNAME} verify <json-file>");
    Environment.Exit(1);
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => {
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
        });
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();
var log = host.Services.GetRequiredService<ILogger<Program>>();

log.LogInformation($"STARTED");

var inputJson = File.ReadAllText(args[1]);
var cmd = args[0];

var jo = JsonNode.Parse(inputJson);

if (jo == null) {
    log.LogError("Invalid JSON");
    return;
}

switch (cmd)
{
    case "sign":
        jo["sig"] = "";
        var textToSign = jo.ToJsonString();
        var bytesToHash = Encoding.UTF8.GetBytes(textToSign);
        var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytesToHash);
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(config["PrivateKeyFile"]));
        var encrypted = rsa.Encrypt(hash, RSAEncryptionPadding.Pkcs1);

        var edf = ECDsa.Create();
        edf.ImportFromPem(File.ReadAllText(config["PrivateKeyFileDf"]));  // 160 bit key brainpoolP160r1 RFC 5639
        var encdh = edf.SignData(bytesToHash, 0, bytesToHash.Length, HashAlgorithmName.SHA256);

        jo["sig"] = Convert.ToBase64String(encdh);

        Console.WriteLine(jo.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        break;
}

log.LogInformation("END");
