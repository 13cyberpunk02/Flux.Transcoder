using FluentValidation;

namespace Flux.Transcoder.Application.Profiles.Commands.CreateProfile;

public sealed class CreateProfileValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя профиля не должно быть пустым.")
            .MaximumLength(128).WithMessage("Имя профиля не длиннее 128 символов.");

        RuleFor(x => x.Renditions)
            .NotNull().WithMessage("Список исполнений(конфигураций) обязателен.")
            .Must(r => r is { Count: > 0 }).WithMessage("Нужен минимум одно исполнение(конфигурация).");

        RuleForEach(x => x.Renditions).ChildRules(r =>
        {
            r.RuleFor(x => x.Width).GreaterThan(0).WithMessage("Ширина должна быть положительной.");
            r.RuleFor(x => x.Height).GreaterThan(0).WithMessage("Высота должна быть положительной.");
            r.RuleFor(x => x.BitrateKbps).GreaterThan(0).WithMessage("Битрейт должен быть положительным.");
        });
    }
}