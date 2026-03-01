import { useState, useCallback } from 'react';
import Editor from '@monaco-editor/react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import { alpha, useTheme } from '@mui/material/styles';
import UploadFileRoundedIcon from '@mui/icons-material/UploadFileRounded';

interface CodeEditorProps {
  value: string;
  onChange: (value: string) => void;
  readOnly?: boolean;
  height?: string | number;
}

const ACCEPTED_EXTENSIONS = ['.el', '.eld', '.md', '.txt'];

export default function CodeEditor({ value, onChange, readOnly = false, height = '500px' }: CodeEditorProps) {
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const [isDragging, setIsDragging] = useState(false);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (readOnly) return;
    const items = e.dataTransfer.items;
    if (items.length > 0 && items[0].kind === 'file') {
      setIsDragging(true);
    }
  }, [readOnly]);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const { clientX, clientY } = e;
    if (clientX < rect.left || clientX > rect.right || clientY < rect.top || clientY > rect.bottom) {
      setIsDragging(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
    if (readOnly) return;

    const file = e.dataTransfer.files[0];
    if (!file) return;

    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ACCEPTED_EXTENSIONS.includes(ext)) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result;
      if (typeof text === 'string') {
        onChange(text);
      }
    };
    reader.readAsText(file);
  }, [readOnly, onChange]);

  return (
    <Box
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      sx={{
        position: 'relative',
        border: 1,
        borderColor: isDragging ? 'primary.main' : 'divider',
        borderRadius: 1,
        overflow: 'hidden',
        transition: 'border-color 0.2s',
      }}
    >
      <Editor
        height={height}
        defaultLanguage="pascal"
        theme={isDark ? 'vs-dark' : 'vs'}
        value={value}
        onChange={(v) => onChange(v ?? '')}
        loading={
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
            <CircularProgress size={32} />
          </Box>
        }
        options={{
          readOnly,
          minimap: { enabled: false },
          fontSize: 14,
          lineNumbers: 'on',
          scrollBeyondLastLine: false,
          wordWrap: 'on',
          automaticLayout: true,
          tabSize: 4,
          insertSpaces: true,
          renderWhitespace: 'selection',
          bracketPairColorization: { enabled: true },
        }}
      />

      {/* Drop overlay */}
      {isDragging && (
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: alpha(theme.palette.primary.main, 0.08),
            backdropFilter: 'blur(4px)',
            border: '2px dashed',
            borderColor: 'primary.main',
            borderRadius: 1,
            zIndex: 10,
            pointerEvents: 'none',
          }}
        >
          <UploadFileRoundedIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
          <Typography variant="subtitle1" fontWeight={600} color="primary.main">
            Drop file to replace code
          </Typography>
          <Typography variant="body2" color="text.secondary">
            .el, .eld, .md, .txt
          </Typography>
        </Box>
      )}
    </Box>
  );
}
