import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { ToastProvider, useToast } from '../../contexts/ToastContext';

describe('ToastContext', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should provide toast functions', () => {
    const TestComponent = () => {
      const { showToast, removeToast } = useToast();
      return (
        <div>
          <button onClick={() => showToast('success', 'Test message')}>Show Toast</button>
          <button onClick={() => removeToast('123')}>Remove Toast</button>
        </div>
      );
    };

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    expect(screen.getByText('Show Toast')).toBeInTheDocument();
    expect(screen.getByText('Remove Toast')).toBeInTheDocument();
  });

  it('should display toast when showToast is called', () => {
    const TestComponent = () => {
      const { showToast } = useToast();
      return (
        <button onClick={() => showToast('success', 'Test message')}>Show Toast</button>
      );
    };

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Test message')).toBeInTheDocument();
  });

  it('should remove toast when removeToast is called', () => {
    const TestComponent = () => {
      const { showToast, removeToast } = useToast();
      return (
        <div>
          <button onClick={() => showToast('success', 'Test message')}>Show Toast</button>
          <button onClick={() => removeToast('1')}>Remove Toast</button>
        </div>
      );
    };

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Test message')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Remove Toast'));
    expect(screen.queryByText('Test message')).not.toBeInTheDocument();
  });

  it('should auto-remove toast after duration', async () => {
    const TestComponent = () => {
      const { showToast } = useToast();
      return (
        <button onClick={() => showToast('success', 'Test message', 1000)}>Show Toast</button>
      );
    };

    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Test message')).toBeInTheDocument();

    vi.advanceTimersByTime(1000);
    await waitFor(() => {
      expect(screen.queryByText('Test message')).not.toBeInTheDocument();
    });
  });

  it('should throw error when useToast is used outside provider', () => {
    const TestComponent = () => {
      const { showToast } = useToast();
      return <button onClick={() => showToast('success', 'Test')}>Show</button>;
    };

    expect(() => render(<TestComponent />)).toThrow('useToast must be used within a ToastProvider');
  });
});
