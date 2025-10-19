using FluentResults;
using MediatR;
using VpnProviderWorker.Services;

namespace VpnProviderWorker.Command.AddClientToInboundCommand;

public class AddClientToInboundCommandHandler(
    IXUiService xuiService,
    ILogger<AddClientToInboundCommandHandler> logger)
    : IRequestHandler<AddClientToInboundCommand, Result>
{
    public async Task<Result> Handle(AddClientToInboundCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to add client to XUI for user {UserId} with name {ClientName}",
            request.UserId, request.TelegramName);

        var result = await xuiService.GenerateConfig(request.UserId, request.TelegramName);

        if (result.IsFailed)
        {
            logger.LogError($"Failed to add client for user {request.UserId}. Reasons: {result.Errors}");
            return Result.Fail(result.Errors);
        }

        logger.LogInformation("Successfully added client to XUI for user {UserId}", request.UserId);
        return Result.Ok();
    }
}