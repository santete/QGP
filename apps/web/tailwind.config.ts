import type { Config } from 'tailwindcss';

/**
 * Tailwind map design tokens Minimal UI (Q4 đã chốt).
 * Nguồn token: src/theme/tokens.css (đồng bộ từ Claude Design — colors_and_type.css).
 * Ở đây chỉ trỏ tới CSS var để single-source: đổi token .css là đổi cả Tailwind.
 */
const config: Config = {
  darkMode: ['class', '[data-theme="dark"]'],
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          lighter: 'var(--color-primary-lighter)',
          light: 'var(--color-primary-light)',
          DEFAULT: 'var(--color-primary-main)',
          dark: 'var(--color-primary-dark)',
          darker: 'var(--color-primary-darker)',
          contrast: 'var(--color-primary-contrast)',
        },
        info: { DEFAULT: 'var(--color-info-main)', lighter: 'var(--color-info-lighter)', darker: 'var(--color-info-darker)' },
        success: { DEFAULT: 'var(--color-success-main)', lighter: 'var(--color-success-lighter)', darker: 'var(--color-success-darker)' },
        warning: { DEFAULT: 'var(--color-warning-main)', lighter: 'var(--color-warning-lighter)', darker: 'var(--color-warning-darker)' },
        error: { DEFAULT: 'var(--color-error-main)', lighter: 'var(--color-error-lighter)', darker: 'var(--color-error-darker)' },
        grey: {
          100: 'var(--color-grey-100)',
          200: 'var(--color-grey-200)',
          300: 'var(--color-grey-300)',
          400: 'var(--color-grey-400)',
          500: 'var(--color-grey-500)',
          600: 'var(--color-grey-600)',
          700: 'var(--color-grey-700)',
          800: 'var(--color-grey-800)',
          900: 'var(--color-grey-900)',
        },
        text: {
          primary: 'var(--color-text-primary)',
          secondary: 'var(--color-text-secondary)',
          disabled: 'var(--color-text-disabled)',
        },
        bg: {
          default: 'var(--color-bg-default)',
          paper: 'var(--color-bg-paper)',
          neutral: 'var(--color-bg-neutral)',
        },
        divider: 'var(--color-divider)',
      },
      borderRadius: {
        xs: 'var(--radius-xs)',
        sm: 'var(--radius-sm)',
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)',
        '2xl': 'var(--radius-2xl)',
        full: 'var(--radius-full)',
      },
      boxShadow: {
        card: 'var(--shadow-card)',
        dropdown: 'var(--shadow-dropdown)',
        dialog: 'var(--shadow-dialog)',
        primary: 'var(--shadow-primary)',
      },
      fontFamily: {
        sans: ['Inter', 'Inter Variable', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      maxWidth: {
        container: 'var(--container-lg)',
      },
    },
  },
  plugins: [],
};

export default config;
