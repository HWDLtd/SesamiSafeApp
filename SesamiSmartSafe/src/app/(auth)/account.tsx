import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Image,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useAuth0 } from 'react-native-auth0';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ScreenFooter } from '@/src/components/ScreenFooter';
import { SesamiLogo } from '@/src/components/SesamiLogo';

type Detail = {
  label: string;
  value: string;
};

export default function AccountScreen() {
  const insets = useSafeAreaInsets();
  const { user, getCredentials, clearCredentials } = useAuth0();
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [expiresAt, setExpiresAt] = useState<number | null>(null);
  const [isLoadingToken, setIsLoadingToken] = useState(true);
  const [tokenError, setTokenError] = useState<string | null>(null);
  const [isSigningOut, setIsSigningOut] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadCredentials() {
      try {
        const credentials = await getCredentials();
        if (cancelled) {
          return;
        }
        setAccessToken(credentials.accessToken);
        setExpiresAt(credentials.expiresAt);
      } catch (error) {
        if (cancelled) {
          return;
        }
        setTokenError(
          error instanceof Error ? error.message : 'Unable to load the access token.',
        );
      } finally {
        if (!cancelled) {
          setIsLoadingToken(false);
        }
      }
    }

    loadCredentials();

    return () => {
      cancelled = true;
    };
  }, [getCredentials]);

  async function handleSignOut() {
    if (isSigningOut) {
      return;
    }

    setIsSigningOut(true);
    try {
      await clearCredentials();
      router.replace('/sign-in');
    } catch (error) {
      setTokenError(
        error instanceof Error ? error.message : 'Unable to sign out. Please try again.',
      );
      setIsSigningOut(false);
    }
  }

  const details: Detail[] = [
    { label: 'Name', value: user?.name },
    { label: 'Email', value: user?.email },
    {
      label: 'Email verified',
      value: user ? (user.emailVerified ? 'Yes' : 'No') : undefined,
    },
    { label: 'Nickname', value: user?.nickname },
    { label: 'User ID', value: user?.sub },
  ].flatMap((detail) => (detail.value ? [{ label: detail.label, value: detail.value }] : []));

  const expiryLabel =
    expiresAt != null
      ? new Date(expiresAt * 1000).toLocaleString(undefined, {
          dateStyle: 'medium',
          timeStyle: 'short',
        })
      : null;

  return (
    <View style={styles.container}>
      <View style={[styles.header, { paddingTop: insets.top + 32 }]}>
        <SesamiLogo />
      </View>

      <ScrollView
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}>
        <Text style={styles.title}>Signed in</Text>

        {user?.picture ? (
          <Image source={{ uri: user.picture }} style={styles.avatar} accessibilityIgnoresInvertColors />
        ) : null}

        <View style={styles.card}>
          <Text style={styles.sectionLabel}>User</Text>
          {details.length > 0 ? (
            details.map((detail) => (
              <View key={detail.label} style={styles.row}>
                <Text style={styles.rowLabel}>{detail.label}</Text>
                <Text selectable style={styles.rowValue}>
                  {detail.value}
                </Text>
              </View>
            ))
          ) : (
            <Text style={styles.empty}>No profile details were returned.</Text>
          )}
        </View>

        <View style={styles.card}>
          <Text style={styles.sectionLabel}>Access token</Text>
          {isLoadingToken ? (
            <ActivityIndicator color="#FFFFFF" style={styles.tokenLoading} />
          ) : tokenError ? (
            <Text style={styles.error}>{tokenError}</Text>
          ) : (
            <>
              {expiryLabel ? (
                <Text style={styles.expiry}>Expires {expiryLabel}</Text>
              ) : null}
              <Text selectable style={styles.token}>
                {accessToken}
              </Text>
            </>
          )}
        </View>

        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Sign out"
          disabled={isSigningOut}
          onPress={handleSignOut}
          style={({ pressed }) => [
            styles.signOutButton,
            pressed && styles.signOutButtonPressed,
            isSigningOut && styles.signOutButtonDisabled,
          ]}>
          {isSigningOut ? (
            <ActivityIndicator color="#FFFFFF" />
          ) : (
            <Text style={styles.signOutLabel}>Sign out</Text>
          )}
        </Pressable>
      </ScrollView>

      <ScreenFooter />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000000',
  },
  header: {
    alignItems: 'center',
  },
  content: {
    paddingHorizontal: 24,
    paddingTop: 28,
    paddingBottom: 24,
    gap: 16,
  },
  title: {
    color: '#FFFFFF',
    fontSize: 28,
    fontWeight: '600',
    textAlign: 'center',
  },
  avatar: {
    width: 72,
    height: 72,
    borderRadius: 36,
    alignSelf: 'center',
    backgroundColor: '#1F1F1F',
  },
  card: {
    borderRadius: 8,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.12)',
    backgroundColor: 'rgba(255, 255, 255, 0.06)',
    paddingHorizontal: 16,
    paddingVertical: 14,
    gap: 12,
  },
  sectionLabel: {
    color: '#9CA3AF',
    fontSize: 12,
    fontWeight: '600',
    letterSpacing: 1,
    textTransform: 'uppercase',
  },
  row: {
    gap: 4,
  },
  rowLabel: {
    color: '#9CA3AF',
    fontSize: 13,
  },
  rowValue: {
    color: '#FFFFFF',
    fontSize: 16,
    lineHeight: 22,
  },
  empty: {
    color: '#D1D5DB',
    fontSize: 15,
    lineHeight: 22,
  },
  expiry: {
    color: '#D1D5DB',
    fontSize: 14,
  },
  token: {
    color: '#FFFFFF',
    fontSize: 13,
    lineHeight: 20,
    fontFamily: Platform.select({ ios: 'Menlo', android: 'monospace', default: 'monospace' }),
  },
  tokenLoading: {
    alignSelf: 'flex-start',
  },
  error: {
    color: '#F87171',
    fontSize: 14,
    lineHeight: 20,
  },
  signOutButton: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 52,
    borderRadius: 4,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.4)',
    marginTop: 8,
  },
  signOutButtonPressed: {
    opacity: 0.85,
  },
  signOutButtonDisabled: {
    opacity: 0.7,
  },
  signOutLabel: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
});
