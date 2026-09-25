import { useRef } from 'react';
import { useFormContext } from 'react-hook-form';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import { TextField } from '@/src/components/form/TextField';
import { SafeSignInBanner } from '@/src/components/safe-sign-in/SafeSignInBanner';
import {
  SAFE_SIGN_IN_REJECTED,
  validateSafeSignIn,
  type SafeSignInFormValues,
} from '@/src/components/safe-sign-in/validateSafeSignIn';
import { colors } from '@/src/constants/colors';

type SafeSignInCardProps = {
  version: string;
  onSubmit: () => void;
  onResetPassword: () => void;
};

export function SafeSignInCard({ version, onSubmit, onResetPassword }: SafeSignInCardProps) {
  const passwordRef = useRef<TextInput>(null);
  const { getValues, formState } = useFormContext<SafeSignInFormValues>();
  const { errors } = formState;
  const feedback = errors.root?.server
    ? SAFE_SIGN_IN_REJECTED
    : errors.email || errors.password
      ? validateSafeSignIn(getValues('email'), getValues('password'))
      : null;

  return (
    <View style={styles.card}>
      {feedback ? <SafeSignInBanner title={feedback.title} lines={feedback.lines} /> : null}

      <TextField<SafeSignInFormValues>
        name="email"
        label="Email Address"
        placeholder="Enter email address"
        onSubmitEditing={() => passwordRef.current?.focus()}
        autoComplete="email"
        keyboardType="email-address"
        returnKeyType="next"
      />

      <TextField<SafeSignInFormValues>
        name="password"
        label="Password"
        placeholder="Enter password"
        onSubmitEditing={onSubmit}
        inputRef={passwordRef}
        secureTextEntry
        autoComplete="current-password"
        returnKeyType="go"
      />

      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Sign In"
        onPress={onSubmit}
        style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}>
        <Text style={styles.buttonLabel}>Sign In</Text>
      </Pressable>

      <View style={styles.divider} />

      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Reset Password"
        onPress={onResetPassword}
        style={styles.helpRow}>
        <View style={styles.helpIcon}>
          <Text style={styles.helpIconText}>?</Text>
        </View>
        <View style={styles.helpCopy}>
          <Text style={styles.helpPrompt}>Having trouble signing in?</Text>
          <Text style={styles.helpAction}>Reset Password</Text>
        </View>
        <Text style={styles.helpArrow}>→</Text>
      </Pressable>

      <View style={styles.divider} />
      <Text style={styles.version}>Version {version}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 18,
    backgroundColor: colors.card,
    paddingHorizontal: 18,
    paddingTop: 18,
    paddingBottom: 14,
    gap: 16,
  },
  button: {
    minHeight: 54,
    borderRadius: 10,
    backgroundColor: colors.black,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
  },
  buttonPressed: {
    opacity: 0.85,
  },
  buttonLabel: {
    color: colors.white,
    fontSize: 20,
    fontWeight: '700',
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    backgroundColor: colors.divider,
    marginHorizontal: -18,
  },
  helpRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  helpIcon: {
    width: 26,
    height: 26,
    borderRadius: 13,
    borderWidth: 1.5,
    borderColor: colors.text,
    alignItems: 'center',
    justifyContent: 'center',
  },
  helpIconText: {
    color: colors.text,
    fontSize: 15,
    fontWeight: '700',
  },
  helpCopy: {
    flex: 1,
  },
  helpPrompt: {
    color: colors.text,
    fontSize: 15,
    lineHeight: 20,
  },
  helpAction: {
    color: colors.text,
    fontSize: 16,
    fontWeight: '700',
    textDecorationLine: 'underline',
    lineHeight: 22,
  },
  helpArrow: {
    color: colors.text,
    fontSize: 22,
    fontWeight: '500',
  },
  version: {
    color: colors.text,
    fontSize: 15,
    fontWeight: '700',
    textAlign: 'center',
  },
});
