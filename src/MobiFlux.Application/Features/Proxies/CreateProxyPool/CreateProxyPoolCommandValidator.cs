using FluentValidation;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Proxies.CreateProxyPool;

public sealed class CreateProxyPoolCommandValidator : AbstractValidator<CreateProxyPoolCommand>
{
    public CreateProxyPoolCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Strategy).Must(strategy => Enum.TryParse<RoutingStrategy>(strategy, true, out _))
            .WithMessage("Strategy must be RoundRobin, LeastConnections, WeightedRoundRobin, or Random.");
        RuleFor(command => command.StickyTtlSeconds).InclusiveBetween(0, 86_400);
        RuleFor(command => command.Members).NotEmpty();
        RuleForEach(command => command.Members).ChildRules(member =>
        {
            member.RuleFor(item => item.DeviceId).NotEmpty();
            member.RuleFor(item => item.Weight).GreaterThan(0);
            member.RuleFor(item => item.Priority).GreaterThanOrEqualTo(0);
        });
        RuleFor(command => command.Members).Must(members => members.Select(member => member.DeviceId).Distinct().Count() == members.Count)
            .WithMessage("Each device can be included once only.");
    }
}
