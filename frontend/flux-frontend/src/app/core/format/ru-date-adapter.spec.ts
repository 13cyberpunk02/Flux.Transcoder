import { TestBed } from '@angular/core/testing';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { RuDateAdapter } from './ru-date-adapter';
import { combineDateAndTime } from './format';

describe('RuDateAdapter', () => {
  let adapter: RuDateAdapter;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RuDateAdapter, { provide: MAT_DATE_LOCALE, useValue: 'ru-RU' }],
    });
    adapter = TestBed.inject(RuDateAdapter);
  });

  it.each([
    ['05.11.2026', 2026, 10, 5],
    ['21.11.2026', 2026, 10, 21],
    ['1.2.2027', 2027, 1, 1],
    ['29.02.2028', 2028, 1, 29],
    [' 21/11/2026 ', 2026, 10, 21],
  ])('parses "%s" as day first', (text, year, month, day) => {
    expect(adapter.parse(text)).toEqual(new Date(year, month, day));
  });

  it.each(['31.11.2026', '29.02.2027', '11.21.2026', '00.11.2026', '21.11.202', '21.11.26', 'abc'])(
    'rejects "%s"',
    text => expect(adapter.isValid(adapter.parse(text)!)).toBe(false),
  );

  it('treats empty input as no date', () => {
    expect(adapter.parse('  ')).toBeNull();
  });

  it('starts the week on Monday', () => {
    expect(adapter.getFirstDayOfWeek()).toBe(1);
  });
});

describe('combineDateAndTime', () => {
  it('takes day from date and hh:mm from time', () => {
    expect(combineDateAndTime(new Date(2026, 10, 5), new Date(2026, 0, 1, 12, 34, 56)))
      .toEqual(new Date(2026, 10, 5, 12, 34));
  });

  it('returns null for missing or invalid parts', () => {
    expect(combineDateAndTime(null, new Date())).toBeNull();
    expect(combineDateAndTime(new Date(), new Date(NaN))).toBeNull();
  });
});
