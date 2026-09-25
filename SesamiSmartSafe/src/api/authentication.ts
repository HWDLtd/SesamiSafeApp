import { deviceCoreRequest, type DeviceCoreSession } from '@/src/api/client';
import { LOCAL_USER_SESSION_TYPE } from '@/src/constants/deviceCore';
import type { LoginResponse } from '@/src/types/authentication';

export function login(username: string, password: string) {
  return deviceCoreRequest<LoginResponse>({
    method: 'POST',
    path: '/authentication/login',
    body: {
      Password: password,
      UserSessionType: LOCAL_USER_SESSION_TYPE,
      Username: username,
    },
  });
}

export function logout(session: DeviceCoreSession) {
  return deviceCoreRequest<void>({
    method: 'POST',
    path: '/authentication/logout',
    body: {},
    session,
  });
}

export function isAuthenticated(session: DeviceCoreSession) {
  return deviceCoreRequest<unknown>({
    method: 'GET',
    path: '/authentication/isAuthenticated',
    session,
  });
}
