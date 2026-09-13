import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, _internal, getDocument } from '../api/client';

// Ở test mode, .env.development không nạp → VITE_USE_MOCK undefined → client gọi fetch thật.
// Ta stub fetch để kiểm tra mapping lỗi HTTP → ApiError(code).

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('api client', () => {
  it('map lỗi HTTP thành ApiError mang error.code từ body', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        text: async () =>
          JSON.stringify({ error: { code: 'DOCUMENT_NOT_FOUND', message: 'Không tìm thấy' } }),
      })),
    );

    await expect(getDocument('QA-PROC-999')).rejects.toMatchObject({
      name: 'ApiError',
      status: 404,
      code: 'DOCUMENT_NOT_FOUND',
    });
  });

  it('ApiError giữ status/code/message', () => {
    const e = new ApiError(403, 'FORBIDDEN', 'Sai quyền');
    expect(e).toBeInstanceOf(Error);
    expect(e.status).toBe(403);
    expect(e.code).toBe('FORBIDDEN');
    expect(e.message).toBe('Sai quyền');
  });

  it('newIdempotencyKey sinh chuỗi dạng UUID v4', () => {
    const key = _internal.newIdempotencyKey();
    expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
  });
});
