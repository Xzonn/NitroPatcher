import { useRef } from 'react';
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'primary';
  loading?: boolean;
}

export const Button = ({
  variant = 'default',
  loading = false,
  disabled,
  className = '',
  children,
  type = 'button',
  ...props
}: ButtonProps) => (
  <button
    {...props}
    type={type}
    className={`button button--${variant} ${className}`}
    disabled={disabled || loading}
    aria-busy={loading}
  >
    {loading && <span className="spinner" aria-hidden="true" />}
    {children}
  </button>
);

export const Input = ({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) => (
  <input {...props} className={`input ${className}`} />
);

export const FormField = ({
  label,
  htmlFor,
  children,
}: {
  label: string;
  htmlFor: string;
  children: ReactNode;
}) => (
  <div className="form-field">
    <label htmlFor={htmlFor}>{label}</label>
    <div className="form-field__control">{children}</div>
  </div>
);

export const FilePicker = ({
  id,
  label,
  accept,
  file,
  disabled = false,
  onSelect,
}: {
  id: string;
  label: string;
  accept?: string;
  file: File | null;
  disabled?: boolean;
  onSelect: (file: File) => void;
}) => {
  const input = useRef<HTMLInputElement>(null);
  return (
    <div className="file-picker">
      <Input aria-label={`${label}文件名`} readOnly value={file?.name ?? ''} disabled={disabled} />
      <input
        ref={input}
        className="visually-hidden"
        type="file"
        id={id}
        aria-label={label}
        accept={accept}
        disabled={disabled}
        tabIndex={-1}
        onChange={(event) => {
          const selected = event.target.files?.[0];
          if (selected) onSelect(selected);
          event.target.value = '';
        }}
      />
      <Button
        disabled={disabled}
        onClick={() => input.current?.click()}
        aria-label={`选择${label}`}
      >
        …
      </Button>
    </div>
  );
};

export const Alert = ({
  variant,
  children,
}: {
  variant: 'error' | 'warning' | 'success';
  children: ReactNode;
}) => (
  <div className={`alert alert--${variant}`} role={variant === 'success' ? 'status' : 'alert'}>
    <span className="alert__icon" aria-hidden="true">
      {variant === 'success' ? '✓' : '!'}
    </span>
    <div>{children}</div>
  </div>
);
