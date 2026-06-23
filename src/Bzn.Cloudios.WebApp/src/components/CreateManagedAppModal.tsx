import { useState, useEffect } from 'react';
import { Modal } from './Modal';
import { apiClient } from '../lib/api';
import type { ManagedAppTemplate, ManagedAppTemplateListResponse } from '../types/managedApp';

const INSTANCE_SIZES = [
  { key: 'Nano1s', label: 'Nano 1s', cpu: '0.25 vCPU', ram: '256 MB', costPerHour: 0.01 },
  { key: 'Micro1s', label: 'Micro 1s', cpu: '0.5 vCPU', ram: '512 MB', costPerHour: 0.02 },
  { key: 'Small1s', label: 'Small 1s', cpu: '1 vCPU', ram: '1 GB', costPerHour: 0.04 },
  { key: 'Medium1s', label: 'Medium 1s', cpu: '2 vCPU', ram: '2 GB', costPerHour: 0.08 },
  { key: 'Large1s', label: 'Large 1s', cpu: '4 vCPU', ram: '4 GB', costPerHour: 0.16 },
];

interface CreateManagedAppModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTemplateSelected: (template: ManagedAppTemplate) => void;
  onAppCreated?: () => void;
}

export function CreateManagedAppModal({ isOpen, onClose, onTemplateSelected, onAppCreated }: CreateManagedAppModalProps) {
  const [step, setStep] = useState<1 | 2>(1);
  const [templates, setTemplates] = useState<ManagedAppTemplate[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedTemplate, setSelectedTemplate] = useState<ManagedAppTemplate | null>(null);
  
  // Step 2 form state
  const [formData, setFormData] = useState({
    name: '',
    instanceSize: '',
  });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

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
      // Reset form state for step 2
      setFormData({
        name: '',
        instanceSize: selectedTemplate.defaultInstanceSize || 'Nano1s',
      });
      setFormErrors({});
    }
  };

  const handleBack = () => {
    setStep(1);
  };

  const handleCancel = () => {
    setSelectedTemplate(null);
    setSearchQuery('');
    setSelectedCategory('All');
    setStep(1);
    setFormData({
      name: '',
      instanceSize: '',
    });
    setFormErrors({});
    onClose();
  };

  const handleFormChange = (field: string, value: string | number) => {
    setFormData({ ...formData, [field]: value });
    // Clear error for this field when user starts typing
    if (formErrors[field]) {
      setFormErrors({ ...formErrors, [field]: '' });
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Instance name is required';
    } else if (!/^[a-z0-9-]+$/.test(formData.name)) {
      newErrors.name = 'Only lowercase letters, numbers, and hyphens allowed';
    }

    if (!formData.instanceSize) {
      newErrors.instanceSize = 'Instance size is required';
    }

    return newErrors;
  };

  const handleSubmit = async () => {
    const errors = validateForm();
    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      return;
    }

    if (!selectedTemplate) return;

    try {
      setSubmitting(true);
      setFormErrors({});
      
      await apiClient.createManagedApp({
        templateId: selectedTemplate.id,
        name: formData.name,
        instanceSize: formData.instanceSize,
      });

      // Success
      handleCancel();
      if (onAppCreated) {
        onAppCreated();
      }
    } catch (err: any) {
      console.error('Failed to create app:', err);
      const errorMessage = err.message || 'Failed to create app';
      if (errorMessage.includes('already exists') || errorMessage.includes('duplicate')) {
        setFormErrors({ name: 'Name already exists in this realm' });
      } else {
        setFormErrors({ submit: errorMessage });
      }
    } finally {
      setSubmitting(false);
    }
  };

  const selectedInstanceSize = INSTANCE_SIZES.find(size => size.key === formData.instanceSize);
  const costPerHour = selectedInstanceSize ? selectedInstanceSize.costPerHour : 0;
  const totalCostPerMonth = costPerHour * 24 * 30;

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
                            if (img.dataset.fallback) return;
                            img.dataset.fallback = '1';
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

      {step === 2 && selectedTemplate && (
        <div className="create-app-modal">
          <div className="selected-app-header">
            <div className="app-icon">
              <img
                src={selectedTemplate.iconUrl}
                alt={selectedTemplate.displayName}
                onError={(e) => {
                  (e.target as HTMLImageElement).src = '/assets/default-app-icon.svg';
                }}
              />
            </div>
            <div className="app-info">
              <h3>{selectedTemplate.displayName}</h3>
              <p>{selectedTemplate.description}</p>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="instanceName">Instance Name *</label>
            <input
              id="instanceName"
              type="text"
              value={formData.name}
              onChange={(e) => handleFormChange('name', e.target.value)}
              placeholder="my-app"
              className={formErrors.name ? 'modal-input error' : 'modal-input'}
            />
            {formErrors.name && <small className="error-text">{formErrors.name}</small>}
            <small>Only lowercase letters, numbers, and hyphens (e.g., my-app)</small>
          </div>

          <div className="form-group">
            <label>Instance Size *</label>
            <div className="instance-sizes-grid">
              {INSTANCE_SIZES.map((size) => (
                <div
                  key={size.key}
                  className={`instance-size-card ${formData.instanceSize === size.key ? 'selected' : ''}`}
                  onClick={() => handleFormChange('instanceSize', size.key)}
                >
                  <div className="size-label">{size.label}</div>
                  <div className="size-specs">
                    <div>{size.cpu}</div>
                    <div>{size.ram}</div>
                  </div>
                  <div className="size-cost">R$ {size.costPerHour.toFixed(3)}/hour</div>
                </div>
              ))}
            </div>
            {formErrors.instanceSize && <small className="error-text">{formErrors.instanceSize}</small>}
          </div>


          <div className="billing-preview">
            <h3>Billing Preview</h3>
            {selectedInstanceSize ? (
              <div className="billing-costs">
                <div className="cost-item">
                  <span className="cost-label">Instance size (per hour)</span>
                  <span className="cost-value">R$ {costPerHour.toFixed(2)}</span>
                </div>
                <div className="cost-item total">
                  <span className="cost-label">Total per month (720h)</span>
                  <span className="cost-value">R$ {totalCostPerMonth.toFixed(2)}</span>
                </div>
              </div>
            ) : (
              <p className="billing-placeholder">Select an instance size to see pricing</p>
            )}
          </div>

          {formErrors.submit && <div className="error-text submit-error">{formErrors.submit}</div>}

          <div className="modal-footer">
            <button className="btn btn-secondary" onClick={handleBack}>
              Back
            </button>
            <button
              className="btn btn-primary"
              onClick={handleSubmit}
              disabled={submitting}
            >
              {submitting ? 'Creating...' : 'Create App'}
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}
