import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { Subscription, SubscriptionStatus } from '../api/types';

const h = vi.hoisted(() => ({
  subs: [] as Subscription[],
}));

vi.mock('../api/client', () => ({
  listSubscriptions: vi.fn(async (): Promise<Subscription[]> => h.subs),
  subscribeDocument: vi.fn(async (docId: string): Promise<SubscriptionStatus> => ({ doc_id: docId, subscribed: true })),
  unsubscribeDocument: vi.fn(async (docId: string): Promise<SubscriptionStatus> => ({ doc_id: docId, subscribed: false })),
}));

import { SubscribeButton } from '../features/subscriptions/SubscribeButton';

describe('SubscribeButton (S19) — theo dõi tài liệu', () => {
  it('bắt đầu ở trạng thái Theo dõi rồi chuyển sang Đang theo dõi khi bấm', async () => {
    render(<SubscribeButton docId="QA-PROC-005" />);

    // Ban đầu chưa theo dõi.
    const btn = await screen.findByRole('button', { name: /Theo dõi/ });
    await waitFor(() => expect(btn).not.toBeDisabled());

    fireEvent.click(btn);

    // Sau khi subscribe → nhãn "Đang theo dõi".
    await waitFor(() => expect(screen.getByRole('button', { name: /Đang theo dõi/ })).toBeInTheDocument());
  });
});
