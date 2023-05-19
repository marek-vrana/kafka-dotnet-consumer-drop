using KafkaDotNetConsumer;
using KafkaDotNetConsumer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();
var logger = Log.Logger.ForContext<Program>();

var builder = WebApplication.CreateBuilder(args);

var env = Environment.GetEnvironmentVariable("K8S_ENVIRONMENT")?.ToLowerInvariant();
logger.Information($"Starting application in {env ?? "local"} environment");

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaExtendedSettings>();
builder.Services.AddSingleton(kafkaConfig);
kafkaConfig.ConsumerConfig.GroupId = kafkaConfig.ConsumerConfig.GroupId.Replace("[host]", Dns.GetHostName());
kafkaConfig.ConsumerConfig.ClientId = kafkaConfig.ConsumerConfig.ClientId.Replace("[host]", Dns.GetHostName());
builder.Services.AddSingleton(kafkaConfig.ConsumerConfig);

builder.Services.AddHostedService<KafkaTopicConsumer1>();
builder.Services.AddHostedService<KafkaTopicConsumer2>();
builder.Services.AddHostedService<KafkaTopicConsumer3>();

var app = builder.Build();
await app.RunAsync();