import { useEffect, useRef, useState } from 'react';
import { api } from '../api.js';

const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp', 'image/gif'];
const MAX_FILES = 10;
const MAX_BYTES = 10 * 1024 * 1024;
const NEW_CLIENT = '__new__';
const NO_REPO = '';

export default function NewProjectWizard({ clients, onComplete, onCancel, onClientsChanged }) {
    const [step, setStep] = useState(1);

    const [name, setName] = useState('');

    const [clientPick, setClientPick] = useState(clients?.[0]?.id ?? '');
    const [newClientName, setNewClientName] = useState('');

    const [repos, setRepos] = useState([]);
    const [repoPick, setRepoPick] = useState(NO_REPO);

    const [createdProject, setCreatedProject] = useState(null);

    const [addTicket, setAddTicket] = useState(null);
    const [ticketMode, setTicketMode] = useState('manual');
    const [ticketCode, setTicketCode] = useState('');
    const [ticketTitle, setTicketTitle] = useState('');
    const [ticketBody, setTicketBody] = useState('');
    const [files, setFiles] = useState([]);

    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);

    const nameRef = useRef(null);
    useEffect(() => { if (step === 1) nameRef.current?.focus(); }, [step]);

    useEffect(() => {
        let cancelled = false;
        async function load() {
            if (clientPick === NEW_CLIENT || !clientPick) {
                if (!cancelled) setRepos([]);
                if (!cancelled) setRepoPick(NO_REPO);
                return;
            }
            try {
                const list = (await api.listClientRepos(clientPick)) ?? [];
                if (!cancelled) {
                    setRepos(list);
                    setRepoPick((cur) => list.some((r) => r.id === cur) ? cur : NO_REPO);
                }
            } catch (err) {
                if (!cancelled) setError(err?.message || String(err));
            }
        }
        load();
        return () => { cancelled = true; };
    }, [clientPick]);

    const resolveClientId = async () => {
        if (clientPick === NEW_CLIENT) {
            const trimmed = newClientName.trim();
            if (!trimmed) {
                throw new Error('Give the new client a name.');
            }
            const created = await api.createClient(trimmed);
            await onClientsChanged?.();
            return created.id;
        }
        if (!clientPick) {
            throw new Error('Pick a client.');
        }
        return clientPick;
    };

    const ensureProjectCreated = async () => {
        if (createdProject) return createdProject;
        const clientId = await resolveClientId();
        const project = await api.createProject({
            name: name.trim(),
            clientId,
            repoId: repoPick || null,
            description: null,
            ticketId: null
        });
        setCreatedProject(project);
        return project;
    };

    const goToTicketStep = () => {
        const trimmed = name.trim();
        if (!trimmed) { setError('Please enter a name.'); return; }
        if (clientPick === NEW_CLIENT && !newClientName.trim()) {
            setError('Give the new client a name.');
            return;
        }
        if (!clientPick) {
            setError('Pick a client.');
            return;
        }
        setError(null);
        setStep(2);
    };

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

    const removeFile = (index) => setFiles((prev) => prev.filter((_, i) => i !== index));

    const runExtraction = async () => {
        if (files.length === 0) {
            setError('Pick at least one screenshot first.');
            return;
        }
        setBusy(true);
        setError(null);
        try {
            const project = await ensureProjectCreated();
            const extracted = await api.extractTicketFromScreenshots(project.id, files);
            setTicketCode(extracted.code || '');
            setTicketTitle(extracted.title || '');
            setTicketBody(extracted.body || '');
            setTicketMode('manual');
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setBusy(false);
        }
    };

    const handleFinish = async () => {
        setBusy(true);
        setError(null);
        try {
            const project = await ensureProjectCreated();

            if (addTicket && ticketCode.trim() && ticketTitle.trim()) {
                await api.createTicket(project.id, {
                    code: ticketCode.trim(),
                    title: ticketTitle.trim(),
                    body: ticketBody
                });
            }

            onComplete(project);
        } catch (err) {
            setError(err?.message || String(err));
        } finally {
            setBusy(false);
        }
    };

    const projectFieldsLocked = busy || createdProject !== null;

    return (
        <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onCancel(); }}>
            <form
                className="modal modal-wide wizard"
                onSubmit={(e) => e.preventDefault()}
            >
                <div className="wizard-progress" aria-label="Steps">
                    <div className={`wizard-step-pill ${step >= 1 ? 'active' : ''}`}>1. Project</div>
                    <div className={`wizard-step-pill ${step >= 2 ? 'active' : ''}`}>2. First ticket</div>
                </div>

                {step === 1 && (
                    <>
                        <h2 className="modal-title">New project</h2>
                        <p className="modal-help">
                            Pick a client. Optionally link one of the client's
                            registered repos; you can also do this later from
                            the project page.
                        </p>

                        <label className="field-label" htmlFor="wizard-client">Client</label>
                        <select
                            id="wizard-client"
                            className="project-select"
                            value={clientPick}
                            onChange={(e) => setClientPick(e.target.value)}
                            disabled={projectFieldsLocked}
                        >
                            {clients.length === 0 && <option value="">No clients yet…</option>}
                            {clients.map((c) => (
                                <option key={c.id} value={c.id}>{c.name}</option>
                            ))}
                            <option value={NEW_CLIENT}>+ Create a new client…</option>
                        </select>

                        {clientPick === NEW_CLIENT && (
                            <>
                                <label className="field-label" htmlFor="wizard-new-client">New client name</label>
                                <input
                                    id="wizard-new-client"
                                    type="text"
                                    className="text-input"
                                    placeholder="e.g. Smolla"
                                    value={newClientName}
                                    onChange={(e) => setNewClientName(e.target.value)}
                                    disabled={projectFieldsLocked}
                                    maxLength={200}
                                />
                            </>
                        )}

                        <label className="field-label" htmlFor="wizard-name">Name</label>
                        <input
                            id="wizard-name"
                            ref={nameRef}
                            type="text"
                            className="text-input"
                            placeholder="e.g. code-refactor"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            disabled={projectFieldsLocked}
                            maxLength={120}
                        />

                        {clientPick && clientPick !== NEW_CLIENT && (
                            <>
                                <label className="field-label" htmlFor="wizard-repo">Repo (optional)</label>
                                <select
                                    id="wizard-repo"
                                    className="project-select"
                                    value={repoPick}
                                    onChange={(e) => setRepoPick(e.target.value)}
                                    disabled={projectFieldsLocked}
                                >
                                    <option value={NO_REPO}>— No repo —</option>
                                    {repos.map((r) => (
                                        <option key={r.id} value={r.id}>{r.name} — {r.path}</option>
                                    ))}
                                </select>
                                {repos.length === 0 && (
                                    <p className="modal-help">
                                        No repos registered against this client yet. You can add one
                                        from the client page and link it later.
                                    </p>
                                )}
                            </>
                        )}

                        {error && <div className="modal-error">{error}</div>}

                        <div className="modal-actions">
                            <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Cancel</button>
                            <button
                                type="button"
                                className="btn btn-primary"
                                onClick={goToTicketStep}
                                disabled={busy || !name.trim() || !clientPick || (clientPick === NEW_CLIENT && !newClientName.trim())}
                            >
                                Next →
                            </button>
                        </div>
                    </>
                )}

                {step === 2 && (
                    <>
                        <h2 className="modal-title">Add a ticket?</h2>
                        <p className="modal-help">
                            Optional. You can attach a starter ticket so the AI knows
                            what you're working on. Skip this step to land in the
                            project view immediately.
                        </p>

                        {addTicket === null && (
                            <div className="wizard-yesno">
                                <button
                                    type="button"
                                    className="btn btn-ghost wizard-yesno-btn"
                                    onClick={() => setAddTicket(false)}
                                    disabled={busy}
                                >
                                    Skip - no ticket
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-primary wizard-yesno-btn"
                                    onClick={() => setAddTicket(true)}
                                    disabled={busy}
                                >
                                    Yes - add ticket
                                </button>
                            </div>
                        )}

                        {addTicket === true && (
                            <>
                                <div className="ticket-mode-toggle" role="radiogroup" aria-label="Ticket source">
                                    <button
                                        type="button"
                                        role="radio"
                                        aria-checked={ticketMode === 'manual'}
                                        className={`kind-option ${ticketMode === 'manual' ? 'active' : ''}`}
                                        onClick={() => setTicketMode('manual')}
                                        disabled={busy}
                                    >
                                        <span aria-hidden="true">✎</span>
                                        <span>Fill in manually</span>
                                    </button>
                                    <button
                                        type="button"
                                        role="radio"
                                        aria-checked={ticketMode === 'screenshots'}
                                        className={`kind-option ${ticketMode === 'screenshots' ? 'active' : ''}`}
                                        onClick={() => setTicketMode('screenshots')}
                                        disabled={busy}
                                    >
                                        <span aria-hidden="true">🖼</span>
                                        <span>From screenshots</span>
                                    </button>
                                </div>

                                {ticketMode === 'screenshots' && (
                                    <div className="ticket-screenshots">
                                        <p className="modal-help">
                                            Upload up to {MAX_FILES} screenshots; the AI will read them
                                            and fill in the ticket fields below.
                                        </p>
                                        <input
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
                                                {busy ? 'Asking the AI…' : 'Extract'}
                                            </button>
                                        </div>
                                    </div>
                                )}

                                <label className="field-label" htmlFor="wizard-ticket-code">Code</label>
                                <input
                                    id="wizard-ticket-code"
                                    type="text"
                                    className="text-input"
                                    placeholder="e.g. PROJ-123"
                                    value={ticketCode}
                                    onChange={(e) => setTicketCode(e.target.value)}
                                    disabled={busy}
                                    maxLength={64}
                                />

                                <label className="field-label" htmlFor="wizard-ticket-title">Title</label>
                                <input
                                    id="wizard-ticket-title"
                                    type="text"
                                    className="text-input"
                                    placeholder="One-line summary"
                                    value={ticketTitle}
                                    onChange={(e) => setTicketTitle(e.target.value)}
                                    disabled={busy}
                                    maxLength={300}
                                />

                                <label className="field-label" htmlFor="wizard-ticket-body">Body (markdown)</label>
                                <textarea
                                    id="wizard-ticket-body"
                                    className="text-input ticket-body-input"
                                    placeholder={'## Description\n\n…'}
                                    value={ticketBody}
                                    onChange={(e) => setTicketBody(e.target.value)}
                                    disabled={busy}
                                    rows={8}
                                />
                            </>
                        )}

                        {error && <div className="modal-error">{error}</div>}

                        <div className="modal-actions">
                            <button type="button" className="btn btn-ghost" onClick={() => setStep(1)} disabled={busy || createdProject !== null}>← Back</button>
                            {addTicket === false && (
                                <button type="button" className="btn btn-primary" onClick={handleFinish} disabled={busy}>
                                    {busy ? 'Creating…' : 'Create project'}
                                </button>
                            )}
                            {addTicket === true && (
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    onClick={handleFinish}
                                    disabled={busy || !ticketCode.trim() || !ticketTitle.trim()}
                                >
                                    {busy ? 'Creating…' : 'Create project + ticket'}
                                </button>
                            )}
                        </div>
                    </>
                )}
            </form>
        </div>
    );
}
