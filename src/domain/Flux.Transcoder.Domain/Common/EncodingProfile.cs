namespace Flux.Transcoder.Domain.Common;

public sealed class EncodingProfile : AggregateRoot
{
    private readonly List<Rendition> _renditions = [];

    public string Name { get; private set; }
    public IReadOnlyCollection<Rendition> Renditions => _renditions.AsReadOnly();

    private EncodingProfile(string name) => Name = name;

    public EncodingProfile(string name, IEnumerable<Rendition> renditions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название профиля декодирования не может быть пустым.");

        var list = renditions?.ToList() ?? [];
        if (list.Count == 0)
            throw new DomainException("Профиль декодирования должен иметь хотя бы одно исполнение.");

        Name = name.Trim();
        _renditions.AddRange(list);
    }

    public void AddRendition(Rendition rendition)
    {
        ArgumentNullException.ThrowIfNull(rendition);
        _renditions.Add(rendition);
    }
}