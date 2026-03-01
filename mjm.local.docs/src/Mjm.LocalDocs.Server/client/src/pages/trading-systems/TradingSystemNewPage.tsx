import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import TextField from '@mui/material/TextField';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Link from '@mui/material/Link';
import InputAdornment from '@mui/material/InputAdornment';
import AddRoundedIcon from '@mui/icons-material/AddRounded';
import LinkRoundedIcon from '@mui/icons-material/LinkRounded';
import LocalOfferRoundedIcon from '@mui/icons-material/LocalOfferRounded';
import ShowChartRoundedIcon from '@mui/icons-material/ShowChartRounded';
import { useSnackbar } from 'notistack';
import { useCreateTradingSystem } from '../../hooks/useTradingSystems';

export default function TradingSystemNewPage() {
  const navigate = useNavigate();
  const { enqueueSnackbar } = useSnackbar();
  const createMutation = useCreateTradingSystem();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [sourceUrl, setSourceUrl] = useState('');
  const [notes, setNotes] = useState('');
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState<string[]>([]);
  const [errors, setErrors] = useState<{ name?: string }>({});

  const handleAddTag = () => {
    const trimmed = tagInput.trim().toLowerCase();
    if (trimmed && !tags.includes(trimmed)) {
      setTags([...tags, trimmed]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tag: string) => {
    setTags(tags.filter((t) => t !== tag));
  };

  const handleTagKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      handleAddTag();
    }
  };

  const handleSubmit = async () => {
    const newErrors: { name?: string } = {};
    if (!name.trim()) {
      newErrors.name = 'Name is required';
    }
    setErrors(newErrors);

    if (Object.keys(newErrors).length > 0) return;

    try {
      const created = await createMutation.mutateAsync({
        name: name.trim(),
        description: description.trim() || undefined,
        sourceUrl: sourceUrl.trim() || undefined,
        tags: tags.length > 0 ? tags : undefined,
        notes: notes.trim() || undefined,
      });

      enqueueSnackbar('Trading system created successfully', { variant: 'success' });
      navigate(`/trading-systems/${created.id}`);
    } catch (error) {
      enqueueSnackbar('Failed to create trading system', { variant: 'error' });
    }
  };

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
        <Typography color="text.primary">New</Typography>
      </Breadcrumbs>

      {/* Header */}
      <Typography variant="h5" fontWeight={600} sx={{ mb: 3 }}>
        Create Trading System
      </Typography>

      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {/* Name */}
            <TextField
              label="Name"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (errors.name) setErrors({});
              }}
              error={!!errors.name}
              helperText={errors.name}
              required
              fullWidth
              placeholder="e.g., Dual Moving Average Crossover"
            />

            {/* Description */}
            <TextField
              label="Description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              multiline
              rows={3}
              fullWidth
              placeholder="Describe the strategy and its logic..."
            />

            {/* Source URL */}
            <TextField
              label="Source URL"
              value={sourceUrl}
              onChange={(e) => setSourceUrl(e.target.value)}
              fullWidth
              placeholder="https://example.com/strategy-article"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <LinkRoundedIcon sx={{ color: 'text.secondary' }} />
                  </InputAdornment>
                ),
              }}
            />

            {/* Tags */}
            <Box>
              <TextField
                label="Tags"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyDown={handleTagKeyDown}
                onBlur={handleAddTag}
                fullWidth
                placeholder="Press Enter to add tags"
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <LocalOfferRoundedIcon sx={{ color: 'text.secondary' }} />
                    </InputAdornment>
                  ),
                }}
                helperText="e.g., trend-following, mean-reversion, futures"
              />
              {tags.length > 0 && (
                <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', gap: 1 }}>
                  {tags.map((tag) => (
                    <Chip
                      key={tag}
                      label={tag}
                      size="small"
                      onDelete={() => handleRemoveTag(tag)}
                    />
                  ))}
                </Stack>
              )}
            </Box>

            {/* Notes */}
            <TextField
              label="Notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              multiline
              rows={4}
              fullWidth
              placeholder="Additional notes, observations, or reminders..."
            />

            {/* Actions */}
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
              <Button variant="outlined" onClick={() => navigate('/trading-systems')}>
                Cancel
              </Button>
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                onClick={handleSubmit}
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? 'Creating...' : 'Create Trading System'}
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
