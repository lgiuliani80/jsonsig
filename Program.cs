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
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();

var inputJson = File.ReadAllText(args[1]);
var cmd = args[0];

var jo = JsonNode.Parse(inputJson);

if (jo == null) {
    Console.Error.WriteLine("Invalid JSON");
    return;
}

switch (cmd)
{
    case "sign":
    {
        jo["sig"] = "";
        var textToSign = jo.ToJsonString();
        var bytesToSign = Encoding.UTF8.GetBytes(textToSign);

        //Console.Error.WriteLine($"Text to sign: '{textToSign}'");

        var edf = ECDsa.Create();
        edf.ImportFromPem(File.ReadAllText(config["PrivateKeyFileDf"]));  // 160 bit key brainpoolP160r1 RFC 5639
        var encdh = edf.SignData(bytesToSign, 0, bytesToSign.Length, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        jo["sig"] = config["KeySpec"] + "." + Base64Url.Encode(encdh);

        Console.WriteLine(jo.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        break;
    }
    case "verify":
    {
        var sigParts = jo["sig"]?.ToString()?.Split('.');

        if (sigParts == null || sigParts.Length < 2) 
        {
            Console.Error.WriteLine("Missing signature");
            Environment.Exit(2);
        }

        if (sigParts[0] != config["KeySpec"])
        {
            Console.Error.WriteLine("Unknown key spec");
            Environment.Exit(3);
        }

        var curSig = sigParts[1];
        var curSigBytes = Base64Url.Decode(curSig);

        jo["sig"] = "";
        var textToSign = jo.ToJsonString();
        var bytesToSign = Encoding.UTF8.GetBytes(textToSign);

        var edf = ECDsa.Create();
        if (config["PrivateKeyFileDf"] == null || !File.Exists(config["PrivateKeyFileDf"])) 
        {
            edf.ImportFromPem(File.ReadAllText(config["PublicKeyFileDf"]));
        }
        else
        {
            edf.ImportFromPem(File.ReadAllText(config["PrivateKeyFileDf"]));  // 160 bit key brainpoolP160r1 RFC 5639
        }
        var result = edf.VerifyData(bytesToSign, curSigBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        Console.WriteLine($"Result = {result}");

        if (!result)
            Environment.Exit(1);

        break;
    }
}
