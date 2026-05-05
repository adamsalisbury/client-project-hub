import { useEffect, useRef, useState } from 'react';

export default function CreateClientDialog({ onCreate, onCancel }) {
    const [name, setName] = useState('');
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);
    const inputRef = useRef(null);

    useEffect(() => { inputRef.current?.focus(); }, []);

    const handleSubmit = async (e) => {
        e.preventDefault();
        const trimmed = name.trim();
        if (!trimmed) {
            setError('Name is required.');
            return;
        }
        setBusy(true);
        setError(null);
        try {
            await onCreate(trimmed);
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form className="modal" onSubmit={handleSubmit}>
                <h2 className="modal-title">New client</h2>
                <p className="modal-help">
                    A client owns a set of projects. Knowledge entries on the
                    client are added to every project's prompt context.
                </p>

                <label className="field-label" htmlFor="client-name">Name</label>
                <input
                    id="client-name"
                    ref={inputRef}
                    type="text"
                    className="text-input"
                    placeholder="e.g. Smolla"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={busy}
                    maxLength={200}
                />

                {error && <div className="modal-error">{error}</div>}

                <div className="modal-actions">
                    <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                    <button type="submit" className="btn btn-primary" disabled={busy || !name.trim()}>
                        {busy ? 'Creating…' : 'Create'}
                    </button>
                </div>
            </form>
        </div>
    );
}
