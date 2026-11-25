import { useEffect, useRef } from 'react';

/**
 * Titre du document + focus sur le titre principal à l'arrivée sur un écran :
 * en SPA, le lecteur d'écran n'annonce pas le changement de page sans ce déplacement du focus (ENF-A2).
 */
export function usePageHeading(title: string) {
  const headingRef = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    document.title = `${title} · TeamLeave`;
    headingRef.current?.focus();
  }, [title]);

  return headingRef;
}
