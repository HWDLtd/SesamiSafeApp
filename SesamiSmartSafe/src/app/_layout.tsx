import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import * as SplashScreen from 'expo-splash-screen';
import { useEffect } from 'react';
import { Auth0Provider } from 'react-native-auth0';

import { AUTH0_CLIENT_ID, AUTH0_DOMAIN } from '@/src/constants/auth0';
import { colors } from '@/src/constants/colors';
import { BackgroundProvider } from '@/src/context/BackgroundContext';
import { SafeSessionProvider } from '@/src/context/SafeSessionContext';

export { ErrorBoundary } from 'expo-router';

SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  useEffect(() => {
    SplashScreen.hideAsync();
  }, []);

  return (
    <Auth0Provider domain={AUTH0_DOMAIN} clientId={AUTH0_CLIENT_ID}>
      <SafeSessionProvider>
        <BackgroundProvider>
          <StatusBar style="light" />
          <Stack
            screenOptions={{
              headerShown: false,
              contentStyle: { backgroundColor: colors.black },
              animation: 'fade',
            }}>
            <Stack.Screen name="index" />
            <Stack.Screen name="(auth)/sign-in" />
            <Stack.Screen name="(auth)/safe-sign-in" />
            <Stack.Screen name="(auth)/account" />
          </Stack>
        </BackgroundProvider>
      </SafeSessionProvider>
    </Auth0Provider>
  );
}
