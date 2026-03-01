import apiClient from './client';
import type { TradingSystem, TradingSystemListItem, TradingSystemStatus, TradingSystemStatusInfo, TradingSystemAttachment } from '../types';

export async function getTradingSystems(
  status?: TradingSystemStatus,
  search?: string
): Promise<TradingSystemListItem[]> {
  const params: Record<string, string> = {};
  if (status) params.status = status;
  if (search) params.search = search;

  const { data } = await apiClient.get<TradingSystemListItem[]>('/tradingsystems', { params });
  return data;
}

export async function getTradingSystem(id: string): Promise<TradingSystem> {
  const { data } = await apiClient.get<TradingSystem>(`/tradingsystems/${id}`);
  return data;
}

export async function createTradingSystem(request: {
  name: string;
  description?: string;
  sourceUrl?: string;
  tags?: string[];
  notes?: string;
}): Promise<TradingSystem> {
  const { data } = await apiClient.post<TradingSystem>('/tradingsystems', request);
  return data;
}

export async function updateTradingSystem(
  id: string,
  request: {
    name: string;
    description?: string;
    sourceUrl?: string;
    tags?: string[];
    notes?: string;
  }
): Promise<TradingSystem> {
  const { data } = await apiClient.put<TradingSystem>(`/tradingsystems/${id}`, request);
  return data;
}

export async function updateTradingSystemStatus(
  id: string,
  status: TradingSystemStatus
): Promise<TradingSystem> {
  const { data } = await apiClient.patch<TradingSystem>(`/tradingsystems/${id}/status`, { status });
  return data;
}

export async function getTradingSystemCode(id: string): Promise<string> {
  const { data } = await apiClient.get<{ code: string }>(`/tradingsystems/${id}/code`);
  return data.code;
}

export async function saveTradingSystemCode(id: string, code: string): Promise<TradingSystem> {
  const { data } = await apiClient.put<TradingSystem>(`/tradingsystems/${id}/code`, { code });
  return data;
}

export async function importTradingSystemCode(id: string, file: File): Promise<TradingSystem> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<TradingSystem>(`/tradingsystems/${id}/code/import`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function exportTradingSystemCode(id: string, fileName: string): Promise<void> {
  const response = await apiClient.get(`/tradingsystems/${id}/code/export`, { responseType: 'blob' });
  const url = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', fileName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function getTradingSystemAttachments(id: string): Promise<TradingSystemAttachment[]> {
  const { data } = await apiClient.get<TradingSystemAttachment[]>(`/tradingsystems/${id}/attachments`);
  return data;
}

export async function addTradingSystemAttachment(id: string, file: File): Promise<TradingSystemAttachment> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<TradingSystemAttachment>(`/tradingsystems/${id}/attachments`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function removeTradingSystemAttachment(id: string, attachmentId: string): Promise<void> {
  await apiClient.delete(`/tradingsystems/${id}/attachments/${attachmentId}`);
}

export function getAttachmentFileUrl(tradingSystemId: string, attachmentId: string): string {
  return `/api/tradingsystems/${tradingSystemId}/attachments/${attachmentId}/file`;
}

export async function downloadAttachmentFile(tradingSystemId: string, attachmentId: string, fileName: string): Promise<void> {
  const response = await apiClient.get(`/tradingsystems/${tradingSystemId}/attachments/${attachmentId}/file`, { responseType: 'blob' });
  const url = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', fileName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function deleteTradingSystem(id: string): Promise<void> {
  await apiClient.delete(`/tradingsystems/${id}`);
}

export async function getTradingSystemStatuses(): Promise<TradingSystemStatusInfo[]> {
  const { data } = await apiClient.get<TradingSystemStatusInfo[]>('/tradingsystems/statuses');
  return data;
}
