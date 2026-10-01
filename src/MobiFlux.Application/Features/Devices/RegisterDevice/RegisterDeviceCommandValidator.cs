using FluentValidation;

namespace MobiFlux.Application.Features.Devices.RegisterDevice;

public sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(command => command.AdbSerial).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}
