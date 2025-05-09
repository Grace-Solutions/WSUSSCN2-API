import { useState } from 'react';
import TokenCard from './TokenCard';
import TokenForm from './TokenForm';

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

interface TokenListProps {
  tokens: Token[];
  isLoading: boolean;
  onTokensChanged: () => void;
}

export default function TokenList({ tokens, isLoading, onTokensChanged }: TokenListProps) {
  const [showCreateForm, setShowCreateForm] = useState(false);

  const handleCreateToken = async (tokenData: any) => {
    try {
      const response = await fetch('/api/tokens', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
        },
        body: JSON.stringify(tokenData),
      });

      if (response.ok) {
        setShowCreateForm(false);
        onTokensChanged();
      } else {
        console.error('Failed to create token');
      }
    } catch (error) {
      console.error('Error creating token:', error);
    }
  };

  const handleRevokeToken = async (id: string) => {
    try {
      const response = await fetch(`/api/tokens/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
        },
      });

      if (response.ok) {
        onTokensChanged();
      } else {
        console.error('Failed to revoke token');
      }
    } catch (error) {
      console.error('Error revoking token:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-40">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-lg font-medium">Manage API Tokens</h3>
        <button
          onClick={() => setShowCreateForm(!showCreateForm)}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors"
        >
          {showCreateForm ? 'Cancel' : 'Create Token'}
        </button>
      </div>

      {showCreateForm && (
        <div className="mb-6 p-4 border border-border rounded-md bg-background">
          <TokenForm onSubmit={handleCreateToken} onCancel={() => setShowCreateForm(false)} />
        </div>
      )}

      {tokens.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground">
          No tokens found. Create your first token to get started.
        </div>
      ) : (
        <div className="space-y-4">
          {tokens.map((token) => (
            <TokenCard
              key={token.id}
              token={token}
              onRevoke={() => handleRevokeToken(token.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}
