var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(cache);


var web = builder.AddProject<Projects.MyWeatherHub>("website")
    .WithReference(api);



builder.Build().Run();
