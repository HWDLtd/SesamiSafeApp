import { router } from 'expo-router';
import { useEffect } from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ScreenFooter } from '@/src/components/ScreenFooter';
import { SesamiLogo } from '@/src/components/SesamiLogo';

const SPLASH_DURATION_MS = 2500;

export default function SplashScreen() {
  const insets = useSafeAreaInsets();

  useEffect(() => {
    const timer = setTimeout(() => {
      router.replace('/sign-in');
    }, SPLASH_DURATION_MS);

    return () => clearTimeout(timer);
  }, []);

  return (
    <View style={styles.container}>
      <View style={[styles.header, { paddingTop: insets.top + 48 }]}>
        <SesamiLogo />
      </View>

      <View style={styles.center}>
        <ActivityIndicator size="large" color="#FFFFFF" />
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
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
