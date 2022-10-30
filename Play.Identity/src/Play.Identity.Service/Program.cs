using MassTransit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.MassTransit;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Play.Identity.Service.HostedServcices;
using Play.Identity.Service.Settings;

var builder = WebApplication.CreateBuilder(args);
const string AllowedOriginSetting = "AllowedOrigin";

// Add services to the container.
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
var identityServerSettings = builder.Configuration.GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();
var identitySettingsConfiguration = builder.Configuration.GetSection(nameof(IdentitySettings));

builder.Services.Configure<IdentitySettings>(identitySettingsConfiguration)
    .AddDefaultIdentity<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
    (
        mongoDbSettings.ConnectionString,
        serviceSettings.ServiceName
    );

builder.Services.AddMassTransitWithRabbitMq(retyConfigurator =>
{
    retyConfigurator.Interval(3, TimeSpan.FromSeconds(5));
    retyConfigurator.Ignore(typeof(UnknownUserException));
    retyConfigurator.Ignore(typeof(InsufficientFundsException));
}
);

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseSuccessEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseErrorEvents = true;
})
    .AddAspNetIdentity<ApplicationUser>()
    .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
    .AddInMemoryApiResources(identityServerSettings.ApiResources)
    .AddInMemoryClients(identityServerSettings.Clients)
    .AddInMemoryIdentityResources(identityServerSettings.IdentityResources)
    .AddDeveloperSigningCredential();

builder.Services.AddLocalApiAuthentication();

builder.Services.AddControllers();
builder.Services.AddHostedService<IdentitySeedHostedService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(corsPolicyBuilder =>
       {
           corsPolicyBuilder.WithOrigins(builder.Configuration[AllowedOriginSetting])
               .AllowAnyHeader()
               .AllowAnyMethod();
       });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
