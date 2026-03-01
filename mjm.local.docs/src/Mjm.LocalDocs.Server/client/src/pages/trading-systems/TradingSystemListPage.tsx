import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardActionArea from '@mui/material/CardActionArea';
import Typography from '@mui/material/Typography';
import TextField from '@mui/material/TextField';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import InputAdornment from '@mui/material/InputAdornment';
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Grid from '@mui/material/Grid';
import { alpha } from '@mui/material/styles';
import SearchRoundedIcon from '@mui/icons-material/SearchRounded';
import AddRoundedIcon from '@mui/icons-material/AddRounded';
import CodeRoundedIcon from '@mui/icons-material/CodeRounded';
import AttachFileRoundedIcon from '@mui/icons-material/AttachFileRounded';
import ShowChartRoundedIcon from '@mui/icons-material/ShowChartRounded';
import LoadingSpinner from '../../components/shared/LoadingSpinner';
import StatusChip, { getStatusOptions } from '../../components/trading-systems/StatusChip';
import { useTradingSystems } from '../../hooks/useTradingSystems';
import { formatDate } from '../../utils/formatters';
import type { TradingSystemStatus, TradingSystemListItem } from '../../types';

export default function TradingSystemListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<TradingSystemStatus | 'all'>('all');

  const { data: systems, isLoading } = useTradingSystems(
    statusFilter === 'all' ? undefined : statusFilter,
    search || undefined
  );

  const statusOptions = useMemo(() => getStatusOptions(), []);

  const filteredSystems = useMemo(() => {
    if (!systems) return [];
    if (!search) return systems;
    const lower = search.toLowerCase();
    return systems.filter(
      (s) =>
        s.name.toLowerCase().includes(lower) ||
        s.description?.toLowerCase().includes(lower) ||
        s.tags.some((t) => t.toLowerCase().includes(lower))
    );
  }, [systems, search]);

  const handleStatusFilter = (_: React.MouseEvent<HTMLElement>, newStatus: TradingSystemStatus | 'all') => {
    if (newStatus !== null) {
      setStatusFilter(newStatus);
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 3,
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <ShowChartRoundedIcon sx={{ fontSize: 28, color: 'primary.main' }} />
          <Typography variant="h5" fontWeight={600}>
            Trading Systems
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddRoundedIcon />}
          onClick={() => navigate('/trading-systems/new')}
        >
          New Trading System
        </Button>
      </Box>

      {/* Search and Filters */}
      <Box sx={{ mb: 3 }}>
        <TextField
          fullWidth
          placeholder="Search trading systems..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          size="small"
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchRoundedIcon sx={{ color: 'text.secondary' }} />
              </InputAdornment>
            ),
          }}
          sx={{ mb: 2 }}
        />

        <ToggleButtonGroup
          value={statusFilter}
          exclusive
          onChange={handleStatusFilter}
          size="small"
          sx={{ flexWrap: 'wrap', gap: 0.5 }}
        >
          <ToggleButton value="all">All</ToggleButton>
          {statusOptions.map((opt) => (
            <ToggleButton key={opt.value} value={opt.value}>
              {opt.label}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
      </Box>

      {/* Results Count */}
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {filteredSystems.length} trading system{filteredSystems.length !== 1 ? 's' : ''} found
      </Typography>

      {/* Trading Systems Grid */}
      {filteredSystems.length === 0 ? (
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 6 }}>
            <ShowChartRoundedIcon sx={{ fontSize: 48, color: 'text.disabled', mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              No trading systems found
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              {search ? 'Try a different search term' : 'Create your first trading system to get started'}
            </Typography>
            {!search && (
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                onClick={() => navigate('/trading-systems/new')}
              >
                Create Trading System
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={2}>
          {filteredSystems.map((system) => (
            <Grid size={{ xs: 12, sm: 6, md: 4 }} key={system.id}>
              <TradingSystemCard system={system} onClick={() => navigate(`/trading-systems/${system.id}`)} />
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
}

interface TradingSystemCardProps {
  system: TradingSystemListItem;
  onClick: () => void;
}

function TradingSystemCard({ system, onClick }: TradingSystemCardProps) {
  return (
    <Card
      sx={{
        height: '100%',
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-2px)',
          boxShadow: 4,
        },
      }}
    >
      <CardActionArea onClick={onClick} sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          {/* Header */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
            <Typography variant="subtitle1" fontWeight={600} sx={{ flex: 1, mr: 1 }}>
              {system.name}
            </Typography>
            <StatusChip status={system.status} size="small" />
          </Box>

          {/* Description */}
          {system.description && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                mb: 2,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
              }}
            >
              {system.description}
            </Typography>
          )}

          {/* Tags */}
          {system.tags.length > 0 && (
            <Stack direction="row" spacing={0.5} sx={{ mb: 2, flexWrap: 'wrap', gap: 0.5 }}>
              {system.tags.slice(0, 3).map((tag) => (
                <Chip
                  key={tag}
                  label={tag}
                  size="small"
                  variant="outlined"
                  sx={{ fontSize: '0.7rem', height: 22 }}
                />
              ))}
              {system.tags.length > 3 && (
                <Chip
                  label={`+${system.tags.length - 3}`}
                  size="small"
                  variant="outlined"
                  sx={{ fontSize: '0.7rem', height: 22 }}
                />
              )}
            </Stack>
          )}

          {/* Footer */}
          <Box sx={{ mt: 'auto', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Stack direction="row" spacing={1}>
              {system.hasCode && (
                <Chip
                  icon={<CodeRoundedIcon sx={{ fontSize: 14 }} />}
                  label="Code"
                  size="small"
                  sx={(theme) => ({
                    bgcolor: alpha(theme.palette.primary.main, 0.1),
                    color: 'primary.main',
                    fontSize: '0.7rem',
                    height: 22,
                  })}
                />
              )}
              {system.attachmentCount > 0 && (
                <Chip
                  icon={<AttachFileRoundedIcon sx={{ fontSize: 14 }} />}
                  label={system.attachmentCount}
                  size="small"
                  sx={(theme) => ({
                    bgcolor: alpha(theme.palette.secondary.main, 0.1),
                    color: 'secondary.main',
                    fontSize: '0.7rem',
                    height: 22,
                  })}
                />
              )}
            </Stack>
            <Typography variant="caption" color="text.secondary">
              {formatDate(system.updatedAt || system.createdAt)}
            </Typography>
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
