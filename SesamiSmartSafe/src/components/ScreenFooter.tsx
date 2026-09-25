import { StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

export function ScreenFooter() {
  const insets = useSafeAreaInsets();

  return (
    <View style={[styles.footer, { paddingBottom: Math.max(insets.bottom, 24) }]}>
      <Text style={styles.copyright}>SESAMI © 2026</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  footer: {
    alignItems: 'center',
    paddingTop: 16,
  },
  copyright: {
    color: '#9CA3AF',
    fontSize: 12,
    letterSpacing: 0.4,
  },
});
