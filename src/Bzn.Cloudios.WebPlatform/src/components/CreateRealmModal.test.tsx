import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { CreateRealmModal } from './CreateRealmModal';

describe('CreateRealmModal', () => {
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should not render when isOpen is false', () => {
    render(
      <CreateRealmModal
        isOpen={false}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.queryByText('Create New Realm')).not.toBeInTheDocument();
  });

  it('should render when isOpen is true', () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByText('Create New Realm')).toBeInTheDocument();
  });

  it('should auto-generate slug from name', () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const nameInput = screen.getByLabelText('Realm Name');
    fireEvent.change(nameInput, { target: { value: 'My Test Realm' } });
    expect(screen.getByDisplayValue('my-test-realm')).toBeInTheDocument();
  });

  it('should call onClose when clicking overlay', () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const overlay = screen.getByTestId('modal-overlay') || document.querySelector('.modal-overlay');
    if (overlay) {
      fireEvent.click(overlay);
      expect(mockOnClose).toHaveBeenCalled();
    }
  });

  it('should close modal on cancel button click', () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const cancelButton = screen.getByText('Cancel');
    fireEvent.click(cancelButton);
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('should validate required fields', async () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    const createButton = screen.getByText('Create');
    fireEvent.click(createButton);
    await waitFor(() => {
      expect(screen.getByText('All fields are required')).toBeInTheDocument();
    });
  });

  it('should have all required form fields', () => {
    render(
      <CreateRealmModal
        isOpen={true}
        onClose={mockOnClose}
        onSuccess={mockOnSuccess}
      />
    );
    expect(screen.getByLabelText('Realm Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Slug')).toBeInTheDocument();
    expect(screen.getByLabelText('Owner Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Owner Password')).toBeInTheDocument();
  });
});
