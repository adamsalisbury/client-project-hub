import { useEffect, useRef, useState } from 'react';
import { api } from '../api.js';

const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp', 'image/gif'];
const MAX_FILES = 10;
const MAX_BYTES = 10 * 1024 * 1024;

export default function NewTicketDialog({ projectId, onCreate, onCancel }) {
    const [mode, setMode] = useState('manual');
    const [code, setCode] = useState('');
    const [title, setTitle] = useState('');
    const [body, setBody] = useState('');
    const [files, setFiles] = useState([]);
    const [extracting, setExtracting] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const codeRef = useRef(null);
    const fileInputRef = useRef(null);

    useEffect(() => {
        codeRef.current?.focus();
    }, []);

    const busy = extracting || saving;

    const handleFilesChosen = (e) => {
        setError(null);
        const incoming = Array.from(e.target.files || []);
        if (incoming.length === 0) return;
        if (incoming.length > MAX_FILES) {
            setError(`At most ${MAX_FILES} files at once.`);
            return;
        }
        for (const f of incoming) {
            if (!ALLOWED_TYPES.includes(f.type)) {
                setError(`'${f.name}' has unsupported type '${f.type}'.`);
                return;
            }
            if (f.size > MAX_BYTES) {
                setError(`'${f.name}' exceeds 10 MB.`);
                return;
            }
        }
        setFiles(incoming);
    };

    const removeFile = (index) => {
        setFiles((prev) => prev.filter((_, i) => i !== index));
    };

    const runExtraction = async () => {
        if (files.length === 0) {
            setError('Pick at least one screenshot first.');
            return;
        }
        setError(null);
        setExtracting(true);
        try {
            const extracted = await api.extractTicketFromScreenshots(projectId, files);
            setCode(extracted.code || '');
            setTitle(extracted.title || '');
            setBody(extracted.body || '');
            setMode('manual');
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setExtracting(false);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (busy) return;
        const trimmedCode = code.trim();
        const trimmedTitle = title.trim();
        if (!trimmedCode) { setError('Code is required.'); return; }
        if (!trimmedTitle) { setError('Title is required.'); return; }
        if (body == null) { setError('Body is required.'); return; }

        setError(null);
        setSaving(true);
        try {
            await onCreate({ code: trimmedCode, title: trimmedTitle, body });
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form className="modal modal-wide" onSubmit={handleSubmit}>
                <h2 className="modal-title">New ticket</h2>

                <div className="ticket-mode-toggle" role="radiogroup" aria-label="Ticket source">
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
                        aria-checked={mode === 'screenshots'}
                        className={`kind-option ${mode === 'screenshots' ? 'active' : ''}`}
                        onClick={() => setMode('screenshots')}
                        disabled={busy}
                    >
                        <span aria-hidden="true">🖼</span>
                        <span>From screenshots</span>
                    </button>
                </div>

                {mode === 'screenshots' && (
                    <div className="ticket-screenshots">
                        <p className="modal-help">
                            Upload up to {MAX_FILES} screenshots (PNG, JPG, WebP, GIF; max 10 MB each).
                            Claude will read them and extract the ticket fields.
                        </p>
                        <input
                            ref={fileInputRef}
                            type="file"
                            multiple
                            accept={ALLOWED_TYPES.join(',')}
                            onChange={handleFilesChosen}
                            disabled={busy}
                        />
                        {files.length > 0 && (
                            <ul className="ticket-screenshots-list">
                                {files.map((f, i) => (
                                    <li key={i}>
                                        <span className="ticket-screenshot-name">{f.name}</span>
                                        <span className="ticket-screenshot-size">{Math.round(f.size / 1024)} KB</span>
                                        <button
                                            type="button"
                                            className="ticket-screenshot-remove"
                                            onClick={() => removeFile(i)}
                                            disabled={busy}
                                            aria-label={`Remove ${f.name}`}
                                        >×</button>
                                    </li>
                                ))}
                            </ul>
                        )}
                        <div className="ticket-screenshots-actions">
                            <button
                                type="button"
                                className="btn btn-primary"
                                onClick={runExtraction}
                                disabled={busy || files.length === 0}
                            >
                                {extracting ? 'Asking Claude…' : 'Extract'}
                            </button>
                        </div>
                    </div>
                )}

                <label className="field-label" htmlFor="ticket-code">Code</label>
                <input
                    id="ticket-code"
                    ref={codeRef}
                    type="text"
                    className="text-input"
                    placeholder="e.g. PROJ-123"
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                    disabled={busy}
                    maxLength={64}
                />

                <label className="field-label" htmlFor="ticket-title">Title</label>
                <input
                    id="ticket-title"
                    type="text"
                    className="text-input"
                    placeholder="One-line summary"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    disabled={busy}
                    maxLength={300}
                />

                <label className="field-label" htmlFor="ticket-body">Body (markdown)</label>
                <textarea
                    id="ticket-body"
                    className="text-input ticket-body-input"
                    placeholder={'## Description\n\n…'}
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    disabled={busy}
                    rows={10}
                />

                {error && <div className="modal-error">{error}</div>}

                <div className="modal-actions">
                    <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                    <button type="submit" className="btn btn-primary" disabled={busy || !code.trim() || !title.trim()}>
                        {saving ? 'Saving…' : 'Save ticket'}
                    </button>
                </div>
            </form>
        </div>
    );
}
