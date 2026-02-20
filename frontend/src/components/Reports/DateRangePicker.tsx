import React from 'react';

interface DateRangePreset {
  readonly label: string;
  readonly from: string;
  readonly to: string;
}

const today = () => new Date().toISOString().slice(0, 10);
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().slice(0, 10);
};

const PRESETS: readonly DateRangePreset[] = [
  { label: 'Last 7 days', from: daysAgo(7), to: today() },
  { label: 'Last 30 days', from: daysAgo(30), to: today() },
  { label: 'Last 90 days', from: daysAgo(90), to: today() },
  { label: 'Last 365 days', from: daysAgo(365), to: today() },
];

interface Props {
  readonly from: string;
  readonly to: string;
  readonly onChange: (from: string, to: string) => void;
}

export const DateRangePicker: React.FC<Props> = ({ from, to, onChange }) => {
  return (
    <div className="date-range-picker">
      <div className="date-range-presets">
        {PRESETS.map((preset) => (
          <button
            key={preset.label}
            className={`preset-btn ${from === preset.from && to === preset.to ? 'active' : ''}`}
            onClick={() => onChange(preset.from, preset.to)}
            type="button"
          >
            {preset.label}
          </button>
        ))}
      </div>
      <div className="date-range-custom">
        <label>
          From
          <input
            type="date"
            value={from}
            max={to}
            onChange={(e) => onChange(e.target.value, to)}
          />
        </label>
        <label>
          To
          <input
            type="date"
            value={to}
            min={from}
            max={today()}
            onChange={(e) => onChange(from, e.target.value)}
          />
        </label>
      </div>
    </div>
  );
};
