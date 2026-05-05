import { useEffect, useRef, useState } from 'react';
import { api } from '../api.js';

export default function AddAgentDialog({ projectId, onCreate, onCancel }) {
    const [mode, setMode] = useState('manual');
    const [title, setTitle] = useState('');
    const [characteristics, setCharacteristics] = useState('');
    const [personality, setPersonality] = useState('');
    const [generating, setGenerating] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const titleRef = useRef(null);

    useEffect(() => { titleRef.current?.focus(); }, []);

    const busy = generating || saving;

    const runGeneration = async () => {
        if (!title.trim()) { setError('Title is required before generating.'); return; }
        if (!personality.trim()) { setError('Describe the agent\'s personality first.'); return; }
        setGenerating(true);
        setError(null);
        try {
            const generated = await api.generateAgent(projectId, {
                title: title.trim(),
                personality: personality.trim()
            });
            setTitle(generated.title || title);
            setCharacteristics(generated.characteristics || '');
            setMode('manual');
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setGenerating(false);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (busy) return;
        if (!title.trim()) { setError('Title is required.'); return; }
        if (characteristics == null) { setError('Characteristics are required.'); return; }
        setSaving(true);
        setError(null);
        try {
            await onCreate({ title: title.trim(), characteristics });
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form className="modal modal-wide" onSubmit={handleSubmit}>
                <h2 className="modal-title">New agent</h2>

                <div className="ticket-mode-toggle" role="radiogroup" aria-label="Agent source">
                    <button
                        type="button"
                        role="radio"
                        aria-checked={mode === 'manual'}
                        className={`kind-option ${mode === 'manual' ? 'active' : ''}`}
                        onClick={() => setMode('manual')}
                        disabled={busy}
                    >
                        <span aria-hidden="true">✎</span>
                        <span>Fill in manually</span>
                    </button>
                    <button
                        type="button"
                        role="radio"
                        aria-checked={mode === 'generate'}
                        className={`kind-option ${mode === 'generate' ? 'active' : ''}`}
                        onClick={() => setMode('generate')}
                        disabled={busy}
                    >
                        <span aria-hidden="true">✨</span>
                        <span>Generate from personality</span>
                    </button>
                </div>

                <label className="field-label" htmlFor="agent-title">Title</label>
                <input
                    id="agent-title"
                    ref={titleRef}
                    type="text"
                    className="text-input"
                    placeholder="e.g. Senior .NET reviewer"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    disabled={busy}
                    maxLength={200}
                />

                {mode === 'generate' && (
                    <>
                        <label className="field-label" htmlFor="agent-personality">Personality / focus</label>
                        <textarea
                            id="agent-personality"
                            className="text-input ticket-body-input"
                            placeholder="Describe the agent's tone, focus areas, what they care about - Claude will turn this into a fuller characteristics body."
                            value={personality}
                            onChange={(e) => setPersonality(e.target.value)}
                            disabled={busy}
                            rows={5}
                        />
                        <div className="ticket-screenshots-actions">
                            <button
                                type="button"
                                className="btn btn-primary"
                                onClick={runGeneration}
                                disabled={busy || !title.trim() || !personality.trim()}
                            >
                                {generating ? 'Asking Claude…' : 'Generate characteristics'}
                            </button>
                        </div>
                    </>
                )}

                <label className="field-label" htmlFor="agent-body">Characteristics (markdown)</label>
                <textarea
                    id="agent-body"
                    className="text-input ticket-body-input"
                    placeholder={'## Skills\n\n- …\n\n## Voice\n\n…'}
                    value={characteristics}
                    onChange={(e) => setCharacteristics(e.target.value)}
                    disabled={busy}
                    rows={10}
                />

                {error && <div className="modal-error">{error}</div>}

                <div className="modal-actions">
                    <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                    <button type="submit" className="btn btn-primary" disabled={busy || !title.trim()}>
                        {saving ? 'Saving…' : 'Save agent'}
                    </button>
                </div>
            </form>
        </div>
    );
}
