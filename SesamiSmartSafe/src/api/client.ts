import { DEVICE_CORE_BASE_URL } from '@/src/constants/deviceCore';

export type DeviceCoreSession = {
  token: string;
  userSession: unknown;
  permissions: unknown[];
};

type DeviceCoreRequest = {
  method: 'GET' | 'POST';
  path: string;
  body?: unknown;
  session?: DeviceCoreSession | null;
};

export class DeviceCoreError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'DeviceCoreError';
    this.status = status;
  }
}

function buildHeaders(session: DeviceCoreSession | null | undefined, hasBody: boolean) {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  if (hasBody) {
    headers['Content-Type'] = 'application/json';
  }

  if (!session) {
    return headers;
  }

  headers.Authorization = `Bearer ${session.token}`;
  headers.UserSession = JSON.stringify(session.userSession);
  headers.UserPermissions = JSON.stringify(session.permissions);
  return headers;
}

export async function deviceCoreRequest<T>(request: DeviceCoreRequest): Promise<T> {
  const hasBody = request.body !== undefined;

  const response = await fetch(`${DEVICE_CORE_BASE_URL}${request.path}`, {
    method: request.method,
    headers: buildHeaders(request.session, hasBody),
    body: hasBody ? JSON.stringify(request.body) : undefined,
  });

  const text = await response.text();
  if (!response.ok) {
    throw new DeviceCoreError(response.status, text || response.statusText);
  }

  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}
