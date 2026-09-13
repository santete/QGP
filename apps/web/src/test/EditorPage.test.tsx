import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ToastProvider } from '../components/Toast';
import type { VersionContent } from '../api/types';

const h = vi.hoisted(() => ({
  content: {
    version_id: 'v-1',
    doc_id: 'QA-PROC-010',
    version: '1.0',
    status: 'Draft',
    content_markdown: '# Nội dung cũ',
  } as VersionContent,
  getVersionContent: vi.fn(),
  updateVersionContent: vi.fn(),
  submitVersion: vi.fn(),
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getVersionContent: h.getVersionContent,
  updateVersionContent: h.updateVersionContent,
  submitVersion: h.submitVersion,
  createDocument: vi.fn(),
  listVersions: vi.fn(),
}));

import { EditorPage } from '../pages/EditorPage';

function renderEdit() {
  return render(
    <MemoryRouter initialEntries={['/editor/v-1']}>
      <ToastProvider>
        <Routes>
          <Route path="/editor/:versionId" element={<EditorPage />} />
          <Route path="/documents/:docId" element={<div>Trang tài liệu</div>} />
        </Routes>
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe('EditorPage (S7) — sửa bản nháp', () => {
  beforeEach(() => {
    h.getVersionContent.mockResolvedValue(h.content);
    h.updateVersionContent.mockResolvedValue({ ...h.content });
    h.submitVersion.mockResolvedValue({});
  });

  it('load nội dung draft vào textarea + hiển thị doc_id/version', async () => {
    renderEdit();
    const box = (await screen.findByLabelText('Nội dung (Markdown)')) as HTMLTextAreaElement;
    expect(box.value).toBe('# Nội dung cũ');
    expect(screen.getByText(/QA-PROC-010/)).toBeInTheDocument();
  });

  it('Lưu gọi updateVersionContent với nội dung đã sửa', async () => {
    renderEdit();
    const box = await screen.findByLabelText('Nội dung (Markdown)');
    fireEvent.change(box, { target: { value: '# Nội dung mới' } });
    fireEvent.click(screen.getByRole('button', { name: 'Lưu' }));
    await waitFor(() => expect(h.updateVersionContent).toHaveBeenCalledWith('v-1', '# Nội dung mới'));
  });

  it('Gửi duyệt: lưu rồi submit rồi điều hướng về trang tài liệu', async () => {
    renderEdit();
    await screen.findByLabelText('Nội dung (Markdown)');
    fireEvent.click(screen.getByRole('button', { name: 'Gửi duyệt' }));
    await waitFor(() => expect(h.submitVersion).toHaveBeenCalledWith('v-1'));
    expect(await screen.findByText('Trang tài liệu')).toBeInTheDocument();
  });
});
