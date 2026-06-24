import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SuspendConfirmDialog } from './SuspendConfirmDialog';

describe('SuspendConfirmDialog', () => {
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should not render when isOpen is false', () => {
    render(
      <SuspendConfirmDialog
        isOpen={false}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.queryByText('Suspend Realm')).not.toBeInTheDocument();
  });

  it('should render when isOpen is true', () => {
    render(
      <SuspendConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Suspend Realm')).toBeInTheDocument();
  });

  it('should display warning text', () => {
    render(
      <SuspendConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText(/Warning:/i)).toBeInTheDocument();
    expect(screen.getByText(/containers will be stopped/i)).toBeInTheDocument();
    expect(screen.getByText(/billing will be terminated/i)).toBeInTheDocument();
  });

  it('should require confirmation input', () => {
    render(
      <SuspendConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByPlaceholderText('Test Realm')).toBeInTheDocument();
  });

  it('should disable suspend button when confirmation does not match', () => {
    render(
      <SuspendConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const suspendButton = screen.getByText('Suspend');
    expect(suspendButton).toBeDisabled();
  });

  it('should enable suspend button when confirmation matches', () => {
    render(
      <SuspendConfirmDialog
        isOpen={true}
        realmId="test-id"
        realmName="Test Realm"
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const input = screen.getByPlaceholderText('Test Realm');
    fireEvent.change(input, { target: { value: 'Test Realm' } });
    const suspendButton = screen.getByText('Suspend');
    expect(suspendButton).not.toBeDisabled();
  });

  it('should call onClose when clicking overlay', () => {
    render(
      <SuspendConfirmDialog
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
      <SuspendConfirmDialog
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
});
