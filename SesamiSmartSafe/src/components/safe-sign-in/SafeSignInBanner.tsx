import { StyleSheet, Text, View } from 'react-native';

import { colors } from '@/src/constants/colors';

type SafeSignInBannerProps = {
  title: string;
  lines: string[];
};

export function SafeSignInBanner({ title, lines }: SafeSignInBannerProps) {
  return (
    <View accessibilityRole="alert" style={styles.banner}>
      <Text style={styles.title}>{title}</Text>
      {lines.map((line) => (
        <Text key={line} style={styles.line}>
          {line}
        </Text>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  banner: {
    borderWidth: 1,
    borderColor: colors.errorBannerBorder,
    borderRadius: 8,
    backgroundColor: colors.errorBanner,
    paddingHorizontal: 14,
    paddingVertical: 12,
    gap: 2,
  },
  title: {
    color: colors.error,
    fontSize: 16,
    fontWeight: '700',
    lineHeight: 22,
  },
  line: {
    color: colors.error,
    fontSize: 15,
    lineHeight: 21,
  },
});
