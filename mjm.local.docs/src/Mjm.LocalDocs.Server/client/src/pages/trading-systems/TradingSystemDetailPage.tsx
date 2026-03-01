import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import TextField from '@mui/material/TextField';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Link from '@mui/material/Link';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Divider from '@mui/material/Divider';
import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';
import Tooltip from '@mui/material/Tooltip';
import Alert from '@mui/material/Alert';
import InputAdornment from '@mui/material/InputAdornment';
import { alpha } from '@mui/material/styles';
import SaveRoundedIcon from '@mui/icons-material/SaveRounded';
import EditRoundedIcon from '@mui/icons-material/EditRounded';
import DeleteRoundedIcon from '@mui/icons-material/DeleteRounded';
import LinkRoundedIcon from '@mui/icons-material/LinkRounded';
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded';
import CodeRoundedIcon from '@mui/icons-material/CodeRounded';
import AttachFileRoundedIcon from '@mui/icons-material/AttachFileRounded';
import UploadFileRoundedIcon from '@mui/icons-material/UploadFileRounded';
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded';
import LocalOfferRoundedIcon from '@mui/icons-material/LocalOfferRounded';
import ShowChartRoundedIcon from '@mui/icons-material/ShowChartRounded';
import ExpandMoreRoundedIcon from '@mui/icons-material/ExpandMoreRounded';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import { useSnackbar } from 'notistack';
import LoadingSpinner from '../../components/shared/LoadingSpinner';
import ConfirmDialog from '../../components/shared/ConfirmDialog';
import FileDropzone from '../../components/shared/FileDropzone';
import StatusChip, { getStatusOptions } from '../../components/trading-systems/StatusChip';
import {
  useTradingSystem,
  useTradingSystemCode,
  useTradingSystemAttachments,
  useUpdateTradingSystem,
  useUpdateTradingSystemStatus,
  useSaveTradingSystemCode,
  useImportTradingSystemCode,
  useExportTradingSystemCode,
  useAddTradingSystemAttachment,
  useRemoveTradingSystemAttachment,
  useDeleteTradingSystem,
} from '../../hooks/useTradingSystems';
import { formatDate, formatFileSize } from '../../utils/formatters';
import { downloadDocumentFile } from '../../api/documents';
import type { TradingSystemStatus } from '../../types';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel({ children, value, index }: TabPanelProps) {
  return (
    <div role="tabpanel" hidden={value !== index}>
      {value === index && <Box sx={{ py: 2 }}>{children}</Box>}
    </div>
  );
}

