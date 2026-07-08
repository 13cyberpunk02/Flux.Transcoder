namespace Flux.Transcoder.Application.Channels.Commands.UpdateRetention;

/// <summary>
/// Обновление Retention(длительность всего архива в минутах,
/// по истечению сборщик удаляет те сегменты, которые уже больше заданной минуты)
/// в минутах — простое число с фронта, до минуты точность
/// </summary>
/// <param name="ChannelId">ID канала(потока)</param>
/// <param name="RetentionMinutes">Минуты продолжительность жизни</param>
public sealed record UpdateRetentionCommand(Guid ChannelId, int RetentionMinutes);