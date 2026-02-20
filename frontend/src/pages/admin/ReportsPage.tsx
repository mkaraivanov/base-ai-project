import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { SalesByDateTab } from './reports/SalesByDateTab';
import { SalesByMovieTab } from './reports/SalesByMovieTab';
import { SalesByShowtimeTab } from './reports/SalesByShowtimeTab';
import { SalesByLocationTab } from './reports/SalesByLocationTab';

type Tab = 'date' | 'movie' | 'showtime' | 'location';

interface TabConfig {
  readonly id: Tab;
  readonly label: string;
}

export const ReportsPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [activeTab, setActiveTab] = useState<Tab>('date');

  const TABS: readonly TabConfig[] = [
    { id: 'date', label: t('reports.tabs.date') },
    { id: 'movie', label: t('reports.tabs.movie') },
    { id: 'showtime', label: t('reports.tabs.showtime') },
    { id: 'location', label: t('reports.tabs.location') },
  ];

  const defaultTo = new Date().toISOString().slice(0, 10);
  const defaultFrom = (() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().slice(0, 10);
  })();

  return (
    <div className="page">
      <div className="container">
        <h1>{t('reports.pageTitle')}</h1>

        <div className="report-tabs">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              className={`tab-btn ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
              data-testid={`tab-${tab.id}`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="report-tab-content">
          {activeTab === 'date' && (
            <SalesByDateTab defaultFrom={defaultFrom} defaultTo={defaultTo} />
          )}
          {activeTab === 'movie' && (
            <SalesByMovieTab defaultFrom={defaultFrom} defaultTo={defaultTo} />
          )}
          {activeTab === 'showtime' && (
            <SalesByShowtimeTab defaultFrom={defaultFrom} defaultTo={defaultTo} />
          )}
          {activeTab === 'location' && (
            <SalesByLocationTab defaultFrom={defaultFrom} defaultTo={defaultTo} />
          )}
        </div>
      </div>
    </div>
  );
};
