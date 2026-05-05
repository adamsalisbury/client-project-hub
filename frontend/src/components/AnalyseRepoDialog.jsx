import { useState } from 'react';

export default function AnalyseRepoDialog({ project, onAnalyse, onCancel }) {
    const [target, setTarget] = useState('Project');
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setBusy(true);
        setError(null);
        try {
            await onAnalyse(target);
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form className="modal" onSubmit={handleSubmit}>
                <h2 className="modal-title">Analyse repo</h2>
                <p className="modal-help">
                    The AI will read the working directory at <code className="info-mono">{project.workingDirectory ?? '(no repo)'}</code>,
                    map its sections, summarise their architecture, and produce an overall write-up.
                    The result is saved as a knowledge entry. This may take a minute or two.
                </p>

                <fieldset className="ticket-mode-toggle" style={{ alignSelf: 'stretch' }}>
                    <button
                        type="button"
                        role="radio"
                        aria-checked={target === 'Project'}
                        className={`kind-option ${target === 'Project' ? 'active' : ''}`}
                        onClick={() => setTarget('Project')}
                        disabled={busy}
                    >
                        <span aria-hidden="true">📘</span>
                        <span>Save on project</span>
                    </button>
                    <button
                        type="button"
                        role="radio"
                        aria-checked={target === 'Client'}
                        className={`kind-option ${target === 'Client' ? 'active' : ''}`}
                        onClick={() => setTarget('Client')}
                        disabled={busy}
                    >
                        <span aria-hidden="true">🏢</span>
                        <span>Save on client</span>
                    </button>
                </fieldset>

                {error && <div className="modal-error">{error}</div>}

                <div className="modal-actions">
                    <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                    <button type="submit" className="btn btn-primary" disabled={busy}>
                        {busy ? 'Asking the AI…' : 'Run analysis'}
                    </button>
                </div>
            </form>
        </div>
    );
}
