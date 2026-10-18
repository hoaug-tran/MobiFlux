using FluentValidation;

namespace MobiFlux.Application.Features.Rotation.RotateIp;

public sealed class RotateIpCommandValidator : AbstractValidator<RotateIpCommand>
{
    public RotateIpCommandValidator() => RuleFor(command => command.TriggeredBy).MaximumLength(200);
}
