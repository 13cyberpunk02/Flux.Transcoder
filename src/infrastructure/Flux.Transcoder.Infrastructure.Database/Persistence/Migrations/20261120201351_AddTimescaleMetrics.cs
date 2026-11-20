using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Migrations
{
    /// <summary>
    /// Метрики каналов в TimescaleDB: hypertable сырых замеров, поминутный и почасовой
    /// continuous aggregate, политики обновления и хранения.
    /// Всё идемпотентно (IF NOT EXISTS / if_not_exists) — на БД, где схему уже создали
    /// руками, миграция просто ничего не меняет.
    /// Требует shared_preload_libraries=timescaledb (см. deploy/docker-compose.yml).
    /// </summary>
    public partial class AddTimescaleMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb;");

            // сырые замеры (пишет ChannelHealthHandler раз в 5 сек на канал)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS channel_metrics (
                    time           timestamptz      NOT NULL,
                    channel_id     uuid             NOT NULL,
                    bitrate_kbps   double precision NOT NULL,
                    dropped_frames integer          NOT NULL,
                    uptime_seconds double precision NOT NULL,
                    status         text             NOT NULL
                );
                """);

            migrationBuilder.Sql(
                "SELECT create_hypertable('channel_metrics', 'time', if_not_exists => TRUE);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_metrics_channel_time ON channel_metrics (channel_id, time DESC);");

            // поминутный агрегат (читает MetricsReader для графика битрейта).
            // WITH NO DATA — иначе создание cagg нельзя выполнить в транзакции миграции;
            // данные наполнит политика обновления.
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS channel_metrics_1m
                WITH (timescaledb.continuous) AS
                SELECT time_bucket(INTERVAL '1 minute', time) AS bucket,
                       channel_id,
                       avg(bitrate_kbps)    AS avg_bitrate,
                       max(bitrate_kbps)    AS max_bitrate,
                       sum(dropped_frames)  AS dropped_frames,
                       max(uptime_seconds)  AS uptime_seconds
                FROM channel_metrics
                GROUP BY bucket, channel_id
                WITH NO DATA;
                """);

            // почасовой агрегат поверх поминутного (иерархический cagg, TimescaleDB 2.9+)
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS channel_metrics_1h
                WITH (timescaledb.continuous) AS
                SELECT time_bucket(INTERVAL '1 hour', bucket) AS bucket,
                       channel_id,
                       avg(avg_bitrate)     AS avg_bitrate,
                       max(max_bitrate)     AS max_bitrate,
                       sum(dropped_frames)  AS dropped_frames,
                       max(uptime_seconds)  AS uptime_seconds
                FROM channel_metrics_1m
                GROUP BY 1, channel_id
                WITH NO DATA;
                """);

            // Политики добавляем, только если у объекта ещё НЕТ политики этого типа.
            // if_not_exists => TRUE пропускает лишь политику с теми же параметрами:
            // при других параметрах TimescaleDB пытается добавить вторую и падает
            // с "refresh interval overlaps with an existing continuous aggregate policy".
            // В timescaledb_information.jobs.hypertable_name для cagg лежит имя самого view.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                                   WHERE proc_name = 'policy_refresh_continuous_aggregate'
                                     AND hypertable_name = 'channel_metrics_1m') THEN
                        PERFORM add_continuous_aggregate_policy('channel_metrics_1m',
                            start_offset      => INTERVAL '10 minutes',
                            end_offset        => INTERVAL '1 minute',
                            schedule_interval => INTERVAL '1 minute');
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                                   WHERE proc_name = 'policy_refresh_continuous_aggregate'
                                     AND hypertable_name = 'channel_metrics_1h') THEN
                        PERFORM add_continuous_aggregate_policy('channel_metrics_1h',
                            start_offset      => INTERVAL '3 hours',
                            end_offset        => INTERVAL '1 hour',
                            schedule_interval => INTERVAL '30 minutes');
                    END IF;

                    -- хранение: сырые 2 дня, поминутные 14 дней, почасовые 90 дней
                    IF NOT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                                   WHERE proc_name = 'policy_retention'
                                     AND hypertable_name = 'channel_metrics') THEN
                        PERFORM add_retention_policy('channel_metrics', INTERVAL '2 days');
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                                   WHERE proc_name = 'policy_retention'
                                     AND hypertable_name = 'channel_metrics_1m') THEN
                        PERFORM add_retention_policy('channel_metrics_1m', INTERVAL '14 days');
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                                   WHERE proc_name = 'policy_retention'
                                     AND hypertable_name = 'channel_metrics_1h') THEN
                        PERFORM add_retention_policy('channel_metrics_1h', INTERVAL '90 days');
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS channel_metrics_1h CASCADE;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS channel_metrics_1m CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS channel_metrics CASCADE;");
        }
    }
}
