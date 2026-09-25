import { Platform } from 'react-native';

/** Auth0 custom domain. */
export const AUTH0_DOMAIN = 'auth.dev.sesami.io';

/**
 * Native application Client IDs (public).
 * SES-Android-App / SES-iOS-App in the Auth0 Development tenant.
 * Never put Client Secrets in the mobile app — Native apps are public clients.
 */
export const AUTH0_CLIENT_ID = Platform.select({
  ios: 'ATFYCHUzYLOsx1vAParKQBfjo34URF5g',
  android: 'oVPNiz3hd6c3XD2s6YHJapLcrlIfrspv',
  default: 'oVPNiz3hd6c3XD2s6YHJapLcrlIfrspv',
}) as string;

/** API audience for access tokens. */
export const AUTH0_AUDIENCE = 'https://frankfurt.uat.sesami.io/';

/**
 * Auth0 callback scheme.
 *
 * Android:
 * Uses HTTPS App Links.
 * https://auth.dev.sesami.io/android/io.sesami.app/callback
 *
 * iOS:
 * Uses the custom URL scheme.
 * sesami://auth.dev.sesami.io/ios/io.sesami.app/callback
 */
export const AUTH0_CUSTOM_SCHEME = Platform.select({
  android: 'https',
  ios: 'sesami',
  default: 'sesami',
}) as string;

/**
 * Auth0 scopes requested by the mobile app.
 */
export const AUTH0_SCOPE = 'openid profile email offline_access';