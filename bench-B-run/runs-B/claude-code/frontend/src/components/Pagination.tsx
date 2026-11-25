interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  label: string;
}

export function Pagination({ page, pageSize, totalCount, onPageChange, label }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  if (pageCount <= 1) {
    return null;
  }
  return (
    <nav className="pagination" aria-label={label}>
      <button type="button" className="button button--secondary" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
        <span aria-hidden="true">←</span> Précédente
      </button>
      <p className="pagination__status" aria-live="polite">
        Page {page} sur {pageCount}
      </p>
      <button type="button" className="button button--secondary" onClick={() => onPageChange(page + 1)} disabled={page >= pageCount}>
        Suivante <span aria-hidden="true">→</span>
      </button>
    </nav>
  );
}
