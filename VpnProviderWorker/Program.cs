using Dapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VpnProviderWorker;
using VpnProviderWorker.Services;
using VpnProviderWorker.Services.Xui;
using Quartz;
using VpnProviderWorker.Command.AddClientToInboundCommand;
using VpnProviderWorker.Kafka;
using VpnProviderWorker.Persistence;
using VpnProviderWorker.Persistence.Inbox;
using VpnProviderWorker.Persistence.Outbox;
using VpnProviderWorker.Persistence.TypeHandlers;


var builder = Host.CreateApplicationBuilder(args);

Dapper.SqlMapper.AddTypeHandler(new GuidTypeHandler());

builder.Services.Configure<XUiOptions>(builder.Configuration.GetSection(XUiOptions.SectionName));

builder.Services.AddHttpClient<XUiClient>();
builder.Services.AddScoped<IXUiService, XUiService>();

builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IInbox, Inbox>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(AddClientToInboundCommandHandler).Assembly
    )
);

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory(); 
    x.AddRider(rider =>
    {
        rider.AddConsumer<SubscriptionCreatedConsumer>();

        //TODO доделать продюсера rider.AddProducer<string, >();
        rider.UsingKafka((context, k) =>
        {
            k.TopicEndpoint<SubscriptionKafkaContracts.From.SubscriptionKafkaEvents.SubscriptionCreated>(
                "subscription-created",
                "vpn-provider-worker",
                e => e.ConfigureConsumer<SubscriptionCreatedConsumer>(context));
            
            k.Host(builder.Configuration.GetValue<string>("Kafka:BootstrapServers"));
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