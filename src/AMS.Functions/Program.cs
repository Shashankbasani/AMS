using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;

/*
╔══════════════════════════════════════════════════════════════════════════════╗
║                    AZURE FUNCTIONS EXPLAINED                                  ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  Azure Functions are SERVERLESS compute - you don't manage servers.           ║
║  Code runs in response to TRIGGERS:                                           ║
║                                                                               ║
║  COMMON TRIGGERS:                                                             ║
║  - HTTP Trigger: Runs when HTTP request is received                           ║
║  - Timer Trigger: Runs on a schedule (like cron jobs)                         ║
║  - Queue Trigger: Runs when message arrives in Azure Service Bus              ║
║  - Blob Trigger: Runs when file is uploaded to Blob Storage                   ║
║                                                                               ║
║  HOW WE USE THEM:                                                             ║
║  1. ClaimSubmitted Queue → Send notification emails                           ║
║  2. ClaimStatusChanged Queue → Update related systems                         ║
║  3. PolicyExpiration Timer → Check for expiring policies                      ║
║                                                                               ║
║  BENEFITS:                                                                    ║
║  - Pay only for execution time                                                ║
║  - Auto-scales based on load                                                  ║
║  - No server management                                                       ║
║                                                                               ║
╚══════════════════════════════════════════════════════════════════════════════╝
*/

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
