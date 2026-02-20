import React from 'react';
import type { ReportGranularity } from '../../types/reporting';

interface Props {
  readonly value: ReportGranularity;
  readonly onChange: (value: ReportGranularity) => void;
}

const OPTIONS: readonly ReportGranularity[] = ['Daily', 'Weekly', 'Monthly'];

export const GranularitySelector: React.FC<Props> = ({ value, onChange }) => {
  return (
    <div className="granularity-selector">
      {OPTIONS.map((opt) => (
        <button
          key={opt}
          type="button"
          className={`granularity-btn ${value === opt ? 'active' : ''}`}
          onClick={() => onChange(opt)}
        >
          {opt}
        </button>
      ))}
    </div>
  );
};
