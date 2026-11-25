import type { OverlapAnalysis } from '../api/types';
import { formatDate } from '../lib/dates';
import { formatRate } from '../lib/format';

/**
 * Analyse de chevauchement d'équipe (RB-16, US-09), présentée en texte explicite :
 * jamais une simple icône ou couleur (ENF-A4).
 */
export function OverlapDetails({ overlap, headingId }: { overlap: OverlapAnalysis; headingId: string }) {
  if (!overlap.applicable) {
    return (
      <section className="overlap overlap--neutral" aria-labelledby={headingId}>
        <h3 id={headingId} className="overlap__title">Chevauchement d'équipe : non applicable</h3>
        <p>Le demandeur n'a pas d'équipe de référence (pas de manager ou équipe vide).</p>
      </section>
    );
  }

  if (!overlap.isAlert) {
    return (
      <section className="overlap overlap--ok" aria-labelledby={headingId}>
        <h3 id={headingId} className="overlap__title">Pas de chevauchement critique</h3>
        <p>
          Au plus {overlap.maxRate ? formatRate(overlap.maxRate) : '0 absent'} sur un même jour de la période, seuil de 50 % non
          dépassé.
        </p>
      </section>
    );
  }

  return (
    <section className="overlap overlap--alert" aria-labelledby={headingId}>
      <h3 id={headingId} className="overlap__title">
        <span className="badge badge--alert">Alerte</span> Chevauchement &gt; 50 %
      </h3>
      <p>
        Plus de la moitié de l'équipe (hors demandeur) est déjà absente sur au moins un jour.
        {overlap.maxRate && <> Taux maximal : {formatRate(overlap.maxRate)}.</>}
      </p>
      <ul className="overlap__days">
        {overlap.daysOverThreshold.map((day) => (
          <li key={day.date}>
            <strong>{formatDate(day.date, 'full')}</strong> — {formatRate(day.rate)}
            {day.absentColleagues.length > 0 && (
              <> : {day.absentColleagues.map((person) => person.fullName).join(', ')}</>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
