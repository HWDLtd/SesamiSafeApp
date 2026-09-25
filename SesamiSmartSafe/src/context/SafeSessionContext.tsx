import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { login, logout } from '@/src/api/authentication';
import type { LoginResponse, UserSession } from '@/src/types/authentication';
import type { DeviceCoreSession } from '@/src/api/client';

type SafeSessionContextValue = {
  loginResult: LoginResponse | null;
  userSession: UserSession | null;
  permissions: unknown[];
  signIn: (username: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
};

const SafeSessionContext = createContext<SafeSessionContextValue | null>(null);

function toDeviceCoreSession(
  loginResult: LoginResponse | null,
  permissions: unknown[],
): DeviceCoreSession | null {
  if (!loginResult) {
    return null;
  }

  return {
    token: loginResult.userSession.apiToken,
    userSession: loginResult.userSession,
    permissions,
  };
}

export function SafeSessionProvider({ children }: { children: ReactNode }) {
  const [loginResult, setLoginResult] = useState<LoginResponse | null>(null);
  const [permissions, setPermissions] = useState<unknown[]>([]);

  const signIn = useCallback(async (username: string, password: string) => {
    const result = await login(username, password);
    setLoginResult(result);
    setPermissions([]);
  }, []);

  const signOut = useCallback(async () => {
    const session = toDeviceCoreSession(loginResult, permissions);
    if (session) {
      await logout(session);
    }
    setLoginResult(null);
    setPermissions([]);
  }, [loginResult, permissions]);

  const value = useMemo(
    () => ({
      loginResult,
      userSession: loginResult?.userSession ?? null,
      permissions,
      signIn,
      signOut,
    }),
    [loginResult, permissions, signIn, signOut],
  );

  return <SafeSessionContext.Provider value={value}>{children}</SafeSessionContext.Provider>;
}

export function useSafeSession() {
  const context = useContext(SafeSessionContext);
  if (!context) {
    throw new Error('useSafeSession must be used within a SafeSessionProvider');
  }
  return context;
}
