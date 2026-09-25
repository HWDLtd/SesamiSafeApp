import type { FieldErrors, Resolver } from 'react-hook-form';

export type SafeSignInFormValues = {
  email: string;
  password: string;
};

export type SafeSignInFeedback = {
  title: string;
  lines: string[];
  emailInvalid: boolean;
  passwordInvalid: boolean;
};

export const SAFE_SIGN_IN_REJECTED: SafeSignInFeedback = {
  title: 'Sign In Failed',
  lines: ['Incorrect email address or password.', 'Try again'],
  emailInvalid: true,
  passwordInvalid: true,
};

export function validateSafeSignIn(email: string, password: string): SafeSignInFeedback | null {
  const emailMissing = email.trim().length === 0;
  const passwordMissing = password.length === 0;

  if (!emailMissing && !passwordMissing) {
    return null;
  }

  if (emailMissing) {
    return {
      title: 'Enter your email address',
      lines: ['Enter your email address and try again'],
      emailInvalid: true,
      passwordInvalid: passwordMissing,
    };
  }

  return {
    title: 'Enter your password',
    lines: ['Enter your password and try again'],
    emailInvalid: false,
    passwordInvalid: true,
  };
}

export const safeSignInResolver: Resolver<SafeSignInFormValues> = (values) => {
  const feedback = validateSafeSignIn(values.email, values.password);
  if (!feedback) {
    return { values, errors: {} };
  }

  const errors: FieldErrors<SafeSignInFormValues> = {};
  if (feedback.emailInvalid) {
    errors.email = { type: 'required', message: feedback.title };
  }
  if (feedback.passwordInvalid) {
    errors.password = { type: 'required', message: feedback.title };
  }

  return { values: {}, errors };
};
