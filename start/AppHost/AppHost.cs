var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");


var web = builder.AddProject<Projects.MyWeatherHub>("website")
    .WithReference(api);



builder.Build().Run();
