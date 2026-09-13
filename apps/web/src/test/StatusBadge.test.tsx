import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatusBadge } from '../components/StatusBadge';
import { VERSION_STATUSES } from '../api/types';
import { statusLabel } from '../i18n/strings';

describe('StatusBadge', () => {
  it('render đủ nhãn tiếng Việt cho cả 7 trạng thái version', () => {
    for (const status of VERSION_STATUSES) {
      const { unmount } = render(<StatusBadge status={status} />);
      expect(screen.getByText(statusLabel(status))).toBeInTheDocument();
      unmount();
    }
  });

  it('bản Effective hiển thị nhãn "Đang áp dụng"', () => {
    render(<StatusBadge status="Effective" />);
    expect(screen.getByText('Đang áp dụng')).toBeInTheDocument();
  });

  it('có đúng 7 trạng thái theo state machine SDD §4.3', () => {
    expect(VERSION_STATUSES).toHaveLength(7);
  });
});
