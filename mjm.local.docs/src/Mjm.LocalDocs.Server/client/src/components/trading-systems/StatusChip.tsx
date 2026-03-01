import Chip from '@mui/material/Chip';
import type { ChipProps } from '@mui/material/Chip';
import EditNoteRoundedIcon from '@mui/icons-material/EditNoteRounded';
import BarChartRoundedIcon from '@mui/icons-material/BarChartRounded';
import HourglassEmptyRoundedIcon from '@mui/icons-material/HourglassEmptyRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import RocketLaunchRoundedIcon from '@mui/icons-material/RocketLaunchRounded';
import PauseCircleRoundedIcon from '@mui/icons-material/PauseCircleRounded';
import ArchiveRoundedIcon from '@mui/icons-material/ArchiveRounded';
import type { TradingSystemStatus } from '../../types';

interface StatusChipProps {
  status: TradingSystemStatus;
  size?: ChipProps['size'];
  onClick?: () => void;
}

const statusConfig: Record<
  TradingSystemStatus,
  { label: string; color: ChipProps['color']; icon: React.ReactElement }
> = {
  Draft: {
    label: 'Draft',
    color: 'default',
    icon: <EditNoteRoundedIcon fontSize="small" />,
  },
  Backtesting: {
    label: 'Backtesting',
    color: 'info',
    icon: <BarChartRoundedIcon fontSize="small" />,
  },
  Validating: {
    label: 'Validating',
    color: 'warning',
    icon: <HourglassEmptyRoundedIcon fontSize="small" />,
  },
  Validated: {
    label: 'Validated',
    color: 'success',
    icon: <CheckCircleRoundedIcon fontSize="small" />,
  },
  Live: {
    label: 'Live',
    color: 'secondary',
    icon: <RocketLaunchRoundedIcon fontSize="small" />,
  },
  Paused: {
    label: 'Paused',
    color: 'warning',
    icon: <PauseCircleRoundedIcon fontSize="small" />,
  },
  Archived: {
    label: 'Archived',
    color: 'default',
    icon: <ArchiveRoundedIcon fontSize="small" />,
  },
};

export default function StatusChip({ status, size = 'small', onClick }: StatusChipProps) {
  const config = statusConfig[status] || statusConfig.Draft;

  return (
    <Chip
      label={config.label}
      color={config.color}
      size={size}
      icon={config.icon}
      onClick={onClick}
      sx={{
        fontWeight: 500,
        ...(onClick && { cursor: 'pointer' }),
      }}
    />
  );
}

export function getStatusLabel(status: TradingSystemStatus): string {
  return statusConfig[status]?.label || status;
}

export function getStatusOptions(): { value: TradingSystemStatus; label: string }[] {
  return Object.entries(statusConfig).map(([value, config]) => ({
    value: value as TradingSystemStatus,
    label: config.label,
  }));
}
