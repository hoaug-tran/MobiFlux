using FluentValidation;
using MobiFlux.Domain.Devices;
namespace MobiFlux.Application.Features.Proxies.UpdateProxyPool;
public sealed class UpdateProxyPoolCommandValidator : AbstractValidator<UpdateProxyPoolCommand>
{
    public UpdateProxyPoolCommandValidator()
    {
        RuleFor(command => command.PoolId).NotEmpty();
        RuleFor(command => command.Strategy).Must(strategy => Enum.TryParse<RoutingStrategy>(strategy, true, out var value) && Enum.IsDefined(value));
        RuleFor(command => command.StickyTtlSeconds).InclusiveBetween(0, 86400);
        When(command => command.Members is not null, () =>
        {
            RuleFor(command => command.Members).NotEmpty().Must(members => members!.Select(member => member.DeviceId).Distinct().Count() == members!.Count);
            RuleForEach(command => command.Members).ChildRules(member =>
            {
                member.RuleFor(item => item.DeviceId).NotEmpty();
                member.RuleFor(item => item.Weight).InclusiveBetween(1, 10000);
                member.RuleFor(item => item.Priority).GreaterThanOrEqualTo(0);
            });
        });
    }
}
