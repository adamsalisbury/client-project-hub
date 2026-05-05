import { useEffect, useRef, useState } from 'react';

export default function AddKnowledgeDialog({ onCreate, onCancel }) {
    const [title, setTitle] = useState('');
    const [body, setBody] = useState('');
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);
    const titleRef = useRef(null);

    useEffect(() => { titleRef.current?.focus(); }, []);

    const handleSubmit = async (e) => {
        e.preventDefault();
        const trimmed = title.trim();
        if (!trimmed) {
            setError('Title is required.');
            return;
        }
        setBusy(true);
        setError(null);
        try {
            await onCreate({ title: trimmed, body });
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form className="modal modal-wide" onSubmit={handleSubmit}>
                <h2 className="modal-title">Add project knowledge</h2>
                <p className="modal-help">
                    Knowledge entries are markdown notes pinned to this project.
                    They show up on the Info tab and can be opened in their own
                    application tab for quick reference.
                </p>

                <label className="field-label" htmlFor="knowledge-title">Title</label>
                <input
                    id="knowledge-title"
                    ref={titleRef}
                    type="text"
                    className="text-input"
                    placeholder="e.g. Domain glossary"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    disabled={busy}
                    maxLength={200}
                />

                <label className="field-label" htmlFor="knowledge-body">Body (markdown)</label>
                <textarea
                    id="knowledge-body"
                    className="text-input ticket-body-input"
                    placeholder={'# Notes\n\n- …'}
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    disabled={busy}
                    rows={14}
                />

                {error && <div className="modal-error">{error}</div>}

                <div className="modal-actions">
                    <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                    <button type="submit" className="btn btn-primary" disabled={busy || !title.trim()}>
                        {busy ? 'Saving…' : 'Save'}
                    </button>
                </div>
            </form>
        </div>
    );
}
