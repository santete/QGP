import { useCallback, useEffect, useState } from 'react';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { ErrorState } from '../components/ErrorState';
import { EmptyState } from '../components/EmptyState';
import * as api from '../api/client';
import type { RbacMatrix, AdminUser, AdminTag, AdminDocType } from '../api/types';

type Tab = 'rbac' | 'users' | 'tags' | 'doctypes';

const TABS: { id: Tab; label: string }[] = [
  { id: 'rbac', label: 'Phân quyền' },
  { id: 'users', label: 'Người dùng' },
  { id: 'tags', label: 'Nhãn (tag)' },
  { id: 'doctypes', label: 'Loại tài liệu' },
];

/** Quản trị hệ thống S17 (ADM-F-01 RBAC read · ADM-F-02 taxonomy). RBAC admin.config. */
export function AdminPage() {
  const [tab, setTab] = useState<Tab>('rbac');
  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">Quản trị</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">
        Cấu hình phân quyền &amp; taxonomy (ADM-F-01/02, S17).
      </p>

      <div role="tablist" className="mb-5 flex flex-wrap gap-1 border-b border-grey-300/60">
        {TABS.map((t) => (
          <button
            key={t.id}
            role="tab"
            aria-selected={tab === t.id}
            onClick={() => setTab(t.id)}
            className={
              '-mb-px border-b-2 px-3 py-2 text-sm font-semibold transition-colors ' +
              (tab === t.id
                ? 'border-primary text-primary'
                : 'border-transparent text-text-secondary hover:text-primary')
            }
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'rbac' && <RbacTab />}
      {tab === 'users' && <UsersTab />}
      {tab === 'tags' && <TagsTab />}
      {tab === 'doctypes' && <DocTypesTab />}
    </div>
  );
}

function errMessage(e: unknown): string {
  return e instanceof Error ? e.message : 'Đã xảy ra lỗi';
}

// ── Tab: RBAC (chỉ đọc) ────────────────────────────────────────────
function RbacTab() {
  const [data, setData] = useState<RbacMatrix | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    api.getRbac(ctrl.signal).then(setData).catch((e) => {
      if (!ctrl.signal.aborted) setError(errMessage(e));
    });
    return () => ctrl.abort();
  }, []);

  if (error) return <Card className="p-4"><ErrorState message={error} /></Card>;
  if (!data) return <Skeleton className="h-40 w-full" />;

  return (
    <Card className="p-4">
      <p className="mb-3 text-xs italic text-text-secondary">
        Ma trận §10.1 chỉ để xem. Gán vai trò cho người dùng được thực hiện ở IdP (Keycloak) — nguồn phân quyền thật.
      </p>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[560px] border-collapse text-sm">
          <thead>
            <tr className="border-b border-grey-300/60 text-left">
              <th className="py-2 pr-3 font-bold text-text-secondary">Quyền</th>
              {data.roles.map((r) => (
                <th key={r.code} className="px-2 py-2 text-center font-bold text-text-secondary" title={r.name}>
                  {r.code}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.policies.map((p) => (
              <tr key={p.policy} className="border-b border-grey-300/30">
                <td className="py-2 pr-3 font-mono text-text-primary">{p.policy}</td>
                {data.roles.map((r) => (
                  <td key={r.code} className="px-2 py-2 text-center">
                    {p.roles.includes(r.code) ? (
                      <span className="text-primary" aria-label="cho phép">✓</span>
                    ) : (
                      <span className="text-grey-300" aria-hidden="true">·</span>
                    )}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

// ── Tab: Users (chỉ đọc, dữ liệu từ lớp chiếu B0) ──────────────────
function UsersTab() {
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    api.listAdminUsers(200, ctrl.signal).then(setUsers).catch((e) => {
      if (!ctrl.signal.aborted) setError(errMessage(e));
    });
    return () => ctrl.abort();
  }, []);

  if (error) return <Card className="p-4"><ErrorState message={error} /></Card>;
  if (!users) return <Skeleton className="h-40 w-full" />;
  if (users.length === 0) return <EmptyState title="Chưa có người dùng nào đăng nhập" icon="👤" />;

  return (
    <Card className="p-4">
      <p className="mb-3 text-xs italic text-text-secondary">
        Chỉ hiển thị người đã đăng nhập (đồng bộ từ Keycloak lúc /me). Đổi vai trò làm ở Keycloak.
      </p>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[520px] border-collapse text-sm">
          <thead>
            <tr className="border-b border-grey-300/60 text-left">
              <th className="py-2 pr-3 font-bold text-text-secondary">Tài khoản (sub)</th>
              <th className="py-2 pr-3 font-bold text-text-secondary">Tên</th>
              <th className="py-2 font-bold text-text-secondary">Vai trò</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.sub} className="border-b border-grey-300/30">
                <td className="py-2 pr-3 font-mono text-text-primary">{u.sub}</td>
                <td className="py-2 pr-3 text-text-secondary">{u.display_name ?? '—'}</td>
                <td className="py-2">
                  <div className="flex flex-wrap gap-1">
                    {u.roles.length === 0 ? (
                      <span className="text-text-secondary">—</span>
                    ) : (
                      u.roles.map((r) => <Badge key={r} color="primary">{r}</Badge>)
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

// ── Tab: Tags CRUD ─────────────────────────────────────────────────
function TagsTab() {
  const [tags, setTags] = useState<AdminTag[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [slug, setSlug] = useState('');
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    setError(null);
    api.listTags().then(setTags).catch((e) => setError(errMessage(e)));
  }, []);
  useEffect(load, [load]);

  async function add() {
    if (!slug.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await api.createTag(slug.trim(), name.trim() || slug.trim());
      setSlug('');
      setName('');
      load();
    } catch (e) {
      setError(errMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function remove(id: string) {
    setError(null);
    try {
      await api.deleteTag(id);
      load();
    } catch (e) {
      setError(errMessage(e));
    }
  }

  return (
    <Card className="p-4">
      <div className="mb-4 flex flex-wrap items-end gap-2">
        <label className="flex flex-col text-xs font-semibold text-text-secondary">
          Slug
          <input value={slug} onChange={(e) => setSlug(e.target.value)} className="mt-1 h-9 rounded-md border border-grey-300 px-2 text-sm text-text-primary" placeholder="vd: quality-gate" />
        </label>
        <label className="flex flex-col text-xs font-semibold text-text-secondary">
          Tên hiển thị
          <input value={name} onChange={(e) => setName(e.target.value)} className="mt-1 h-9 rounded-md border border-grey-300 px-2 text-sm text-text-primary" placeholder="vd: Quality Gate" />
        </label>
        <Button size="sm" onClick={add} loading={busy} disabled={!slug.trim()}>Thêm tag</Button>
      </div>

      {error && <div className="mb-3"><ErrorState message={error} /></div>}
      {!tags ? (
        <Skeleton className="h-32 w-full" />
      ) : tags.length === 0 ? (
        <EmptyState title="Chưa có tag" icon="🏷️" />
      ) : (
        <ul className="divide-y divide-grey-300/40">
          {tags.map((t) => (
            <li key={t.id} className="flex items-center justify-between gap-2 py-2">
              <div className="min-w-0">
                <span className="font-mono text-sm text-text-primary">{t.slug}</span>
                <span className="ml-2 text-sm text-text-secondary">{t.name}</span>
              </div>
              <div className="flex items-center gap-2">
                <Badge color={t.doc_count > 0 ? 'info' : 'default'}>{t.doc_count} tài liệu</Badge>
                <Button size="sm" variant="text" onClick={() => remove(t.id)}>Xoá</Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

// ── Tab: Doc types CRUD (B1 chiều sâu) ─────────────────────────────
function DocTypesTab() {
  const [types, setTypes] = useState<AdminDocType[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [label, setLabel] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    setError(null);
    api.listDocTypes().then(setTypes).catch((e) => setError(errMessage(e)));
  }, []);
  useEffect(load, [load]);

  async function add() {
    if (!code.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await api.createDocType(code.trim(), label.trim() || undefined);
      setCode('');
      setLabel('');
      load();
    } catch (e) {
      setError(errMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function toggleActive(t: AdminDocType) {
    setError(null);
    try {
      await api.updateDocType(t.id, { active: !t.active });
      load();
    } catch (e) {
      setError(errMessage(e));
    }
  }

  async function remove(id: string) {
    setError(null);
    try {
      await api.deleteDocType(id);
      load();
    } catch (e) {
      setError(errMessage(e));
    }
  }

  return (
    <Card className="p-4">
      <div className="mb-4 flex flex-wrap items-end gap-2">
        <label className="flex flex-col text-xs font-semibold text-text-secondary">
          Mã (code)
          <input value={code} onChange={(e) => setCode(e.target.value)} className="mt-1 h-9 rounded-md border border-grey-300 px-2 text-sm text-text-primary" placeholder="vd: Guideline" />
        </label>
        <label className="flex flex-col text-xs font-semibold text-text-secondary">
          Nhãn hiển thị
          <input value={label} onChange={(e) => setLabel(e.target.value)} className="mt-1 h-9 rounded-md border border-grey-300 px-2 text-sm text-text-primary" placeholder="vd: Hướng dẫn" />
        </label>
        <Button size="sm" onClick={add} loading={busy} disabled={!code.trim()}>Thêm loại</Button>
      </div>

      {error && <div className="mb-3"><ErrorState message={error} /></div>}
      {!types ? (
        <Skeleton className="h-32 w-full" />
      ) : types.length === 0 ? (
        <EmptyState title="Chưa có loại tài liệu" icon="📁" />
      ) : (
        <ul className="divide-y divide-grey-300/40">
          {types.map((t) => (
            <li key={t.id} className="flex items-center justify-between gap-2 py-2">
              <div className="min-w-0">
                <span className="font-mono text-sm text-text-primary">{t.code}</span>
                {t.label !== t.code && <span className="ml-2 text-sm text-text-secondary">{t.label}</span>}
                {!t.active && <Badge color="warning" className="ml-2">ẩn</Badge>}
              </div>
              <div className="flex items-center gap-2">
                <Badge color={t.doc_count > 0 ? 'info' : 'default'}>{t.doc_count} tài liệu</Badge>
                <Button size="sm" variant="text" onClick={() => toggleActive(t)}>{t.active ? 'Ẩn' : 'Hiện'}</Button>
                <Button size="sm" variant="text" onClick={() => remove(t.id)}>Xoá</Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
