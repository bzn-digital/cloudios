import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { RealmQuotaEditor } from './RealmQuotaEditor';

describe('RealmQuotaEditor', () => {
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();
  const mockQuotas = {
    maxContainers: 10,
    maxDatabases: 5,
    maxManagedApps: 3,
    maxRamBytes: 8589934592, // 8GB
    maxCpuCores: 4,
  };
  const mockUsage = {
    containersCount: 7,
    databasesCount: 3,
    managedAppsCount: 2,
    ramBytesUsed: 4294967296, // 4GB
    cpuCoresUsed: 2,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render modal', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Edit Realm Quotas')).toBeInTheDocument();
  });

  it('should display all quota fields', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByLabelText('Max Containers')).toBeInTheDocument();
    expect(screen.getByLabelText('Max Databases')).toBeInTheDocument();
    expect(screen.getByLabelText('Max Managed Apps')).toBeInTheDocument();
    expect(screen.getByLabelText('Max RAM (GB)')).toBeInTheDocument();
    expect(screen.getByLabelText('Max CPU Cores')).toBeInTheDocument();
  });

  it('should display current quota values', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByDisplayValue('10')).toBeInTheDocument();
    expect(screen.getByDisplayValue('5')).toBeInTheDocument();
    expect(screen.getByDisplayValue('3')).toBeInTheDocument();
    expect(screen.getByDisplayValue('8.00')).toBeInTheDocument();
    expect(screen.getByDisplayValue('4')).toBeInTheDocument();
  });

  it('should display usage progress bars', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('7 / 10')).toBeInTheDocument();
    expect(screen.getByText('3 / 5')).toBeInTheDocument();
    expect(screen.getByText('2 / 3')).toBeInTheDocument();
  });

  it('should call onClose when clicking overlay', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const overlay = document.querySelector('.modal-overlay');
    if (overlay) {
      fireEvent.click(overlay);
      expect(mockOnClose).toHaveBeenCalled();
    }
  });

  it('should close modal on cancel button click', () => {
    render(
      <RealmQuotaEditor
        realmId="test-id"
        quotas={mockQuotas}
        usage={mockUsage}
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const cancelButton = screen.getByText('Cancel');
    fireEvent.click(cancelButton);
    expect(mockOnClose).toHaveBeenCalled();
  });
});
