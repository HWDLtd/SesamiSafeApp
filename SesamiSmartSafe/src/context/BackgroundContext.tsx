import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { Image, StyleSheet, View } from 'react-native';

import { colors } from '@/src/constants/colors';
import {
  DEFAULT_GRADIENT_ID,
  GRADIENT_IDS,
  GRADIENT_SOURCES,
  type GradientId,
} from '@/src/constants/gradients';

/** Set to true when ready to show gradient background images. */
export const GRADIENT_BACKGROUND_ENABLED = false;

type BackgroundContextValue = {
  gradientId: GradientId;
  setGradientId: (id: GradientId) => void;
  cycleGradient: () => void;
};

const BackgroundContext = createContext<BackgroundContextValue | null>(null);

export function BackgroundProvider({ children }: { children: ReactNode }) {
  const [gradientId, setGradientId] = useState<GradientId>(DEFAULT_GRADIENT_ID);

  const cycleGradient = useCallback(() => {
    setGradientId((current) => {
      const index = GRADIENT_IDS.indexOf(current);
      return GRADIENT_IDS[(index + 1) % GRADIENT_IDS.length];
    });
  }, []);

  const value = useMemo(
    () => ({ gradientId, setGradientId, cycleGradient }),
    [gradientId, cycleGradient],
  );

  return (
    <BackgroundContext.Provider value={value}>
      <View style={styles.root}>
        {GRADIENT_BACKGROUND_ENABLED ? (
          <View
            style={styles.background}
            pointerEvents="none"
            accessibilityElementsHidden
            importantForAccessibility="no-hide-descendants">
            <Image
              source={GRADIENT_SOURCES[gradientId]}
              style={styles.backgroundImage}
              resizeMode="cover"
            />
          </View>
        ) : null}
        <View style={styles.content}>{children}</View>
      </View>
    </BackgroundContext.Provider>
  );
}

export function useBackground() {
  const context = useContext(BackgroundContext);
  if (!context) {
    throw new Error('useBackground must be used within a BackgroundProvider');
  }
  return context;
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: colors.black,
  },
  background: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    backgroundColor: colors.black,
  },
  backgroundImage: {
    width: '100%',
    height: '100%',
    backgroundColor: colors.black,
  },
  content: {
    flex: 1,
    backgroundColor: colors.black,
  },
});
