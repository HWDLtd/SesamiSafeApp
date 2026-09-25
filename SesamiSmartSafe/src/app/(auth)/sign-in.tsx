import { router } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useAuth0 } from 'react-native-auth0';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ScreenFooter } from '@/src/components/ScreenFooter';
import { SesamiLogo } from '@/src/components/SesamiLogo';
import {
  AUTH0_AUDIENCE,
  AUTH0_CUSTOM_SCHEME,
  AUTH0_SCOPE,
} from '@/src/constants/auth0';

export default function SignInScreen() {
  const insets = useSafeAreaInsets();
  const { authorize } = useAuth0();
  const [isSigningIn, setIsSigningIn] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleSignIn() {
    if (isSigningIn) {
      return;
    }

    setErrorMessage(null);
    setIsSigningIn(true);

    try {
      await authorize(
        { scope: AUTH0_SCOPE, audience: AUTH0_AUDIENCE },
        { customScheme: AUTH0_CUSTOM_SCHEME },
      );
      router.replace('/account');
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Unable to sign in. Please try again.';

      // User dismissed the Auth0 browser session — not an error to surface.
      if (message.includes('user_cancelled') || message.includes('a0.session.user_cancelled')) {
        return;
      }

      setErrorMessage(message);
    } finally {
      setIsSigningIn(false);
    }
  }

  return (
    <View style={styles.container}>
      <View style={[styles.header, { paddingTop: insets.top + 48 }]}>
        <SesamiLogo />
      </View>

      <View style={styles.body}>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Sign In"
          disabled={isSigningIn}
          onPress={handleSignIn}
          style={({ pressed }) => [
            styles.signInButton,
            pressed && styles.signInButtonPressed,
            isSigningIn && styles.signInButtonDisabled,
          ]}>
          {isSigningIn ? (
            <ActivityIndicator color="#0A0A0A" />
          ) : (
            <Text style={styles.signInLabel}>Sign In</Text>
          )}
        </Pressable>

        {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
      </View>

      <ScreenFooter />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: 'transparent',
  },
  header: {
    alignItems: 'center',
  },
  body: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: 32,
  },
  signInButton: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 56,
    borderRadius: 4,
    backgroundColor: '#FFFFFF',
    paddingHorizontal: 24,
  },
  signInButtonPressed: {
    opacity: 0.85,
  },
  signInButtonDisabled: {
    opacity: 0.7,
  },
  signInLabel: {
    color: '#0A0A0A',
    fontSize: 17,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  error: {
    marginTop: 16,
    color: '#F87171',
    fontSize: 14,
    textAlign: 'center',
    lineHeight: 20,
  },
});
