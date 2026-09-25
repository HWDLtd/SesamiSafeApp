import { Image, StyleSheet, type ImageStyle, type StyleProp } from 'react-native';

type SesamiLogoProps = {
  style?: StyleProp<ImageStyle>;
};

export function SesamiLogo({ style }: SesamiLogoProps) {
  return (
    <Image
      source={require('../../assets/images/sesami-logo.png')}
      style={[styles.logo, style]}
      resizeMode="contain"
      accessibilityLabel="SESAMI"
    />
  );
}

const styles = StyleSheet.create({
  logo: {
    width: 220,
    height: 48,
  },
});
