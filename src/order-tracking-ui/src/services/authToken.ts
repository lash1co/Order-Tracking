import type { AuthInfo, UserRole } from '../domain/types';

const roleClaimNames = ['role', 'roles', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
const validRoles: UserRole[] = ['Admin', 'Dispatcher', 'Driver'];

export function decodeAuthInfo(token: string | null): AuthInfo {
  if (!token) return { roles: [], isExpired: false, isValidToken: false };

  try {
    const [, payload] = token.split('.');
    if (!payload) return { roles: [], isExpired: false, isValidToken: false };

    const claims = JSON.parse(decodeBase64Url(payload)) as Record<string, unknown>;
    const roles = readRoles(claims);
    const expiresAt = typeof claims.exp === 'number' ? new Date(claims.exp * 1000).toISOString() : undefined;
    const isExpired = expiresAt ? new Date(expiresAt).getTime() <= Date.now() : false;
    const subject = typeof claims.sub === 'string' ? claims.sub : typeof claims.name === 'string' ? claims.name : undefined;

    return {
      subject,
      roles,
      expiresAt,
      isExpired,
      isValidToken: true
    };
  } catch {
    return { roles: [], isExpired: false, isValidToken: false };
  }
}

export function hasAnyRole(auth: AuthInfo, roles: UserRole[]) {
  return roles.some((role) => auth.roles.includes(role));
}

function readRoles(claims: Record<string, unknown>) {
  const values = roleClaimNames.flatMap((claimName) => normalizeClaimValue(claims[claimName]));
  return validRoles.filter((role) => values.some((value) => value.toLowerCase() === role.toLowerCase()));
}

function normalizeClaimValue(value: unknown): string[] {
  if (typeof value === 'string') return [value];
  if (Array.isArray(value)) return value.filter((item): item is string => typeof item === 'string');
  return [];
}

function decodeBase64Url(value: string) {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
