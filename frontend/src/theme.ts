import { createTheme, type PaletteMode } from '@mui/material/styles';

export function buildTheme(mode: PaletteMode) {
  return createTheme({
    palette: {
      mode,
      primary: {
        main: mode === 'dark' ? '#818cf8' : '#4f46e5',
        light: mode === 'dark' ? '#a5b4fc' : '#6366f1',
        dark: mode === 'dark' ? '#6366f1' : '#3730a3',
        contrastText: '#ffffff',
      },
      secondary: {
        main: mode === 'dark' ? '#f472b6' : '#ec4899',
        contrastText: '#ffffff',
      },
      background: {
        default: mode === 'dark' ? '#09090b' : '#f8fafc',
        paper: mode === 'dark' ? '#18181b' : '#ffffff',
      },
      text: {
        primary: mode === 'dark' ? '#f4f4f5' : '#09090b',
        secondary: mode === 'dark' ? '#a1a1aa' : '#71717a',
        disabled: mode === 'dark' ? '#52525b' : '#d4d4d8',
      },
      divider: mode === 'dark' ? '#27272a' : '#e4e4e7',
      error: { main: '#ef4444' },
      warning: { main: '#f59e0b' },
      success: { main: '#22c55e' },
      info: { main: '#3b82f6' },
    },
    shape: {
      borderRadius: 8,
    },
    typography: {
      fontFamily: '"Inter", "Roboto", system-ui, -apple-system, sans-serif',
      h1: { fontWeight: 700 },
      h2: { fontWeight: 700 },
      h3: { fontWeight: 600 },
      h4: { fontWeight: 600 },
      h5: { fontWeight: 600 },
      h6: { fontWeight: 600 },
      button: { textTransform: 'none', fontWeight: 500 },
    },
    components: {
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { borderRadius: 8, fontWeight: 500 },
          contained: {
            '&:hover': { opacity: 0.92 },
          },
        },
      },
      MuiTextField: {
        defaultProps: { variant: 'outlined', size: 'small' },
      },
      MuiCard: {
        styleOverrides: {
          root: { borderRadius: 12 },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: { fontWeight: 500 },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: { borderRadius: 12 },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: { backgroundImage: 'none' },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: { backgroundImage: 'none' },
        },
      },
      MuiCssBaseline: {
        styleOverrides: `
          ::-webkit-scrollbar { width: 6px; height: 6px; }
          ::-webkit-scrollbar-track { background: transparent; }
          ::-webkit-scrollbar-thumb { background: #52525b; border-radius: 3px; }
          ::-webkit-scrollbar-thumb:hover { background: #71717a; }
        `,
      },
    },
  });
}
