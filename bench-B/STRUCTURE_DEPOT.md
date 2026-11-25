# Structure à pousser dans le repo bench-harness

Pousse seulement ceci (copie de `runs-B/` du zip, rempli) :

```
runs-B/
  claude-code/
    00_brief.md
    step1_spec.md
    step2_arch.md
    step3_backend.md
    step4_frontend.md
    backend/            (sans bin/ ni obj/)
    frontend/           (sans node_modules/ ni dist/)
    logs/               step3_build.txt, step3_test.txt, step4_build.txt, stepN_transcript.txt (optionnel)
    FICHE_DE_COLLECTE.md   (remplie)
  deepseek-harness/
    00_brief.md
    step1_spec.md
    step2_arch.md
    step3_backend.md
    step4_frontend.md
    backend/
    frontend/
    logs/               mêmes fichiers + stepN_events.jsonl + settings.yaml (sans clé)
    FICHE_DE_COLLECTE.md   (remplie)
```
Ne pas pousser `_prompts/` (ils sont dans le zip), ni `.env`, clés, `node_modules`, `bin`, `obj`, `dist`.
Commit unique à la fin, message libre. Dis-moi simplement "poussé" sur WhatsApp et je récupère tout pour la notation à l'aveugle.
Pour l'aveugle : je renomme moi-même en A/B. Ne mets le nom de l'outil nulle part dans le contenu des fichiers de code ou de docs (le nom du dossier suffit).
