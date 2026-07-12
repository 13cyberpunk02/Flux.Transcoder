import {Component, inject, input, signal, OnInit, effect, ChangeDetectionStrategy} from '@angular/core';
import {NgApexchartsModule, ChartComponent} from 'ng-apexcharts';
import type {ApexOptions} from 'ng-apexcharts';
import {ChannelsApi} from '../../core/api/channels.api';
import {MetricPoint} from '../../core/api/models';

@Component({
  selector: 'app-bitrate-chart',
  standalone: true,
  imports: [NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="glass rounded-2xl p-5">
      <div class="flex items-center justify-between mb-4">
        <h3 class="font-medium">Битрейт</h3>
        <div class="flex gap-1">
          @for (r of ranges; track r.minutes) {
            <button (click)="setRange(r.minutes)"
                    [class]="range() === r.minutes
                        ? 'px-2.5 py-1 rounded-lg text-xs font-medium bg-[rgb(var(--accent)/0.25)] text-[rgb(var(--accent))] border border-[rgb(var(--accent)/0.4)]'
                        : 'px-2.5 py-1 rounded-lg text-xs bg-[rgb(var(--surface)/var(--surface-opacity))] text-[rgb(var(--text-muted))] border border-[rgb(var(--border)/var(--border-opacity))] hover:text-[rgb(var(--text))]'">
              {{ r.label }}
            </button>
          }
        </div>
      </div>

      @if (loading()) {
        <div class="h-64 flex items-center justify-center text-slate-500">Загрузка…</div>
      } @else if (points().length === 0) {
        <div class="h-64 flex items-center justify-center text-slate-500">Нет данных за период</div>
      } @else {
        <apx-chart
          [series]="chartOptions.series!"
          [chart]="chartOptions.chart!"
          [xaxis]="chartOptions.xaxis!"
          [yaxis]="chartOptions.yaxis!"
          [stroke]="chartOptions.stroke!"
          [fill]="chartOptions.fill!"
          [grid]="chartOptions.grid!"
          [tooltip]="chartOptions.tooltip!"
          [dataLabels]="chartOptions.dataLabels!"
          [colors]="chartOptions.colors!"/>
      }
    </div>
  `,
})
export class BitrateChartComponent implements OnInit {
  readonly channelId = input.required<string>();
  private readonly api = inject(ChannelsApi);

  readonly ranges = [
    {label: '1ч', minutes: 60},
    {label: '6ч', minutes: 360},
    {label: '24ч', minutes: 1440},
  ];
  readonly range = signal(60);
  readonly points = signal<MetricPoint[]>([]);
  readonly loading = signal(false);

  chartOptions: ApexOptions = this.buildOptions([]);

  constructor() {
    // перестраиваем график при смене данных
    effect(() => {
      this.chartOptions = this.buildOptions(this.points());
    });
  }

  ngOnInit(): void {
    this.load();
  }

  setRange(minutes: number): void {
    this.range.set(minutes);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.getMetrics(this.channelId(), this.range()).subscribe({
      next: data => {
        this.points.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private buildOptions(data: MetricPoint[]): ApexOptions {
    return {
      series: [{
        name: 'Битрейт (kbps)',
        data: data.map(p => ({x: new Date(p.time).getTime(), y: Math.round(p.avgBitrate)})),
      }],
      chart: {
        type: 'area',
        height: 256,
        background: 'transparent',
        toolbar: {show: false},
        animations: {enabled: true, speed: 400},
        fontFamily: 'inherit',
      },
      colors: ['#818cf8'],
      stroke: {curve: 'smooth', width: 2},
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.4,
          opacityTo: 0.05,
          stops: [0, 100],
        },
      },
      dataLabels: {enabled: false},
      grid: {
        borderColor: 'rgba(255,255,255,0.08)',
        strokeDashArray: 4,
      },
      xaxis: {
        type: 'datetime',
        labels: {
          style: {colors: '#94a3b8'},
          datetimeUTC: false,
        },
        axisBorder: {color: 'rgba(255,255,255,0.1)'},
        axisTicks: {color: 'rgba(255,255,255,0.1)'},
      },
      yaxis: {
        labels: {
          style: {colors: '#94a3b8'},
          formatter: (val) => `${Math.round(val)}`,
        },
      },
      tooltip: {
        theme: 'dark',
        x: {format: 'HH:mm'},
      },
    };
  }
}
