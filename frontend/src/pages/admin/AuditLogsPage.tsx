import React, { useCallback, useEffect, useState } from 'react';
import { Download, Eye, Search, X } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Pagination from '@mui/material/Pagination';
import Chip from '@mui/material/Chip';
import Drawer from '@mui/material/Drawer';
import Divider from '@mui/material/Divider';
import { auditApi } from '../../api/auditApi';
import type { AuditLogDto, AuditLogFilterParams, PagedResult } from '../../types/audit';
import { extractErrorMessage } from '../../utils/errorHandler';

const ACTIONS = ['Created', 'Updated', 'Deleted'] as const;

const ENTITY_TYPES = [
  'Cinema', 'Hall', 'Movie', 'Showtime', 'TicketType',
  'Booking', 'Reservation', 'User', 'LoyaltySettings',
] as const;

const PAGE_SIZE = 20;

const ACTION_COLOR: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Created: 'success',
  Updated: 'warning',
  Deleted: 'error',
};

function JsonDiffView({ label, json }: { readonly label: string; readonly json: string | null }) {
  if (!json) return null;
  let parsed: unknown;
  try { parsed = JSON.parse(json); } catch { parsed = json; }
  return (
    <Box mb={2}>
      <Typography variant="caption" color="text.secondary" fontWeight={600} sx={{ display: 'block', mb: 0.5 }}>
        {label}
      </Typography>
      <Box
        component="pre"
        sx={{
          m: 0, p: 1.5, borderRadius: 1, fontSize: 12,
          bgcolor: 'action.hover', overflowX: 'auto',
          whiteSpace: 'pre-wrap', wordBreak: 'break-all',
          maxHeight: 280, overflowY: 'auto',
          fontFamily: 'monospace',
        }}
      >
        {JSON.stringify(parsed, null, 2)}
      </Box>
    </Box>
  );
}

