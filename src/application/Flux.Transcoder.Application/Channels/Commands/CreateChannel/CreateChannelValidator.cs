using FluentValidation;

namespace Flux.Transcoder.Application.Channels.Commands.CreateChannel;

public sealed class CreateChannelValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelValidator()
    {
        RuleFor(x => x.SourceUrl)
            .NotEmpty().WithMessage("Источник не может быть пустым.");

        RuleFor(x => x.ProfileId)
            .NotEmpty().WithMessage("Id профиля обязательно нужно заполнять.");
    }
}