export default function TradingSystemDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { enqueueSnackbar } = useSnackbar();

  const { data: system, isLoading: systemLoading } = useTradingSystem(id!);
  const { data: code, isLoading: codeLoading } = useTradingSystemCode(id!);
  const { data: attachments, isLoading: attachmentsLoading } = useTradingSystemAttachments(id!);

  const updateMutation = useUpdateTradingSystem();
  const updateStatusMutation = useUpdateTradingSystemStatus();
  const saveCodeMutation = useSaveTradingSystemCode();
  const importCodeMutation = useImportTradingSystemCode();
  const exportCodeMutation = useExportTradingSystemCode();
  const addAttachmentMutation = useAddTradingSystemAttachment();
  const removeAttachmentMutation = useRemoveTradingSystemAttachment();
  const deleteMutation = useDeleteTradingSystem();

  const [activeTab, setActiveTab] = useState(0);
  const [editMode, setEditMode] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [statusMenuAnchor, setStatusMenuAnchor] = useState<null | HTMLElement>(null);

  // Edit form state
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editSourceUrl, setEditSourceUrl] = useState('');
  const [editNotes, setEditNotes] = useState('');
  const [editTags, setEditTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState('');

  // Code editor state
  const [editedCode, setEditedCode] = useState('');
  const [codeChanged, setCodeChanged] = useState(false);

  // Initialize form when system loads
  useEffect(() => {
    if (system) {
      setEditName(system.name);
      setEditDescription(system.description || '');
      setEditSourceUrl(system.sourceUrl || '');
      setEditNotes(system.notes || '');
      setEditTags([...system.tags]);
    }
  }, [system]);

  // Initialize code when it loads
  useEffect(() => {
    if (code !== undefined) {
      setEditedCode(code);
      setCodeChanged(false);
    }
  }, [code]);

  const handleAddTag = () => {
    const trimmed = tagInput.trim().toLowerCase();
    if (trimmed && !editTags.includes(trimmed)) {
      setEditTags([...editTags, trimmed]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tag: string) => {
    setEditTags(editTags.filter((t) => t !== tag));
  };

  const handleSaveMetadata = async () => {
    if (!system) return;
    try {
      await updateMutation.mutateAsync({
        id: system.id,
        name: editName.trim(),
        description: editDescription.trim() || undefined,
        sourceUrl: editSourceUrl.trim() || undefined,
        tags: editTags.length > 0 ? editTags : undefined,
        notes: editNotes.trim() || undefined,
      });
      setEditMode(false);
      enqueueSnackbar('Trading system updated', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to update trading system', { variant: 'error' });
    }
  };

  const handleStatusChange = async (newStatus: TradingSystemStatus) => {
    if (!system) return;
    setStatusMenuAnchor(null);
    try {
      await updateStatusMutation.mutateAsync({ id: system.id, status: newStatus });
      enqueueSnackbar(`Status changed to ${newStatus}`, { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to update status', { variant: 'error' });
    }
  };

  const handleSaveCode = async () => {
    if (!system) return;
    try {
      await saveCodeMutation.mutateAsync({ id: system.id, code: editedCode });
      setCodeChanged(false);
      enqueueSnackbar('Code saved', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to save code', { variant: 'error' });
    }
  };

  const handleImportCode = async (file: File) => {
    if (!system) return;
    try {
      await importCodeMutation.mutateAsync({ id: system.id, file });
      enqueueSnackbar('Code imported', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to import code', { variant: 'error' });
    }
  };

  const handleExportCode = async () => {
    if (!system) return;
    const fileName = `${system.name.replace(/[^a-z0-9]/gi, '_')}.el`;
    try {
      await exportCodeMutation.mutateAsync({ id: system.id, fileName });
      enqueueSnackbar('Code exported', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to export code', { variant: 'error' });
    }
  };

  const handleAddAttachment = async (files: File[]) => {
    if (!system || files.length === 0) return;
    try {
      for (const file of files) {
        await addAttachmentMutation.mutateAsync({ id: system.id, file });
      }
      enqueueSnackbar('Attachment(s) added', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to add attachment', { variant: 'error' });
    }
  };

  const handleRemoveAttachment = async (attachmentId: string) => {
    if (!system) return;
    try {
      await removeAttachmentMutation.mutateAsync({ id: system.id, attachmentId });
      enqueueSnackbar('Attachment removed', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to remove attachment', { variant: 'error' });
    }
  };

  const handleDelete = async () => {
    if (!system) return;
    try {
      await deleteMutation.mutateAsync(system.id);
      enqueueSnackbar('Trading system deleted', { variant: 'success' });
      navigate('/trading-systems');
    } catch {
      enqueueSnackbar('Failed to delete trading system', { variant: 'error' });
    }
  };

  if (systemLoading) {
    return <LoadingSpinner />;
  }

  if (!system) {
    return (
      <Box sx={{ textAlign: 'center', py: 6 }}>
        <Typography variant="h6" color="text.secondary">
          Trading system not found
        </Typography>
        <Button sx={{ mt: 2 }} onClick={() => navigate('/trading-systems')}>
          Back to list
        </Button>
      </Box>
    );
  }

  const statusOptions = getStatusOptions();

  return (
    <Box>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 2 }}>
        <Link
          underline="hover"
          color="inherit"
          href="/trading-systems"
          onClick={(e) => {
            e.preventDefault();
            navigate('/trading-systems');
          }}
          sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}
        >
          <ShowChartRoundedIcon fontSize="small" />
          Trading Systems
        </Link>
        <Typography color="text.primary">{system.name}</Typography>
      </Breadcrumbs>

      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
            <Typography variant="h5" fontWeight={600}>
              {system.name}
            </Typography>
            <StatusChip
              status={system.status}
              onClick={(e) => setStatusMenuAnchor(e.currentTarget as HTMLElement)}
            />
            <Menu
              anchorEl={statusMenuAnchor}
              open={Boolean(statusMenuAnchor)}
              onClose={() => setStatusMenuAnchor(null)}
            >
              {statusOptions.map((opt) => (
                <MenuItem
                  key={opt.value}
                  onClick={() => handleStatusChange(opt.value)}
                  selected={opt.value === system.status}
                >
                  {opt.label}
                </MenuItem>
              ))}
            </Menu>
          </Box>
          {system.description && (
            <Typography variant="body1" color="text.secondary">
              {system.description}
            </Typography>
          )}
        </Box>
        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            color="error"
            startIcon={<DeleteRoundedIcon />}
            onClick={() => setDeleteOpen(true)}
          >
            Delete
          </Button>
        </Stack>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)}>
          <Tab icon={<DescriptionRoundedIcon />} iconPosition="start" label="Details" />
          <Tab icon={<CodeRoundedIcon />} iconPosition="start" label="Code" />
          <Tab
            icon={<AttachFileRoundedIcon />}
            iconPosition="start"
            label={`Attachments (${attachments?.length || 0})`}
          />
        </Tabs>
      </Box>

      {/* Details Tab */}
      <TabPanel value={activeTab} index={0}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
              <Typography variant="h6">Details</Typography>
              {!editMode ? (
                <Button startIcon={<EditRoundedIcon />} onClick={() => setEditMode(true)}>
                  Edit
                </Button>
              ) : (
                <Stack direction="row" spacing={1}>
                  <Button onClick={() => setEditMode(false)}>Cancel</Button>
                  <Button
                    variant="contained"
                    startIcon={<SaveRoundedIcon />}
                    onClick={handleSaveMetadata}
                    disabled={updateMutation.isPending}
                  >
                    Save
                  </Button>
                </Stack>
              )}
            </Box>

            {editMode ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label="Name"
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  fullWidth
                  required
                />
                <TextField
                  label="Description"
                  value={editDescription}
                  onChange={(e) => setEditDescription(e.target.value)}
                  fullWidth
                  multiline
                  rows={2}
                />
                <TextField
                  label="Source URL"
                  value={editSourceUrl}
                  onChange={(e) => setEditSourceUrl(e.target.value)}
                  fullWidth
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <LinkRoundedIcon sx={{ color: 'text.secondary' }} />
                      </InputAdornment>
                    ),
                  }}
                />
                <Box>
                  <TextField
                    label="Tags"
                    value={tagInput}
                    onChange={(e) => setTagInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ',') {
                        e.preventDefault();
                        handleAddTag();
                      }
                    }}
                    onBlur={handleAddTag}
                    fullWidth
                    helperText="Press Enter to add"
                  />
                  {editTags.length > 0 && (
                    <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', gap: 1 }}>
                      {editTags.map((tag) => (
                        <Chip key={tag} label={tag} size="small" onDelete={() => handleRemoveTag(tag)} />
                      ))}
                    </Stack>
                  )}
                </Box>
                <TextField
                  label="Notes"
                  value={editNotes}
                  onChange={(e) => setEditNotes(e.target.value)}
                  fullWidth
                  multiline
                  rows={4}
                />
              </Box>
            ) : (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {system.sourceUrl && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Source
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <LinkRoundedIcon fontSize="small" sx={{ color: 'text.secondary' }} />
                      <Link href={system.sourceUrl} target="_blank" rel="noopener">
                        {system.sourceUrl}
                        <OpenInNewRoundedIcon sx={{ fontSize: 14, ml: 0.5, verticalAlign: 'middle' }} />
                      </Link>
                    </Box>
                  </Box>
                )}

                {system.tags.length > 0 && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Tags
                    </Typography>
                    <Stack direction="row" spacing={1} sx={{ mt: 0.5, flexWrap: 'wrap', gap: 1 }}>
                      {system.tags.map((tag) => (
                        <Chip key={tag} label={tag} size="small" icon={<LocalOfferRoundedIcon />} />
                      ))}
                    </Stack>
                  </Box>
                )}

                {system.notes && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Notes
                    </Typography>
                    <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                      {system.notes}
                    </Typography>
                  </Box>
                )}

                <Box sx={{ display: 'flex', gap: 4 }}>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Created
                    </Typography>
                    <Typography variant="body2">{formatDate(system.createdAt)}</Typography>
                  </Box>
                  {system.updatedAt && (
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Last Updated
                      </Typography>
                      <Typography variant="body2">{formatDate(system.updatedAt)}</Typography>
                    </Box>
                  )}
                </Box>
              </Box>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Code Tab */}
      <TabPanel value={activeTab} index={1}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">EasyLanguage Code</Typography>
              <Stack direction="row" spacing={1}>
                <Button
                  size="small"
                  startIcon={<UploadFileRoundedIcon />}
                  component="label"
                >
                  Import
                  <input
                    type="file"
                    hidden
                    accept=".el,.txt"
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) handleImportCode(file);
                      e.target.value = '';
                    }}
                  />
                </Button>
                {system.codeDocumentId && (
                  <Button size="small" startIcon={<DownloadRoundedIcon />} onClick={handleExportCode}>
                    Export
                  </Button>
                )}
                <Button
                  variant="contained"
                  size="small"
                  startIcon={<SaveRoundedIcon />}
                  onClick={handleSaveCode}
                  disabled={!codeChanged || saveCodeMutation.isPending}
                >
                  Save Code
                </Button>
              </Stack>
            </Box>

            {codeChanged && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                You have unsaved changes
              </Alert>
            )}

            <TextField
              fullWidth
              multiline
              rows={20}
              value={editedCode}
              onChange={(e) => {
                setEditedCode(e.target.value);
                setCodeChanged(true);
              }}
              placeholder="// Enter your EasyLanguage code here..."
              sx={{
                '& .MuiInputBase-input': {
                  fontFamily: 'monospace',
                  fontSize: '0.875rem',
                  lineHeight: 1.5,
                },
              }}
            />
          </CardContent>
        </Card>
      </TabPanel>

      {/* Attachments Tab */}
      <TabPanel value={activeTab} index={2}>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Attachments
            </Typography>

            <FileDropzone
              onDrop={handleAddAttachment}
              accept={{
                'image/*': [],
                'application/pdf': [],
                'text/*': [],
                'application/vnd.ms-excel': [],
                'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': [],
              }}
              multiple
            />

            {attachments && attachments.length > 0 && (
              <Box sx={{ mt: 3 }}>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Uploaded Files
                </Typography>
                <Stack spacing={1}>
                  {attachments.map((att) => (
                    <Box
                      key={att.id}
                      sx={(theme) => ({
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        p: 1.5,
                        borderRadius: 1,
                        bgcolor: alpha(theme.palette.primary.main, 0.05),
                        border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
                      })}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <AttachFileRoundedIcon sx={{ color: 'text.secondary' }} />
                        <Box>
                          <Typography variant="body2">{att.fileName}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatFileSize(att.fileSizeBytes)} • {formatDate(att.createdAt)}
                          </Typography>
                        </Box>
                      </Box>
                      <Stack direction="row" spacing={1}>
                        <Tooltip title="Download">
                          <IconButton
                            size="small"
                            onClick={() => downloadDocumentFile(att.id, att.fileName)}
                          >
                            <DownloadRoundedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Remove">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => handleRemoveAttachment(att.id)}
                          >
                            <DeleteRoundedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </Box>
                  ))}
                </Stack>
              </Box>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={deleteOpen}
        title="Delete Trading System"
        message={`Are you sure you want to delete "${system.name}"? This will also delete all associated code and attachments. This action cannot be undone.`}
        confirmText="Delete"
        confirmColor="error"
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />
    </Box>
  );
}
