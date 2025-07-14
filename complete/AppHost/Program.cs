var builder = DistributedApplication.CreateBuilder(args);

var invalidationKey = builder.AddParameter("ApiCacheInvalidationKey");	

var cache = builder.AddRedis("cache")
	.WithClearCommand()
	.WithRedisInsight();

var api = builder.AddProject<Projects.Api>("api")
	.WithApiCacheInvalidation(invalidationKey)
	.WithReference(cache);

var postgres = builder.AddPostgres("postgres")
								.WithDataVolume(isReadOnly: false);

var weatherDb = postgres.AddDatabase("weatherdb");

var web = builder.AddProject<Projects.MyWeatherHub>("myweatherhub")
								 .WithReference(api)
								 .WithReference(weatherDb)
								 .WaitFor(postgres)
								 .WithExternalHttpEndpoints();

builder.Build().Run();
