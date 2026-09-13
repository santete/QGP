import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { AdminDocType, AdminTag, AdminUser, RbacMatrix } from '../api/types';

const h = vi.hoisted(() => ({
  rbac: {
    roles: [
      { code: 'READER', name: 'Reader' },
      { code: 'ADMIN', name: 'Admin' },
    ],
    policies: [
      { policy: 'doc.read', roles: ['READER', 'ADMIN'] },
      { policy: 'admin.config', roles: ['ADMIN'] },
    ],
  } as RbacMatrix,
  users: [{ sub: 'admin1', display_name: 'admin1', email: null, roles: ['ADMIN'] }] as AdminUser[],
  tags: [{ id: 'tag-1', slug: 'qa', name: 'QA', doc_count: 3 }] as AdminTag[],
  docTypes: [{ id: 'dt-2', code: 'Process', label: 'Process', seq: 2, active: true, doc_count: 5 }] as AdminDocType[],
}));

vi.mock('../api/client', () => ({
  getRbac: vi.fn(async (): Promise<RbacMatrix> => h.rbac),
  listAdminUsers: vi.fn(async (): Promise<AdminUser[]> => h.users),
  listTags: vi.fn(async (): Promise<AdminTag[]> => h.tags),
  createTag: vi.fn(),
  updateTag: vi.fn(),
  deleteTag: vi.fn(async (): Promise<void> => undefined),
  listDocTypes: vi.fn(async (): Promise<AdminDocType[]> => h.docTypes),
  createDocType: vi.fn(),
  updateDocType: vi.fn(),
  deleteDocType: vi.fn(),
}));

import { AdminPage } from '../pages/AdminPage';

describe('AdminPage (S17) — quản trị RBAC + taxonomy', () => {
  it('hiển thị ma trận RBAC read-only rồi chuyển tab Tags/Doc types', async () => {
    render(
      <MemoryRouter>
        <AdminPage />
      </MemoryRouter>,
    );

    // Tab RBAC mặc định: các policy §10.1.
    expect(await screen.findByText('admin.config')).toBeInTheDocument();
    expect(screen.getByText('doc.read')).toBeInTheDocument();
    expect(screen.getByText(/nguồn phân quyền thật/i)).toBeInTheDocument();

    // Chuyển sang tab Nhãn (tag).
    fireEvent.click(screen.getByRole('tab', { name: 'Nhãn (tag)' }));
    expect(await screen.findByText('qa')).toBeInTheDocument();
    expect(screen.getByText('3 tài liệu')).toBeInTheDocument();

    // Chuyển sang tab Loại tài liệu.
    fireEvent.click(screen.getByRole('tab', { name: 'Loại tài liệu' }));
    expect(await screen.findByText('Process')).toBeInTheDocument();
    expect(screen.getByText('5 tài liệu')).toBeInTheDocument();
  });
});