export const AuditLogsPage: React.FC = () => {
  const { t } = useTranslation('admin');

  const [result, setResult] = useState<PagedResult<AuditLogDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [page, setPage] = useState(1);

  const [filter, setFilter] = useState<AuditLogFilterParams>({});
  const [draft, setDraft] = useState<AuditLogFilterParams>({});

  const [selected, setSelected] = useState<AuditLogDto | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const load = useCallback(async (f: AuditLogFilterParams, p: number) => {
    try {
      setLoading(true);
      const data = await auditApi.getAuditLogs(f, p, PAGE_SIZE);
      setResult(data);
    } catch (err) {
      toast.error(extractErrorMessage(err, t('auditLogs.toasts.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => { load(filter, page); }, [filter, page, load]);

  const applyFilter = () => {
    setFilter({ ...draft });
    setPage(1);
  };

  const clearFilter = () => {
    const empty: AuditLogFilterParams = {};
    setDraft(empty);
    setFilter(empty);
    setPage(1);
  };

  const setDraftField = (key: keyof AuditLogFilterParams, value: string) =>
    setDraft(prev => ({ ...prev, [key]: value || undefined }));

  const openDetail = (row: AuditLogDto) => {
    setSelected(row);
    setDrawerOpen(true);
  };

  const handleExport = async () => {
    try {
      setExporting(true);
      await auditApi.exportAuditLogsCsv(filter);
      toast.success(t('auditLogs.toasts.exported'));
    } catch (err) {
      toast.error(extractErrorMessage(err, t('auditLogs.toasts.exportFailed')));
    } finally {
      setExporting(false);
    }
  };

  const hasFilter = Object.values(filter).some(Boolean);

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3, flexWrap: 'wrap', gap: 2 }}>
          <Typography variant="h5" fontWeight={700}>{t('auditLogs.title')}</Typography>
          <MuiButton
            variant="outlined"
            startIcon={<Download size={16} />}
            onClick={handleExport}
            disabled={exporting}
            size="small"
          >
            {exporting ? t('common.loading') : t('auditLogs.export')}
          </MuiButton>
        </Box>

        {/* Filter Bar */}
        <Paper variant="outlined" sx={{ p: 2, mb: 2, borderRadius: 2 }}>
          <Grid container spacing={1.5} alignItems="center">
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <TextField
                label={t('auditLogs.filter.dateFrom')}
                type="date"
                size="small"
                fullWidth
                slotProps={{ inputLabel: { shrink: true } }}
                value={draft.dateFrom ?? ''}
                onChange={e => setDraftField('dateFrom', e.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <TextField
                label={t('auditLogs.filter.dateTo')}
                type="date"
                size="small"
                fullWidth
                slotProps={{ inputLabel: { shrink: true } }}
                value={draft.dateTo ?? ''}
                onChange={e => setDraftField('dateTo', e.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <TextField
                label={t('auditLogs.filter.action')}
                select
                size="small"
                fullWidth
                value={draft.action ?? ''}
                onChange={e => setDraftField('action', e.target.value)}
              >
                <MenuItem value="">{t('auditLogs.filter.allActions')}</MenuItem>
                {ACTIONS.map(a => <MenuItem key={a} value={a}>{t(`auditLogs.actionValues.${a}`)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <TextField
                label={t('auditLogs.filter.entityType')}
                select
                size="small"
                fullWidth
                value={draft.entityName ?? ''}
                onChange={e => setDraftField('entityName', e.target.value)}
              >
                <MenuItem value="">{t('auditLogs.filter.allEntities')}</MenuItem>
                {ENTITY_TYPES.map(e => <MenuItem key={e} value={e}>{e}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <TextField
                label={t('auditLogs.filter.search')}
                size="small"
                fullWidth
                value={draft.search ?? ''}
                onChange={e => setDraftField('search', e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') applyFilter(); }}
                slotProps={{ input: { endAdornment: <Search size={14} style={{ color: 'gray' }} /> } }}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <Stack direction="row" spacing={1}>
                <MuiButton variant="contained" size="small" onClick={applyFilter} fullWidth>
                  {t('auditLogs.filter.apply')}
                </MuiButton>
                {hasFilter && (
                  <Tooltip title={t('auditLogs.filter.clear')}>
                    <IconButton size="small" onClick={clearFilter}><X size={16} /></IconButton>
                  </Tooltip>
                )}
              </Stack>
            </Grid>
          </Grid>
        </Paper>

        {/* Table */}
        <Paper variant="outlined" sx={{ borderRadius: 2, overflow: 'hidden' }}>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ bgcolor: 'action.hover' }}>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.timestamp')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.user')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.entityType')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.entityId')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.action')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{t('auditLogs.columns.ipAddress')}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="center">{t('auditLogs.columns.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {loading
                  ? Array.from({ length: 8 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 7 }).map((_, j) => (
                        <TableCell key={j}><Skeleton variant="text" /></TableCell>
                      ))}
                    </TableRow>
                  ))
                  : result?.items.length === 0
                    ? (
                      <TableRow>
                        <TableCell colSpan={7} align="center" sx={{ py: 6, color: 'text.secondary' }}>
                          {t('auditLogs.emptyState')}
                        </TableCell>
                      </TableRow>
                    )
                    : result?.items.map(row => (
                      <TableRow key={row.id} hover>
                        <TableCell sx={{ whiteSpace: 'nowrap', fontSize: 13 }}>
                          {new Date(row.timestamp).toLocaleString()}
                        </TableCell>
                        <TableCell sx={{ fontSize: 13 }}>
                          <Box>{row.userEmail ?? '—'}</Box>
                          {row.userRole && (
                            <Typography variant="caption" color="text.secondary">{row.userRole}</Typography>
                          )}
                        </TableCell>
                        <TableCell sx={{ fontSize: 13 }}>{row.entityName}</TableCell>
                        <TableCell sx={{ fontSize: 12, fontFamily: 'monospace', maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {row.entityId}
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={t(`auditLogs.actionValues.${row.action}`, { defaultValue: row.action })}
                            size="small"
                            color={ACTION_COLOR[row.action] ?? 'default'}
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell sx={{ fontSize: 13 }}>{row.ipAddress ?? '—'}</TableCell>
                        <TableCell align="center">
                          <Tooltip title={t('auditLogs.viewDetails')}>
                            <IconButton size="small" onClick={() => openDetail(row)}>
                              <Eye size={15} />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                }
              </TableBody>
            </Table>
          </TableContainer>

          {/* Pagination */}
          {result && result.totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', px: 2, py: 1.5, borderTop: 1, borderColor: 'divider' }}>
              <Typography variant="caption" color="text.secondary">
                {t('auditLogs.pagination.total', { count: result.totalCount })}
              </Typography>
              <Pagination
                count={result.totalPages}
                page={page}
                onChange={(_, p) => setPage(p)}
                size="small"
                color="primary"
              />
            </Box>
          )}
        </Paper>
      </Container>

      {/* Detail Drawer */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        slotProps={{ paper: { sx: { width: { xs: '100vw', sm: 480 } } } }}
      >
        <Box sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6" fontWeight={600}>{t('auditLogs.drawerTitle')}</Typography>
            <IconButton size="small" onClick={() => setDrawerOpen(false)}><X size={18} /></IconButton>
          </Box>
          <Divider sx={{ mb: 2 }} />

          {selected && (
            <Box sx={{ flex: 1, overflowY: 'auto' }}>
              {/* Meta fields */}
              {[
                { label: t('auditLogs.columns.timestamp'), value: new Date(selected.timestamp).toLocaleString() },
                { label: t('auditLogs.columns.user'), value: selected.userEmail ?? '—' },
                { label: t('auditLogs.columns.action'), value: t(`auditLogs.actionValues.${selected.action}`, { defaultValue: selected.action }) },
                { label: t('auditLogs.columns.entityType'), value: selected.entityName },
                { label: t('auditLogs.columns.entityId'), value: selected.entityId },
                { label: t('auditLogs.columns.ipAddress'), value: selected.ipAddress ?? '—' },
              ].map(({ label, value }) => (
                <Box key={label} sx={{ display: 'flex', mb: 1.5, gap: 1 }}>
                  <Typography variant="body2" color="text.secondary" sx={{ minWidth: 120, flexShrink: 0 }}>
                    {label}
                  </Typography>
                  <Typography variant="body2" sx={{ wordBreak: 'break-all' }}>{value}</Typography>
                </Box>
              ))}

              <Divider sx={{ my: 2 }} />

              <JsonDiffView label={t('auditLogs.oldValues')} json={selected.oldValues} />
              <JsonDiffView label={t('auditLogs.newValues')} json={selected.newValues} />
            </Box>
          )}
        </Box>
      </Drawer>
    </Box>
  );
};
