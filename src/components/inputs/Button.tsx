import React from 'react';

import { Loading } from '../feedback';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  ariaLabel?: string;
  children?: React.ReactNode;
  disabled?: boolean;
  leftIcon?: React.ReactNode;
  className?: string;
  loading?: boolean;
  variant?: 'filled' | 'outline' | 'light' | 'transparent';
}

const Button = (props: ButtonProps) => {
  // default variant is filled
  const { variant = 'filled' } = props;

  let variantClass = '';

  switch (variant) {
    case 'transparent':
      variantClass = 'bg-transparent text-black hover:bg-primary-200 hover:bg-opacity-20';
      break;
    case 'outline':
      variantClass = 'border border-gray-300 text-gray-700 hover:bg-primary-100';
      break;
    case 'light':
      variantClass = 'bg-primary-100 text-gray-800 hover:bg-primary-200';
      break;
    default:
      variantClass = 'bg-primary-600 dark:bg-primary-950 hover:bg-primary-700 text-white';
      break;
  }

  return (
    <button
      disabled={props.disabled || props.loading}
      aria-label={props.ariaLabel}
      onClick={props.onClick}
      className={`${variantClass} font-bold my-6 py-2 px-4 flex flex-row ${props.className ?? ''}`}
    >
      {props.leftIcon && <span className="mr-2">{props.leftIcon}</span>}
      {props.children}
      {props.loading && (
        <div>
          <Loading />
        </div>
      )}
    </button>
  );
};

export default Button;
