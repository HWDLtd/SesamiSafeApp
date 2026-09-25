import Constants from 'expo-constants';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, View } from 'react-native';
import { FormProvider, useForm } from 'react-hook-form';
import { useAuth0 } from 'react-native-auth0';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ScreenFooter } from '@/src/components/ScreenFooter';
import { SafeSignInCard } from '@/src/components/safe-sign-in/SafeSignInCard';
import { SafeVerifyingCard } from '@/src/components/safe-sign-in/SafeVerifyingCard';
import {
  safeSignInResolver,
  type SafeSignInFormValues,
} from '@/src/components/safe-sign-in/validateSafeSignIn';
import { SesamiLogo } from '@/src/components/SesamiLogo';
import { colors } from '@/src/constants/colors';
import { useSafeSession } from '@/src/context/SafeSessionContext';

const APP_VERSION = Constants.expoConfig?.version ?? '1.0.0';

export default function SafeSignInScreen() {
  const insets = useSafeAreaInsets();
  const { user, isLoading } = useAuth0();
  const { signIn } = useSafeSession();
  const [isVerifying, setIsVerifying] = useState(false);
  const form = useForm<SafeSignInFormValues>({
    defaultValues: { email: '', password: '' },
    resolver: safeSignInResolver,
  });

  useEffect(() => {
    if (!isLoading && !user) {
      router.replace('/sign-in');
    }
  }, [isLoading, user]);

  const handleSignIn = form.handleSubmit(async (values) => {
    if (isVerifying) {
      return;
    }

    setIsVerifying(true);
    try {
      await signIn(values.email.trim(), values.password);
    } catch {
      form.setError('email', { type: 'server' });
      form.setError('password', { type: 'server' });
      form.setError('root.server', { type: 'server' });
    } finally {
      setIsVerifying(false);
    }
  });

  if (isLoading || !user) {
    return <View style={styles.container} />;
  }

  return (
    <View style={styles.container}>
      <KeyboardAvoidingView
        style={styles.container}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView
          keyboardShouldPersistTaps="handled"
          contentContainerStyle={styles.scroll}
          showsVerticalScrollIndicator={false}>
          <View style={[styles.header, { paddingTop: insets.top + 48 }]}>
            <SesamiLogo />
          </View>

          <View style={styles.cardSlot}>
            {isVerifying ? (
              <SafeVerifyingCard />
            ) : (
              <FormProvider {...form}>
                <SafeSignInCard
                  version={APP_VERSION}
                  onSubmit={handleSignIn}
                  onResetPassword={() => {}}
                />
              </FormProvider>
            )}
          </View>
        </ScrollView>
        <ScreenFooter />
      </KeyboardAvoidingView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.black,
  },
  scroll: {
    flexGrow: 1,
    paddingBottom: 24,
  },
  header: {
    alignItems: 'center',
  },
  cardSlot: {
    paddingHorizontal: 24,
    paddingTop: 36,
  },
});
