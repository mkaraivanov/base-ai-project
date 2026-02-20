import React, { useState } from 'react';

interface Props {
  readonly reportType: string;
  readonly onExport: () => Promise<void>;
}

export const ExportButton: React.FC<Props> = ({ onExport }) => {
  const [loading, setLoading] = useState(false);

  const handleClick = async () => {
    setLoading(true);
    try {
      await onExport();
    } finally {
      setLoading(false);
    }
  };

  return (
    <button
      type="button"
      className="btn btn-secondary export-btn"
      onClick={handleClick}
      disabled={loading}
    >
      {loading ? 'Exporting…' : '⬇ Export CSV'}
    </button>
  );
};
