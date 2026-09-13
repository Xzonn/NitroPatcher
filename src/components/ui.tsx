import { useRef, useState } from 'react';
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
  const dragDepth = useRef(0);
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState('');
  const selectFile = (selected: File): void => {
    if (disabled) return;
    const accepted =
      !accept ||
      accept.split(',').some((entry) => {
        const pattern = entry.trim().toLowerCase();
        if (pattern.startsWith('.')) return selected.name.toLowerCase().endsWith(pattern);
        if (pattern.endsWith('/*'))
          return selected.type.toLowerCase().startsWith(pattern.slice(0, -1));
        return selected.type.toLowerCase() === pattern;
      });
    if (!accepted) {
      setError(`请选择支持的文件格式：${accept}`);
      return;
    }
    setError('');
    onSelect(selected);
  };
  return (
    <div
      onDragEnter={(event) => {
        event.preventDefault();
        if (disabled || !event.dataTransfer.types.includes('Files')) return;
        ++dragDepth.current;
        setDragging(true);
      }}
      onDragOver={(event) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = disabled ? 'none' : 'copy';
      }}
      onDragLeave={(event) => {
        event.preventDefault();
        dragDepth.current = Math.max(0, dragDepth.current - 1);
        if (!dragDepth.current) setDragging(false);
      }}
      onDrop={(event) => {
        event.preventDefault();
        dragDepth.current = 0;
        setDragging(false);
        if (disabled) return;
        const files = event.dataTransfer.files;
        if (files.length !== 1) {
          setError('请一次拖入一个文件。');
          return;
        }
        const selected = files[0];
        if (selected) selectFile(selected);
      }}
    >
      <div className={`file-picker${dragging && !disabled ? ' file-picker--dragging' : ''}`}>
        <Input
          aria-label={`${label}文件名`}
          aria-describedby={error ? `${id}-error` : undefined}
          aria-invalid={!!error}
          placeholder="选择或拖入文件"
          readOnly
          value={file?.name ?? ''}
          disabled={disabled}
        />
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
            if (selected) selectFile(selected);
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
      {error && (
        <p id={`${id}-error`} className="file-picker-error" role="alert">
          {error}
        </p>
      )}
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
