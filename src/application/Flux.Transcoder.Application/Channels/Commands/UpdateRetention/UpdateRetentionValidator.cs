using FluentValidation;

namespace Flux.Transcoder.Application.Channels.Commands.UpdateRetention;

public sealed class UpdateRetentionValidator : AbstractValidator<UpdateRetentionCommand>
{
    public UpdateRetentionValidator()
    {
        RuleFor(x => x.RetentionMinutes)
            .GreaterThanOrEqualTo(1).WithMessage("Минимум 1 минута.")
            .LessThanOrEqualTo(14 * 24 * 60).WithMessage("Максимум 14 дней.");
    }
}