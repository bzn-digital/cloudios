import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ReactivateConfirmDialog } from './ReactivateConfirmDialog';

describe('ReactivateConfirmDialog', () => {
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should not render when isOpen is false', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={false}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.queryByText('Reactivate Realm')).not.toBeInTheDocument();
  });

  it('should render when isOpen is true', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Reactivate Realm')).toBeInTheDocument();
  });

  it('should display realm name in confirmation text', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Test Realm')).toBeInTheDocument();
  });

  it('should display info text about consequences', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText(/restore access to all resources/i)).toBeInTheDocument();
    expect(screen.getByText(/resume billing/i)).toBeInTheDocument();
  });

  it('should call onClose when clicking overlay', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
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
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const cancelButton = screen.getByText('Cancel');
    fireEvent.click(cancelButton);
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('should have Reactivate button', () => {
    render(
      <ReactivateConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Reactivate')).toBeInTheDocument();
  });
});
