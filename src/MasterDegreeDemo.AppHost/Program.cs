var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddProject<Projects.MasterDegreeDemo_EventReceiver1>("eventreceiver1");
builder
    .AddProject<Projects.MasterDegreeDemo_EventReceiver2>("eventreceiver2");

builder.Build().Run();
