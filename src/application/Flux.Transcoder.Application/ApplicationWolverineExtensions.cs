using Wolverine;
using Wolverine.FluentValidation;

namespace Flux.Transcoder.Application;

public static class ApplicationWolverineExtensions
{
    // Маркер сборки для сканирования валидаторов/хендлеров
    public static readonly System.Reflection.Assembly Assembly =
        typeof(ApplicationWolverineExtensions).Assembly;

    public static WolverineOptions UseApplicationLayer(this WolverineOptions opts)
    {
        // Подключает FluentValidation-middleware в пайплайн:
        // валидаторы вызываются автоматически ПЕРЕД хендлерами,
        // и Wolverine оборачивает только те хендлеры, у которых есть валидатор.
        opts.UseFluentValidation();
        
        // Регистрируем все хендлеры из этой сборки
        opts.Discovery.IncludeAssembly(Assembly);

        return opts;
    }
}