using System.Net;
using Confluent.Kafka;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VpnProviderWorker.Services;
using VpnProviderWorker.Services.Xui;
using Quartz;
using VpnProviderWorker.Command.AddClientToInboundCommand;
using VpnProviderWorker.DelegatingHandlers;
using VpnProviderWorker.Kafka;
using VpnProviderWorker.Persistence;
using VpnProviderWorker.Persistence.Inbox;
using VpnProviderWorker.Persistence.Outbox;
using VpnProviderWorker.Persistence.TypeHandlers;
using Polly;
using Polly.Extensions.Http;
using SubscriptionKafkaContracts.From.SubscriptionKafkaEvents;
using SubscriptionKafkaContracts.From.VpnServiceEvents;


var builder = Host.CreateApplicationBuilder(args);

Dapper.SqlMapper.AddTypeHandler(new GuidTypeHandler());

builder.Services.Configure<XUiOptions>(builder.Configuration.GetSection(XUiOptions.SectionName));

builder.Services.AddTransient<ReLoginHandler>();
builder.Services.AddScoped<IXUiService, XUiService>();

builder.Services.AddSingleton<CookieContainer>();

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<IXUiClient, XUiClient>(client =>
    {
        var xuiOptions = builder.Configuration.GetSection(XUiOptions.SectionName).Get<XUiOptions>();
        client.BaseAddress = new Uri(xuiOptions.ApiUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(ServiceProvider =>
    {
        return new HttpClientHandler
        {
            CookieContainer = ServiceProvider.GetRequiredService<CookieContainer>()
        };
    })
    .AddHttpMessageHandler<ReLoginHandler>()
    .AddPolicyHandler(retryPolicy);

builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IInbox, Inbox>();
builder.Services.AddTransient<IMessageBus, KafkaProducer>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(AddClientToInboundCommandHandler).Assembly
    )
);

builder.Services.Configure<KafkaTopicsConfiguration>(
    builder.Configuration.GetSection("KafkaTopics"));
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory(); 
    x.AddRider(rider =>
    {
        rider.AddConsumer<SubscriptionCreatedConsumer>();
        rider.AddProducer<string, VpnConfigGenerated>(builder.Configuration.GetValue<string>("KafkaTopics:VpnConfigGeneratedTopic"));

        
        rider.UsingKafka((context, k) =>
        {
            var topics = context.GetRequiredService<IOptions<KafkaTopicsConfiguration>>().Value;
            var kafka = context.GetRequiredService<IOptions<KafkaOptions>>().Value;
            
            k.Host(kafka.BootstrapServers);
            
            void ConfigureEndpoint<TConsumer, TEvent>(IKafkaTopicReceiveEndpointConfigurator<Ignore, TEvent> e)
                where TConsumer : class, IConsumer
                where TEvent : class
            {
                e.EnableAutoOffsetStore = false;
                e.EnablePartitionEof = true;
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.CreateIfMissing();
                e.UseKillSwitch(cfg => cfg.SetActivationThreshold(1)
                    .SetRestartTimeout(TimeSpan.FromMinutes(1))
                    .SetTripThreshold(0.05)
                    .SetTrackingPeriod(TimeSpan.FromMinutes(1)));
                e.UseMessageRetry(retry => retry.Interval(200, TimeSpan.FromSeconds(1)));
                e.ConfigureConsumer<TConsumer>(context);
            }
            
            k.TopicEndpoint<SubscriptionKafkaContracts.From.SubscriptionKafkaEvents.SubscriptionCreated>(
                topics.SubscriptionCreatedTopic,
                kafka.GroupId,
                e => ConfigureEndpoint<
                SubscriptionCreatedConsumer,
                SubscriptionCreated>(e));
        });
    });
});

builder.Services.AddQuartz(configure =>
{
    var outboxJobKey = new JobKey(nameof(OutboxBackgroundJob));
    configure
        .AddJob<OutboxBackgroundJob>(j => j.WithIdentity(outboxJobKey))
        .AddTrigger(trigger => trigger.ForJob(outboxJobKey)
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10).RepeatForever()));

    var inboxJobKey = new JobKey(nameof(InboxBackgroundJob));
    configure
        .AddJob<InboxBackgroundJob>(j => j.WithIdentity(inboxJobKey))
        .AddTrigger(trigger => trigger.ForJob(inboxJobKey)
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);


var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dbContext.Database.Migrate();
}

host.Run();