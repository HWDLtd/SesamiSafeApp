import type { ImageSourcePropType } from 'react-native';

export const GRADIENT_IDS = ['1', '2', '3'] as const;

export type GradientId = (typeof GRADIENT_IDS)[number];

export const DEFAULT_GRADIENT_ID: GradientId = '1';

export const GRADIENT_SOURCES: Record<GradientId, ImageSourcePropType> = {
  '1': require('../../assets/images/gradient-1.png'),
  '2': require('../../assets/images/gradient-2.png'),
  '3': require('../../assets/images/gradient-3.png'),
};
