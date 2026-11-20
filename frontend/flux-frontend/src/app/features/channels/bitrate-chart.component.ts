import { Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { form, FormField } from '@angular/forms/signals';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexOptions } from 'ng-apexcharts';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';

import { MetricPoint } from '../../core/api/models';
import { ThemeService } from '../../core/theme/theme.service';
import { formatBitrate } from '../../core/format/format';

const RANGES = [
  { value: 60, label: '1 ч' },
  { value: 360, label: '6 ч' },
  { value: 1440, label: '24 ч' },
];

/** Цвета графика под тему (близко к палитре M3 cyan: primary / on-surface-variant). */
const PALETTE = {
  dark: { line: '#4dd9e6', text: '#bec8ca', grid: 'rgba(255,255,255,0.08)' },
  light: { line: '#00687a', text: '#3f484a', grid: 'rgba(0,0,0,0.08)' },
};

const REFRESH_MS = 60_000;

/** Русская локаль ApexCharts (подписи оси времени). */
const RU_LOCALE = {
  name: 'ru',
  options: {
    months: ['Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'],
    shortMonths: ['янв', 'фев', 'мар', 'апр', 'мая', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек'],
    days: ['Воскресенье', 'Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота'],
    shortDays: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
  },
};

/** Поминутный битрейт канала. Клик по графику — открыть архив с этого момента. */
@Component({
  selector: 'app-bitrate-chart',
  imports: [NgApexchartsModule, FormField, MatButtonToggleGroup, MatButtonToggle],
  template: `
    <div class="flex items-center justify-between gap-3 flex-wrap mb-3">
      <div>
        <h2 class="font-semibold">Битрейт</h2>
        <p class="text-xs text-muted">Среднее за минуту · клик по графику открывает архив с этого момента</p>
      </div>
      <mat-button-toggle-group [formField]="rangeForm.minutes" hideSingleSelectionIndicator aria-label="Период">
        @for (r of ranges; track r.value) {
          <mat-button-toggle [value]="r.value">{{ r.label }}</mat-button-toggle>
        }
      </mat-button-toggle-group>
    </div>

    @if (metrics.isLoading() && !metrics.hasValue()) {
      <div class="h-64 rounded-md bg-surface-high animate-pulse"></div>
    } @else if (metrics.error()) {
      <div class="h-64 flex items-center justify-center text-sm text-error">Не удалось загрузить метрики</div>
    } @else if (points().length === 0) {
      <div class="h-64 flex flex-col items-center justify-center gap-1 text-sm text-muted">
        <span class="material-symbols-outlined !text-3xl" aria-hidden="true">show_chart</span>
        Нет данных за период
      </div>
    } @else {
      <apx-chart
        [series]="options().series!"
        [chart]="options().chart!"
        [xaxis]="options().xaxis!"
        [yaxis]="options().yaxis!"
        [stroke]="options().stroke!"
        [fill]="options().fill!"
        [grid]="options().grid!"
        [tooltip]="options().tooltip!"
        [dataLabels]="options().dataLabels!"
        [colors]="options().colors!" />
    }
  `,
})
export class BitrateChartComponent {
  readonly channelId = input.required<string>();
  /** Пользователь кликнул в точку графика — время этой точки. */
  readonly pointSelected = output<Date>();

  private readonly theme = inject(ThemeService);

  protected readonly ranges = RANGES;
  private readonly rangeModel = signal({ minutes: 60 });
  protected readonly rangeForm = form(this.rangeModel);

  protected readonly metrics = httpResource<MetricPoint[]>(() =>
    `/api/channels/${this.channelId()}/metrics?minutes=${this.rangeModel().minutes}`);

  protected readonly points = computed(() => this.metrics.value() ?? []);

  /** Индекс точки под курсором (tooltip shared) — по клику берём его. */
  private hoverIndex = -1;

  protected readonly options = computed<ApexOptions>(() => {
    const data = this.points();
    const c = PALETTE[this.theme.theme()];
    return {
      series: [{
        name: 'Битрейт',
        data: data.map(p => ({ x: new Date(p.time).getTime(), y: Math.round(p.avgBitrate) })),
      }],
      chart: {
        type: 'area',
        height: 256,
        background: 'transparent',
        toolbar: { show: false },
        zoom: { enabled: false },
        animations: { enabled: false },
        fontFamily: 'inherit',
        locales: [RU_LOCALE],
        defaultLocale: 'ru',
        events: {
          mouseMove: (_e: unknown, _ctx: unknown, cfg: { dataPointIndex: number }) => {
            if (cfg?.dataPointIndex >= 0) this.hoverIndex = cfg.dataPointIndex;
          },
          click: () => {
            const p = data[this.hoverIndex];
            if (p) this.pointSelected.emit(new Date(p.time));
          },
        },
      },
      colors: [c.line],
      stroke: { curve: 'smooth', width: 2 },
      fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.02, stops: [0, 100] } },
      dataLabels: { enabled: false },
      grid: { borderColor: c.grid, strokeDashArray: 4 },
      xaxis: {
        type: 'datetime',
        labels: {
          style: { colors: c.text },
          datetimeUTC: false,
          datetimeFormatter: { year: 'yyyy', month: 'MMM', day: 'dd MMM', hour: 'HH:mm', minute: 'HH:mm' },
        },
        axisBorder: { color: c.grid },
        axisTicks: { color: c.grid },
        tooltip: { enabled: false },
      },
      yaxis: {
        labels: { style: { colors: c.text }, formatter: v => formatBitrate(v) },
      },
      tooltip: {
        theme: this.theme.theme(),
        shared: true,
        intersect: false,
        x: { format: 'dd.MM HH:mm' },
        y: { formatter: v => formatBitrate(v) },
      },
    };
  });

  constructor() {
    // поминутные агрегаты обновляются раз в минуту — подтягиваем свежие
    const timer = setInterval(() => this.metrics.reload(), REFRESH_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
