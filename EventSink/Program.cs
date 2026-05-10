using Amazon.S3;
using Amazon.SimpleNotificationService;
using AppHost.ServiceDefaults;
using EventSink.Messaging;
using EventSink.Storage;
using LocalStack.Client.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml"));
});

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddScoped<IStorageService, StorageService>();

var app = builder.Build();

using var scope = app.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<SubscriptionService>().SubscribeEndpoint();
await scope.ServiceProvider.GetRequiredService<IStorageService>().EnsureBucketExists();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.Run();
