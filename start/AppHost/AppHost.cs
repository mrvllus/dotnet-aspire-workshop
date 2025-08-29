var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(cache)
    .WaitFor(cache);

var web = builder.AddProject<Projects.MyWeatherHub>("website")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();