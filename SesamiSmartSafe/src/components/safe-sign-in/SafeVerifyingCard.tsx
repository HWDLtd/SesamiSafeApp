import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { colors } from '@/src/constants/colors';

export function SafeVerifyingCard() {
  return (
    <View style={styles.card}>
      <View style={styles.heading}>
        <ActivityIndicator color={colors.text} />
        <View style={styles.headingCopy}>
          <Text style={styles.title}>Please Wait</Text>
          <Text style={styles.subtitle}>a Few Moments</Text>
        </View>
      </View>
      <View style={styles.divider} />
      <Text style={styles.status}>Verifying your account</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 18,
    backgroundColor: colors.card,
    paddingTop: 22,
    paddingBottom: 18,
    overflow: 'hidden',
  },
  heading: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 14,
    paddingHorizontal: 22,
  },
  headingCopy: {
    flex: 1,
  },
  title: {
    color: colors.text,
    fontSize: 28,
    fontWeight: '700',
    lineHeight: 34,
  },
  subtitle: {
    color: colors.text,
    fontSize: 22,
    lineHeight: 28,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    backgroundColor: colors.divider,
    marginTop: 18,
  },
  status: {
    color: colors.text,
    fontSize: 16,
    textAlign: 'center',
    paddingTop: 16,
  },
});
