/**
 * Layout component wrapping main content with Header and Footer.
 * @param {{ children: ReactNode, fluid?: boolean }} props - Layout properties.
 * @returns {JSX.Element} The rendered Layout component.
 */
import { ReactNode } from 'react';

import { Footer } from '@/components/ui';
import getSiteSettings from '@/utils/site/getSiteSettings';

import Navbar from './Navbar';
import UserMenu from './UserMenu';

type LayoutProps = {
  children: ReactNode;
  fluid?: boolean;
  imageNavbar?: boolean;
  darkImage?: boolean;
};

export default async function Layout(props: LayoutProps) {
  const { children, fluid } = props;
  const site = await getSiteSettings();

  const bgClass = props.imageNavbar ? 'bg-transparent z-10 absolute' : 'bg-primary-700';

  return (
    <>
      <Navbar title={site?.name ?? 'Eventuras'} bgColor={bgClass} dark>
        <UserMenu lightText />
      </Navbar>
      <main id="main-content" className={fluid ? '' : 'container'}>
        {children}
      </main>
      <Footer />
    </>
  );
}
