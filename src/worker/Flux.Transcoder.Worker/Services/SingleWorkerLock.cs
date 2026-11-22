using Npgsql;

namespace Flux.Transcoder.Worker.Services;

/// <summary>
/// Один активный воркер на БД (session advisory lock). Без этого второй воркер при сверке
/// запустил бы все каналы ещё раз, и два ffmpeg писали бы в одни папки.
/// Второй экземпляр ждёт и подхватывает каналы, когда первый умирает.
/// Когда появится привязка канала к воркеру, блокировка станет не нужна.
/// </summary>
public sealed class SingleWorkerLock : BackgroundService
{
    private const long LockKey = 0x464C5558_574B52;   // "FLUXWKR"

    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);

    // наша же оборванная сессия может держать lock, пока сервер её не закроет (keepalive ниже — до 25 сек)
    private static readonly TimeSpan LostLockGrace = TimeSpan.FromSeconds(45);

    private readonly string _connectionString;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SingleWorkerLock> _logger;
    private NpgsqlConnection? _connection;

    public SingleWorkerLock(
        IConfiguration configuration,
        IHostApplicationLifetime lifetime,
        ILogger<SingleWorkerLock> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
        _connectionString = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("flux_transcoder_db"))
        {
            Pooling = false,   // lock живёт ровно столько, сколько это соединение
            KeepAlive = 10
        }.ConnectionString;
    }

    /// <summary>Вызывается до запуска хоста: пока lock не наш, не слушаем очередь и ничего не запускаем.</summary>
    public async Task AcquireAsync()
    {
        string? lastProblem = null;
        while (true)
        {
            string problem;
            try
            {
                if (await TryLockAsync(CancellationToken.None))
                {
                    _logger.LogInformation("Блокировка активного воркера получена");
                    return;
                }

                problem = "активен другой воркер, этот ждёт в резерве";
            }
            catch (Exception ex)
            {
                problem = $"БД недоступна ({ex.Message})";
            }

            if (problem != lastProblem)
                _logger.LogWarning("Старт воркера отложен: {Problem}", problem);

            lastProblem = problem;
            await Task.Delay(RetryInterval);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset? heldByOtherSince = null;
        using var timer = new PeriodicTimer(RetryInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (_connection is not null)
                    {
                        await using var ping = new NpgsqlCommand("SELECT 1", _connection);
                        await ping.ExecuteScalarAsync(stoppingToken);
                        continue;
                    }

                    if (await TryLockAsync(stoppingToken))
                    {
                        _logger.LogInformation("Блокировка активного воркера восстановлена");
                        heldByOtherSince = null;
                        continue;
                    }

                    heldByOtherSince ??= DateTimeOffset.UtcNow;
                    if (DateTimeOffset.UtcNow - heldByOtherSince < LostLockGrace)
                        continue;

                    _logger.LogCritical("Блокировку забрал другой воркер — останавливаюсь, чтобы не задвоить каналы");
                    _lifetime.StopApplication();
                    return;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // БД недоступна — эфир не трогаем, переподключаемся
                    _logger.LogWarning("Нет связи с БД для блокировки воркера: {Reason}", ex.Message);
                    heldByOtherSince = null;

                    if (_connection is not null)
                    {
                        await _connection.DisposeAsync();
                        _connection = null;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // штатное выключение; lock отпустим в Dispose, когда ffmpeg уже остановлены
        }
    }

    private async Task<bool> TryLockAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(ct);

            // машина воркера пропала без RST — сервер должен заметить за секунды, а не за 2 часа
            await using (var keepalive = new NpgsqlCommand(
                             "SET tcp_keepalives_idle = 10; SET tcp_keepalives_interval = 5; SET tcp_keepalives_count = 3",
                             connection))
                await keepalive.ExecuteNonQueryAsync(ct);

            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", LockKey);

            if (await command.ExecuteScalarAsync(ct) is not true)
            {
                await connection.DisposeAsync();
                return false;
            }

            _connection = connection;
            return true;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public override void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
    }
}
