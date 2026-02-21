import React, { useState, useEffect } from 'react';
import Button from '@mui/material/Button';
import ButtonGroup from '@mui/material/ButtonGroup';
import i18n from '../../i18n';

const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'EN' },
  { code: 'bg', label: 'BG' },
] as const;

const resolveLang = (lng: string) => lng.split('-')[0];

export const LanguageSwitcher: React.FC = () => {
  const [currentLang, setCurrentLang] = useState(() => resolveLang(i18n.language ?? 'en'));

  useEffect(() => {
    const onLanguageChanged = (lng: string) => setCurrentLang(resolveLang(lng));
    i18n.on('languageChanged', onLanguageChanged);
    return () => { i18n.off('languageChanged', onLanguageChanged); };
  }, []);

  return (
    <ButtonGroup size="small" sx={{ height: 32 }}>
      {SUPPORTED_LANGUAGES.map(({ code, label }) => (
        <Button
          key={code}
          onClick={() => i18n.changeLanguage(code)}
          variant={currentLang === code ? 'contained' : 'outlined'}
          color="primary"
          disableElevation
          sx={{ px: 1.5, fontSize: 12, fontWeight: 600, textTransform: 'none', minWidth: 36 }}
        >
          {label}
        </Button>
      ))}
    </ButtonGroup>
  );
};
