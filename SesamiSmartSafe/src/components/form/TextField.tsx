import { useState, type Ref } from 'react';
import { useController, useFormContext, type FieldPath, type FieldValues } from 'react-hook-form';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import { colors } from '@/src/constants/colors';

type TextFieldProps<T extends FieldValues> = {
  name: FieldPath<T>;
  label: string;
  placeholder?: string;
  secureTextEntry?: boolean;
  onSubmitEditing?: () => void;
  inputRef?: Ref<TextInput>;
  autoComplete?: 'email' | 'password' | 'current-password' | 'off';
  keyboardType?: 'email-address' | 'default';
  returnKeyType?: 'next' | 'go' | 'done';
};

function assignRef<T>(ref: Ref<T> | undefined, value: T | null) {
  if (!ref) {
    return;
  }
  if (typeof ref === 'function') {
    ref(value);
    return;
  }
  ref.current = value;
}

export function TextField<T extends FieldValues>({
  name,
  label,
  placeholder,
  secureTextEntry = false,
  onSubmitEditing,
  inputRef,
  autoComplete = 'off',
  keyboardType = 'default',
  returnKeyType = 'next',
}: TextFieldProps<T>) {
  const { control, clearErrors } = useFormContext<T>();
  const { field, fieldState } = useController({ control, name });
  const [concealed, setConcealed] = useState(secureTextEntry);
  const invalid = Boolean(fieldState.error);

  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <View style={[styles.inputRow, invalid && styles.inputRowInvalid]}>
        <TextInput
          ref={(node) => {
            field.ref(node);
            assignRef(inputRef, node);
          }}
          defaultValue={field.value ?? ''}
          placeholder={placeholder}
          placeholderTextColor={colors.placeholder}
          onChangeText={(text) => {
            field.onChange(text);
            if (fieldState.error) {
              clearErrors();
            }
          }}
          onBlur={field.onBlur}
          onSubmitEditing={onSubmitEditing}
          secureTextEntry={secureTextEntry && concealed}
          autoCapitalize="none"
          autoCorrect={false}
          autoComplete={autoComplete}
          keyboardType={keyboardType}
          returnKeyType={returnKeyType}
          textContentType={
            autoComplete === 'email' ? 'emailAddress' : secureTextEntry ? 'password' : 'none'
          }
          style={[styles.input, secureTextEntry && styles.inputWithToggle]}
        />
        {secureTextEntry ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={concealed ? 'Show' : 'Hide'}
            onPress={() => setConcealed((current) => !current)}
            hitSlop={8}
            style={styles.toggle}>
            <Text style={styles.toggleLabel}>{concealed ? 'Show' : 'Hide'}</Text>
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  field: {
    gap: 8,
  },
  label: {
    color: colors.text,
    fontSize: 16,
    fontWeight: '600',
  },
  inputRow: {
    minHeight: 48,
    borderRadius: 8,
    backgroundColor: colors.white,
    justifyContent: 'center',
  },
  inputRowInvalid: {
    backgroundColor: colors.inputError,
    borderWidth: 1,
    borderColor: colors.inputErrorBorder,
  },
  input: {
    color: colors.text,
    fontSize: 16,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  inputWithToggle: {
    paddingRight: 72,
  },
  toggle: {
    position: 'absolute',
    right: 14,
    top: 0,
    bottom: 0,
    justifyContent: 'center',
  },
  toggleLabel: {
    color: colors.text,
    fontSize: 16,
    fontWeight: '700',
    textDecorationLine: 'underline',
  },
});
