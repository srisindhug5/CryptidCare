using ApiStartup = CryptidCare.Api.Configuration.Startup;
using ApplicationStartup = CryptidCare.Application.Configuration.Startup;
using DataStartup = CryptidCare.Data.Configuration.Startup;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ApiStartup.ConfigureServices(builder);
ApplicationStartup.ConfigureServices(builder.Services);
DataStartup.ConfigureServices(builder.Services, builder.Configuration);

WebApplication app = builder.Build();

await DataStartup.ApplyPersistenceAsync(app.Services).ConfigureAwait(false);

ApiStartup.Configure(app);

await app.RunAsync().ConfigureAwait(false);
