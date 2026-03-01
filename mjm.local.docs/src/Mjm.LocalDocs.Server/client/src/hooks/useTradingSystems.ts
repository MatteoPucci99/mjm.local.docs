import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getTradingSystems,
  getTradingSystem,
  createTradingSystem,
  updateTradingSystem,
  updateTradingSystemStatus,
  getTradingSystemCode,
  saveTradingSystemCode,
  importTradingSystemCode,
  exportTradingSystemCode,
  getTradingSystemAttachments,
  addTradingSystemAttachment,
  removeTradingSystemAttachment,
  deleteTradingSystem,
  getTradingSystemStatuses,
} from '../api/tradingSystems';
import type { TradingSystemStatus } from '../types';

const QUERY_KEYS = {
  all: ['tradingSystems'] as const,
  list: (status?: TradingSystemStatus, search?: string) =>
    [...QUERY_KEYS.all, 'list', { status, search }] as const,
  detail: (id: string) => [...QUERY_KEYS.all, 'detail', id] as const,
  code: (id: string) => [...QUERY_KEYS.all, 'code', id] as const,
  attachments: (id: string) => [...QUERY_KEYS.all, 'attachments', id] as const,
  statuses: ['tradingSystemStatuses'] as const,
};

export function useTradingSystems(status?: TradingSystemStatus, search?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.list(status, search),
    queryFn: () => getTradingSystems(status, search),
  });
}

export function useTradingSystem(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.detail(id),
    queryFn: () => getTradingSystem(id),
    enabled: !!id,
  });
}

export function useTradingSystemCode(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.code(id),
    queryFn: () => getTradingSystemCode(id),
    enabled: !!id,
  });
}

export function useTradingSystemAttachments(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.attachments(id),
    queryFn: () => getTradingSystemAttachments(id),
    enabled: !!id,
  });
}

export function useTradingSystemStatuses() {
  return useQuery({
    queryKey: QUERY_KEYS.statuses,
    queryFn: getTradingSystemStatuses,
    staleTime: Infinity,
  });
}

export function useCreateTradingSystem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createTradingSystem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useUpdateTradingSystem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ...data }: Parameters<typeof updateTradingSystem>[1] & { id: string }) =>
      updateTradingSystem(id, data),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.setQueryData(QUERY_KEYS.detail(data.id), data);
    },
  });
}

export function useUpdateTradingSystemStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: TradingSystemStatus }) =>
      updateTradingSystemStatus(id, status),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.setQueryData(QUERY_KEYS.detail(data.id), data);
    },
  });
}

export function useSaveTradingSystemCode() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, code }: { id: string; code: string }) => saveTradingSystemCode(id, code),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.code(data.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useImportTradingSystemCode() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => importTradingSystemCode(id, file),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.code(data.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useExportTradingSystemCode() {
  return useMutation({
    mutationFn: ({ id, fileName }: { id: string; fileName: string }) =>
      exportTradingSystemCode(id, fileName),
  });
}

export function useAddTradingSystemAttachment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => addTradingSystemAttachment(id, file),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.attachments(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useRemoveTradingSystemAttachment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, attachmentId }: { id: string; attachmentId: string }) =>
      removeTradingSystemAttachment(id, attachmentId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.attachments(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useDeleteTradingSystem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: deleteTradingSystem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}
