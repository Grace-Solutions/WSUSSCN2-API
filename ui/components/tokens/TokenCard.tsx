import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';

interface Token {
  id: string;
  token: string;
  label: string;
  description?: string;
  permissions: string[];
  createdAt: string;
  expiresAt?: string;
  revoked: boolean;
}

interface TokenCardProps {
  token: Token;
  onRevoke: () => void;
}

export default function TokenCard({ token, onRevoke }: TokenCardProps) {
  const [showToken, setShowToken] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleCopyToken = () => {
    navigator.clipboard.writeText(token.token);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return formatDistanceToNow(date, { addSuffix: true });
  };

  const getStatusColor = () => {
    if (token.revoked) return 'text-red-500';
    if (token.expiresAt && new Date(token.expiresAt) < new Date()) return 'text-yellow-500';
    return 'text-green-500';
  };

  const getStatusText = () => {
    if (token.revoked) return 'Revoked';
    if (token.expiresAt && new Date(token.expiresAt) < new Date()) return 'Expired';
    return 'Active';
  };

  return (
    <div className="border border-border rounded-lg p-4 bg-card">
      <div className="flex justify-between items-start">
        <div>
          <h3 className="font-medium text-lg">{token.label}</h3>
          {token.description && <p className="text-muted-foreground text-sm mt-1">{token.description}</p>}
        </div>
        <div className={`px-2 py-1 rounded text-sm font-medium ${getStatusColor()}`}>
          {getStatusText()}
        </div>
      </div>

      <div className="mt-4">
        <div className="flex items-center space-x-2 mb-2">
          <div className="text-sm font-medium">Token:</div>
          <div className="flex-1 bg-background rounded p-2 font-mono text-sm overflow-hidden">
            {showToken ? token.token : '••••••••••••••••••••••••••••••'}
          </div>
          <button
            onClick={() => setShowToken(!showToken)}
            className="text-sm text-primary hover:underline"
          >
            {showToken ? 'Hide' : 'Show'}
          </button>
          <button
            onClick={handleCopyToken}
            className="text-sm text-primary hover:underline"
          >
            {copied ? 'Copied!' : 'Copy'}
          </button>
        </div>

        <div className="grid grid-cols-2 gap-4 mt-4 text-sm">
          <div>
            <div className="text-muted-foreground">Created</div>
            <div>{formatDate(token.createdAt)}</div>
          </div>
          <div>
            <div className="text-muted-foreground">Expires</div>
            <div>{formatDate(token.expiresAt)}</div>
          </div>
        </div>

        <div className="mt-4">
          <div className="text-sm text-muted-foreground mb-1">Permissions:</div>
          <div className="flex flex-wrap gap-2">
            {token.permissions.map((permission) => (
              <span
                key={permission}
                className="px-2 py-1 bg-secondary text-secondary-foreground rounded-full text-xs"
              >
                {permission}
              </span>
            ))}
          </div>
        </div>

        {!token.revoked && (
          <div className="mt-4 pt-4 border-t border-border">
            <button
              onClick={onRevoke}
              className="text-sm text-red-500 hover:text-red-700"
            >
              Revoke Token
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
