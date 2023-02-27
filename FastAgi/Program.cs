using System;
using System.Net;
using AsterNET.NetStandard.FastAGI;
using AsterNET.NetStandard.FastAGI.MappingStrategies;
using AsterNET.NetStandard.Manager;
using AsterNET.NetStandard.Manager.Action;
using AsterNET.NetStandard.Manager.Event;
using AsterNET.NetStandard.Manager.Exceptions;
using AsterNET.NetStandard.Manager.Response;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseWebSockets();

checkFastAGI();

app.Run();

static void checkFastAGI()
{
    //    Console.WriteLine(@"
    //Add next lines to your extension.conf file
    //	exten => 200,1,agi(agi://" + DEV_HOST + @"/customivr)
    //	exten => 200,2,Hangup()
    //reload Asterisk and dial 200 from phone.
    //Also enter 'agi debug' from Asterisk console to more information.
    //See CustomIVR.cs and fastagi-mapping.resx to detail.

    //Ctrl-C to exit");
    AsteriskFastAGI agi = new AsteriskFastAGI(ipaddress: "127.0.0.1", port: 4573);
    // Remove the lines below to enable the default (resource based) MappingStrategy
    // You can use an XML file with XmlMappingStrategy, or simply pass in a list of
    // ScriptMapping. 
    // If you wish to save it to a file, use ScriptMapping.SaveMappings and pass in a path.
    // This can then be used to load the mappings without having to change the source code!

    agi.MappingStrategy = new GeneralMappingStrategy(new List<ScriptMapping>()
            {
                new ScriptMapping() {
                    ScriptClass = "FastAgi.Script",
                    ScriptName = "Script"
                }
            });

    //agi.SC511_CAUSES_EXCEPTION = true;
    //agi.SCHANGUP_CAUSES_EXCEPTION = true;

    agi.Start();
    
}