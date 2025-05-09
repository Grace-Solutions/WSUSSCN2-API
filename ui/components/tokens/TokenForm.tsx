import { useState } from 'react';

interface TokenFormProps {
  onSubmit: (data: any) => void;
  onCancel: () => void;
}

export default function TokenForm({ onSubmit, onCancel }: TokenFormProps) {
  const [label, setLabel] = useState('');
  const [description, setDescription] = useState('');
  const [permissions, setPermissions] = useState<string[]>(['updates:read']);
  const [expiration, setExpiration] = useState('never');
  const [customDate, setCustomDate] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    let expiresAt = null;
    if (expiration === 'custom' && customDate) {
      expiresAt = new Date(customDate).toISOString();
    } else if (expiration === '1h') {
      expiresAt = new Date(Date.now() + 60 * 60 * 1000).toISOString();
    } else if (expiration === '1d') {
      expiresAt = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
    } else if (expiration === '7d') {
      expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
    } else if (expiration === '30d') {
      expiresAt = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString();
    }

    onSubmit({
      label,
      description: description || undefined,
      permissions,
      expiresAt,
    });
  };

  const handlePermissionChange = (permission: string) => {
    if (permissions.includes(permission)) {
      setPermissions(permissions.filter(p => p !== permission));
    } else {
      setPermissions([...permissions, permission]);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label htmlFor="label" className="block text-sm font-medium mb-1">
          Label *
        </label>
        <input
          id="label"
          type="text"
          value={label}
          onChange={(e) => setLabel(e.target.value)}
          required
          className="w-full px-3 py-2 bg-background border border-input rounded-md"
          placeholder="My API Token"
        />
      </div>

      <div>
        <label htmlFor="description" className="block text-sm font-medium mb-1">
          Description
        </label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          className="w-full px-3 py-2 bg-background border border-input rounded-md"
          placeholder="Used for..."
          rows={2}
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-2">
          Permissions *
        </label>
        <div className="space-y-2">
          <div className="flex items-center">
            <input
              type="checkbox"
              id="perm-updates-read"
              checked={permissions.includes('updates:read')}
              onChange={() => handlePermissionChange('updates:read')}
              className="mr-2"
            />
            <label htmlFor="perm-updates-read" className="text-sm">
              updates:read - Access update metadata
            </label>
          </div>
          <div className="flex items-center">
            <input
              type="checkbox"
              id="perm-cabs-read"
              checked={permissions.includes('cabs:read')}
              onChange={() => handlePermissionChange('cabs:read')}
              className="mr-2"
            />
            <label htmlFor="perm-cabs-read" className="text-sm">
              cabs:read - Download CAB files
            </label>
          </div>
          <div className="flex items-center">
            <input
              type="checkbox"
              id="perm-sync-trigger"
              checked={permissions.includes('sync:trigger')}
              onChange={() => handlePermissionChange('sync:trigger')}
              className="mr-2"
            />
            <label htmlFor="perm-sync-trigger" className="text-sm">
              sync:trigger - Trigger update synchronization
            </label>
          </div>
          <div className="flex items-center">
            <input
              type="checkbox"
              id="perm-admin"
              checked={permissions.includes('admin')}
              onChange={() => handlePermissionChange('admin')}
              className="mr-2"
            />
            <label htmlFor="perm-admin" className="text-sm">
              admin - Full administrative access
            </label>
          </div>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium mb-2">
          Expiration
        </label>
        <select
          value={expiration}
          onChange={(e) => setExpiration(e.target.value)}
          className="w-full px-3 py-2 bg-background border border-input rounded-md"
        >
          <option value="never">Never</option>
          <option value="1h">1 hour</option>
          <option value="1d">1 day</option>
          <option value="7d">7 days</option>
          <option value="30d">30 days</option>
          <option value="custom">Custom date</option>
        </select>

        {expiration === 'custom' && (
          <div className="mt-2">
            <input
              type="datetime-local"
              value={customDate}
              onChange={(e) => setCustomDate(e.target.value)}
              className="w-full px-3 py-2 bg-background border border-input rounded-md"
              required
            />
          </div>
        )}
      </div>

      <div className="flex justify-end space-x-2 pt-4">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 border border-input rounded-md text-sm"
        >
          Cancel
        </button>
        <button
          type="submit"
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md text-sm"
          disabled={!label || permissions.length === 0}
        >
          Create Token
        </button>
      </div>
    </form>
  );
}
