using FluentResults;
using MediatR;
using SubscriptionKafkaContracts.From.VpnServiceEvents;
using VpnProviderWorker.Kafka;
using VpnProviderWorker.Services;

namespace VpnProviderWorker.Command.AddClientToInboundCommand;

public class AddClientToInboundCommandHandler(
    IXUiService xuiService,
    IMessageBus messageBus,
    ILogger<AddClientToInboundCommandHandler> logger)
    : IRequestHandler<AddClientToInboundCommand, Result>
{
    public async Task<Result> Handle(AddClientToInboundCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to add client to XUI for user {UserId} with name {ClientName}",
            request.UserId, request.TelegramName);
        
        DateTimeOffset endDateOffset = new DateTimeOffset(request.EndDate.ToUniversalTime());
        long expiryTime = endDateOffset.ToUnixTimeMilliseconds();
        var result = await xuiService.GenerateConfig(
            request.UserId,
            request.TelegramName,
            expiryTime,
            request.SubscriptionId,
            0,
            5);

        if (result.IsFailed)
        {
            logger.LogError($"Failed to add client for user {request.UserId}. Reasons: {result.Errors}");
            return Result.Fail(result.Errors);
        }

        logger.LogInformation("Successfully added client to XUI for user {UserId}", request.UserId);
        await messageBus.Publish(new VpnConfigGenerated(
            eventId: Guid.NewGuid(),
            userId: request.UserId,
            telegramId: request.TelegramId, 
            telegramName: request.TelegramName,
            vpnConfig: result.Value.ToString()
        ), cancellationToken);
        
        return Result.Ok();
    }
}