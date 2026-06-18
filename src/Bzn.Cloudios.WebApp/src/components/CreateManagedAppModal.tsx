import { useState, useEffect } from 'react';
import { Modal } from './Modal';
import { apiClient } from '../lib/api';
import type { ManagedAppTemplate, ManagedAppTemplateListResponse } from '../types/managedApp';

interface CreateManagedAppModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTemplateSelected: (template: ManagedAppTemplate) => void;
}

export function CreateManagedAppModal({ isOpen, onClose, onTemplateSelected }: CreateManagedAppModalProps) {
  const [step, setStep] = useState<1 | 2>(1);
  const [templates, setTemplates] = useState<ManagedAppTemplate[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedTemplate, setSelectedTemplate] = useState<ManagedAppTemplate | null>(null);

  useEffect(() => {
    if (isOpen && step === 1) {
      loadTemplates();
    }
  }, [isOpen, step]);

  const loadTemplates = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiClient.getManagedAppTemplates() as ManagedAppTemplateListResponse;
      setTemplates(data.items || []);
      setCategories(data.categories || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setLoading(false);
    }
  };

  const filteredTemplates = templates.filter(template => {
    const matchesSearch = template.displayName.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory = selectedCategory === 'All' || template.category === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  const handleTemplateSelect = (template: ManagedAppTemplate) => {
    setSelectedTemplate(template);
  };

  const handleNext = () => {
    if (selectedTemplate) {
      onTemplateSelected(selectedTemplate);
      setStep(2);
    }
  };

  const handleCancel = () => {
    setSelectedTemplate(null);
    setSearchQuery('');
    setSelectedCategory('All');
    setStep(1);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleCancel} title="Create Managed App">
      {step === 1 && (
        <div className="create-app-modal">
          {loading ? (
            <div className="loading-state">Loading templates...</div>
          ) : error ? (
            <div className="error-state">{error}</div>
          ) : (
            <>
              <div className="modal-filters">
                <input
                  type="text"
                  placeholder="Search apps..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="search-input"
                />
                <div className="category-pills">
                  <button
                    className={`category-pill ${selectedCategory === 'All' ? 'active' : ''}`}
                    onClick={() => setSelectedCategory('All')}
                  >
                    All
                  </button>
                  {categories.map(category => (
                    <button
                      key={category}
                      className={`category-pill ${selectedCategory === category ? 'active' : ''}`}
                      onClick={() => setSelectedCategory(category)}
                    >
                      {category}
                    </button>
                  ))}
                </div>
              </div>

              <div className="templates-grid">
                {filteredTemplates.length === 0 ? (
                  <div className="empty-state">No templates found matching your criteria.</div>
                ) : (
                  filteredTemplates.map(template => (
                    <div
                      key={template.id}
                      className={`template-card ${selectedTemplate?.id === template.id ? 'selected' : ''}`}
                      onClick={() => handleTemplateSelect(template)}
                    >
                      <div className="template-icon">
                        <img
                          src={template.iconUrl}
                          alt={template.displayName}
                          onError={(e) => {
                            const img = e.target as HTMLImageElement;
                            img.onerror = null;
                            img.src = '/assets/default-app-icon.svg';
                          }}
                        />
                      </div>
                      <div className="template-info">
                        <h3>{template.displayName}</h3>
                        <p>{template.description}</p>
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div className="modal-footer">
                <button className="btn btn-secondary" onClick={handleCancel}>
                  Cancel
                </button>
                <button
                  className="btn btn-primary"
                  onClick={handleNext}
                  disabled={!selectedTemplate}
                >
                  Next
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {step === 2 && (
        <div className="create-app-modal">
          <p>Step 2 will be implemented in a future issue.</p>
          <div className="modal-footer">
            <button className="btn btn-secondary" onClick={handleCancel}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}
