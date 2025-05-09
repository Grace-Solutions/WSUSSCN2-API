import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { useAuth } from '../hooks/useAuth';
import TokenList from '../components/tokens/TokenList';

export default function Home() {
  const { isAuthenticated, loading } = useAuth();
  const router = useRouter();
  const [tokens, setTokens] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      router.push('/login');
    }
  }, [isAuthenticated, loading, router]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchTokens();
    }
  }, [isAuthenticated]);

  const fetchTokens = async () => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/tokens');
      if (response.ok) {
        const data = await response.json();
        setTokens(data);
      } else {
        console.error('Failed to fetch tokens');
      }
    } catch (error) {
      console.error('Error fetching tokens:', error);
    } finally {
      setIsLoading(false);
    }
  };

  if (loading || !isAuthenticated) {
    return <div className="flex items-center justify-center min-h-screen">Loading...</div>;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-6">WSUSSCN2-API Dashboard</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2">
          <div className="bg-card rounded-lg shadow p-6">
            <h2 className="text-2xl font-semibold mb-4">API Tokens</h2>
            <TokenList tokens={tokens} isLoading={isLoading} onTokensChanged={fetchTokens} />
          </div>
        </div>
        
        <div>
          <div className="bg-card rounded-lg shadow p-6 mb-6">
            <h2 className="text-xl font-semibold mb-4">System Status</h2>
            <div className="space-y-4">
              <div className="flex justify-between">
                <span>API Status:</span>
                <span className="text-green-500 font-medium">Online</span>
              </div>
              <div className="flex justify-between">
                <span>Database:</span>
                <span className="text-green-500 font-medium">Connected</span>
              </div>
              <div className="flex justify-between">
                <span>MinIO:</span>
                <span className="text-green-500 font-medium">Connected</span>
              </div>
              <div className="flex justify-between">
                <span>Redis:</span>
                <span className="text-green-500 font-medium">Connected</span>
              </div>
            </div>
          </div>
          
          <div className="bg-card rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
            <div className="space-y-4">
              <button 
                className="w-full bg-primary text-primary-foreground py-2 px-4 rounded hover:bg-primary/90 transition-colors"
                onClick={() => {/* Trigger sync */}}
              >
                Trigger Sync
              </button>
              <button 
                className="w-full bg-secondary text-secondary-foreground py-2 px-4 rounded hover:bg-secondary/90 transition-colors"
                onClick={() => {/* View updates */}}
              >
                View Updates
              </button>
              <button 
                className="w-full bg-secondary text-secondary-foreground py-2 px-4 rounded hover:bg-secondary/90 transition-colors"
                onClick={() => {/* View CABs */}}
              >
                View CAB Files
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
