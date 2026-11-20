import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { ChannelsStore } from './channels.store';
import { ConfirmData, ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

/**
 * Действия над каналом с подтверждением опасных операций.
 * Общие для таблицы и страницы канала.
 */
@Injectable({ providedIn: 'root' })
export class ChannelActions {
  private readonly store = inject(ChannelsStore);
  private readonly dialog = inject(MatDialog);

  start(id: string): void {
    this.store.start(id);
  }

  restart(id: string): void {
    this.store.restart(id);
  }

  /** Остановка прерывает эфир — спрашиваем. */
  async stop(id: string, source: string): Promise<void> {
    const ok = await this.confirm({
      title: 'Остановить канал?',
      message: `Трансляция «${source}» прекратится, запись архива остановится.`,
      confirmLabel: 'Остановить',
      icon: 'warning',
      danger: true,
    });
    if (ok) this.store.stop(id);
  }

  /** Удаление необратимо; true — канал удалён. */
  async delete(id: string, source: string): Promise<boolean> {
    const ok = await this.confirm({
      title: 'Удалить канал?',
      message: `Канал «${source}» и его настройки будут удалены. Записанный архив останется на диске.`,
      confirmLabel: 'Удалить',
      icon: 'delete',
      danger: true,
    });
    return ok ? this.store.delete(id) : false;
  }

  private async confirm(data: ConfirmData): Promise<boolean> {
    const ref = this.dialog.open<ConfirmDialogComponent, ConfirmData, boolean>(ConfirmDialogComponent, {
      data,
      width: '28rem',
      autoFocus: 'first-tabbable',
    });
    return (await firstValueFrom(ref.afterClosed())) === true;
  }
}
